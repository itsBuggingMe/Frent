using Frent.Collections;
using Frent.Core;
using Frent.Marshalling;
using Frent.Systems;
using Frent.Updating;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Frent.Serialization;

/// <summary>
/// Provides functionality for serializing and deserializing a World instance to and from JSON.
/// </summary>
public class JsonWorldSerializer
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static JsonWorldSerializer CreateSerializerSingleton()
    {
        var instance = new JsonWorldSerializer(new JsonSerializerOptions(JsonSerializerOptions.Default)
        {
            IncludeFields = true,
        });
        return Interlocked.CompareExchange(ref s_default, instance, null) ?? instance;
    }

    /// <summary>
    /// Gets the default instance of the serializer used for serializing and deserializing world data in JSON format.
    /// </summary>
    /// <remarks><see cref="JsonSerializerOptions.Default"/> is used along with converters for Frent specific types.</remarks>
    public static JsonWorldSerializer Default => s_default ?? CreateSerializerSingleton();
    private static JsonWorldSerializer? s_default;

    private static class Props
    {
        internal static ReadOnlySpan<byte> Tags => "Tags"u8;
        internal static ReadOnlySpan<byte> Id => "Id"u8;
        internal static ReadOnlySpan<byte> Components => "Components"u8;
        internal static ReadOnlySpan<byte> Types => "$types"u8;
    }

    private readonly JsonSerializerOptions _options;

    private readonly Queue<string> _componentMetadataNames = [];
    private FastStack<TagID> _readTags = FastStack<TagID>.Create(4);

    private bool _ignoreNonSerializableComponents;
    private int _nextEntityId;
    private RefDictionary<int, int> _entityMap = new();

    private World? _activeWorld;

    /// <summary>
    /// Initializes a new instance of the JsonWorldSerializer class, which is used to serialize and deserialize World instances to and from JSON.
    /// </summary>
    /// <param name="options">The JsonSerializerOptions to use for customizing JSON serialization behavior. If null, default options are used.</param>
    /// <param name="ignoreNonSerializableComponents">Indicates whether to skip components that cannot be serialized or throw an exception.</param>
    /// <param name="addGeneratedTypeInfoResolvers">Specifies whether to add source generated type resolvers to this serializer.</param>
    public JsonWorldSerializer(JsonSerializerOptions? options = null, bool ignoreNonSerializableComponents = true, bool addGeneratedTypeInfoResolvers = true)
    {
        _options = options ?? new JsonSerializerOptions(JsonSerializerOptions.Default);

        _options.Converters.Add(new EntityJsonConverter(this));
        _options.Converters.Add(TagIDJsonConverter.Instance);
        _options.Converters.Add(ComponentIDJsonConverter.Instance);
        _options.Converters.Add(ArchetypeIDJsonConverter.Instance);
        
        if(addGeneratedTypeInfoResolvers)
        {
            foreach(var resolver in GenerationServices.GeneratedJsonTypeInfoResolvers)
                _options.TypeInfoResolverChain.Insert(0, resolver);
        }

        _options.MakeReadOnly();

        _ignoreNonSerializableComponents = ignoreNonSerializableComponents;
    }

    /// <summary>
    /// Deserializes a JSON string into a new instance of the World class.
    /// </summary>
    public World Deserialize(string json, bool invokeIniters = false, IUniformProvider? uniformProvider = null, Config? config = null)
    {
        byte[] utf8Bytes = Encoding.UTF8.GetBytes(json);
        return Deserialize(new MemoryStream(utf8Bytes), invokeIniters, uniformProvider, config);
    }

    /// <summary>
    /// Deserializes a world and its entities from the specified JSON stream.
    /// </summary>
    public World Deserialize(Stream stream, bool invokeIniters = false, IUniformProvider? uniformProvider = null, Config? config = null)
    {
        _entityMap.Clear();

        World world = _activeWorld = new(uniformProvider, config);

        // when callling .Read, use the streamReader
        // when calling anything else, use the ref reader
        StreamJsonReader jsonStreamReader = new(stream);
        ref Utf8JsonReader reader = ref jsonStreamReader.CurrentReader;

        ReadAssert(ref jsonStreamReader, JsonTokenType.StartArray);


        while (jsonStreamReader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            AssertJsonToken(ref reader, JsonTokenType.StartObject);

            Entity entity = Entity.Null;
            bool hasTags = false;

            while (jsonStreamReader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                AssertJsonToken(ref reader, JsonTokenType.PropertyName);

                if (reader.ValueTextEquals(Props.Id))
                {
                    ReadAssert(ref jsonStreamReader, JsonTokenType.Number);
                    entity = MapEntityRead(reader.GetInt32());
                }
                else if (reader.ValueTextEquals(Props.Components))
                {
                    jsonStreamReader.Capture();
                }
                else if (reader.ValueTextEquals(Props.Types))
                {
                    ReadAssert(ref reader, JsonTokenType.StartArray);

                    hasTags = true;
                    _readTags.Clear();
                    while (jsonStreamReader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        _componentMetadataNames.Enqueue(reader.GetString() ?? "");
                }
                else if (reader.ValueTextEquals(Props.Tags))
                {
                    ReadAssert(ref reader, JsonTokenType.StartArray);

                    while (jsonStreamReader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        string tagTypeName = reader.GetString() ?? "";

                        var tagId = Tag.GetTagType(tagTypeName);

                        if (tagId is not { } t)
                            FrentExceptions.Throw_InvalidOperationException($"{tagTypeName} is not serializable.");
                        else
                            _readTags.Push(t);
                    }
                }
                else
                {
                    reader.Skip();
                }
            }

            // we have info now
            Utf8JsonReader capturedReader = jsonStreamReader.Restore();
            CreateEntity(ref capturedReader);

            // local method to deallocate stack space
            void CreateEntity(ref Utf8JsonReader capturedReader)
            {
                // tags
                if(hasTags)
                    entity.TagFromIDs(_readTags.AsSpan());

                // components
                int index = 0;
                Span<ComponentHandle> components = stackalloc ComponentHandle[_componentMetadataNames.Count];

                ReadAssert(ref capturedReader, JsonTokenType.StartArray);

                while (capturedReader.Read() && capturedReader.TokenType != JsonTokenType.EndArray)
                {
                    string componentTypeName = _componentMetadataNames.Dequeue();
                    Type? componentType = GenerationServices.SerializableTypesMap.GetValueOrDefault(componentTypeName) ??
                        Component.GetComponentByString(componentTypeName)?.Type;

                    if (componentType is null)
                        FrentExceptions.Throw_InvalidOperationException($"{componentTypeName} is not serializable.");

                    object comp = JsonSerializer.Deserialize(ref capturedReader, _options.GetTypeInfo(componentType))!;
                    components[index++] = ComponentHandle.CreateFromBoxed(Component.GetComponentID(componentType), comp);
                }

                entity.AddFromHandles(components);
            }
        }

        _activeWorld = null;
        jsonStreamReader.Dispose();

        Query everything = world.CreateQuery()
            .Build();

        if (invokeIniters)
        {
            foreach(var entity in everything
                .EnumerateWithEntities())
            {
                entity.EnumerateComponents(new OnDeserializedIniterInvokerState(entity));
            }
        }

        return world;
    }

    #region Serialize
    /// <summary>
    /// 
    /// </summary>
    /// <param name="world"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    public string Serialize(World world, Query? query = null)
    {
        AssertQueryFromWorld(world, query);
        using MemoryStream stream = new();
        Serialize(stream, world, query);
        return Encoding.UTF8.GetString(stream.GetBuffer().AsSpan(0/*_origin is 0 since we didn't provide it*/, (int)stream.Length));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="world"></param>
    /// <param name="query"></param>
    public void Serialize(Stream stream, World world, Query? query = null)
    {
        AssertQueryFromWorld(world, query);
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, GetWriterOptions());
        Serialize(writer, world, query);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="world"></param>
    /// <param name="query"></param>
    public void Serialize(Utf8JsonWriter writer, World world, Query? query = null)
    {
        AssertQueryFromWorld(world, query);

        Query everything = world.CreateQuery()
            .Build();

        _entityMap.Clear();

        _activeWorld = world;

        writer.WriteStartArray();

        SerializerState state = new SerializerState(writer, this);

        foreach(Entity e in (query ?? world.CreateQuery().Build())
            .EnumerateWithEntities())
        {
            state.Entity = e;
            state.HasDerivedComponent = false;

            writer.WriteStartObject();

            // metadata prop comes first
            ComponentTypes();

            writer.WriteNumber(Props.Id, MapEntityWrite(e));

            ComponentData();

            Tags();

            writer.WriteEndObject();

            void ComponentData()
            {
                writer.WritePropertyName(Props.Components);
                writer.WriteStartArray();

                e.EnumerateComponents(state);

                writer.WriteEndArray();
            }

            void ComponentTypes()
            {
                writer.WritePropertyName(Props.Types);
                writer.WriteStartArray();

                foreach (var component in e)
                    writer.WriteStringValue(component.Type.ToString());

                writer.WriteEndArray();
            }

            void Tags()
            {
                var types = e.TagTypes;
                if (types.Length == 0)
                    return;

                writer.WritePropertyName(Props.Tags);
                writer.WriteStartArray();

                foreach (var tag in types)
                    writer.WriteStringValue(tag.Type.ToString());

                writer.WriteEndArray();
            }
        }

        writer.WriteEndArray();

        _activeWorld = null;
    }

    private void AssertQueryFromWorld(World world, Query? query)
    {
        if (query is null)
            return;
        if (world != query.World)
            FrentExceptions.Throw_InvalidOperationException("Query does not belong to this world.");
    }
    #endregion

    private JsonWriterOptions GetWriterOptions()
    {
        return new JsonWriterOptions
        {
            Encoder = _options.Encoder,
            Indented = _options.WriteIndented,
            MaxDepth = _options.MaxDepth,

#if !NET8_0
            IndentSize = _options.IndentSize,
            NewLine = _options.NewLine,
            IndentCharacter = _options.IndentCharacter,
#endif
        };
    }

    private int MapEntityWrite(Entity entity)
    {
        if (!entity.IsAlive)
            return -1;

        ref int serializeId = ref _entityMap.GetValueRefOrAddDefault(entity.EntityID, out bool exists);
        if(!exists)
            serializeId = _nextEntityId++;

        return serializeId;
    }

    private Entity MapEntityRead(int serializedId)
    {
        if (serializedId == -1)
            return Entity.Null;

        ref int entityId = ref _entityMap.GetValueRefOrAddDefault(serializedId, out bool exists);

        if (exists)
            return new Entity(_activeWorld!.WorldID, 0, entityId); // point to existing entity

        Entity created = WorldMarshal.CreateEntityWithID(_activeWorld!, serializedId);

        entityId = serializedId;

        return created;
    }

    private struct OnDeserializedIniterInvokerState(Entity self) : IGenericAction
    {
        public void Invoke<T>(ref T type)
        {
            Component<T>.Initer?.Invoke(self, ref type);
        }
    }

    private struct SerializerState(
        Utf8JsonWriter jsonWriter, 
        JsonWorldSerializer serializer) : IGenericAction
    {
        public Entity Entity;
        public bool HasDerivedComponent;

        public void Invoke<T>(ref T component)
        {
            Type actualComponentType = typeof(T);
            bool componentIsDerivedType = false;

            if (!(typeof(T).IsValueType || typeof(T).IsSealed || component is null))
            {
                actualComponentType = component.GetType();
                componentIsDerivedType = actualComponentType != typeof(T);
            }

            serializer._options.TryGetTypeInfo(actualComponentType, out JsonTypeInfo? typeInfoToUse);
            
            // we manually get type info so we can choose whether or not to throw
            if (typeInfoToUse is null)
            {
                if (serializer._ignoreNonSerializableComponents)
                    return;

                FrentExceptions.Throw_InvalidOperationException($"{typeof(T).Name} is not serializable.");
            }

            HasDerivedComponent |= componentIsDerivedType;

            // serialize
            if(componentIsDerivedType)
            {
                JsonSerializer.Serialize(jsonWriter, component, typeInfoToUse);
            }
            else
            {
                // prevent excess boxing
                JsonSerializer.Serialize(jsonWriter, component, (JsonTypeInfo<T>)typeInfoToUse);
            }
        }
    }

    private static Type ReadSerializableType(ref Utf8JsonReader reader)
    {
        string name = reader.GetString() ?? string.Empty;
        Type? type = 
            GenerationServices.SerializableTypesMap.GetValueOrDefault(name) ?? 
            Component.GetComponentByString(name)?.Type ??
            Tag.GetTagType(name)?.Type;
        return type is null ? throw new SerializationException($"Type {name} not marked as serializable.") : type;
    }

    private static void ReadAssert(ref Utf8JsonReader reader, JsonTokenType expectedToken)
    {
        if (!reader.Read())
            throw new JsonException("Read to end in invalid state.");
        AssertJsonToken(ref reader, expectedToken);
    }

    private static void ReadAssert(ref StreamJsonReader state, JsonTokenType expectedToken)
    {
        if (!state.Read())
            throw new JsonException("Read to end in invalid state.");
        AssertJsonToken(ref state.CurrentReader, expectedToken);
    }
    private static void ReadAssert(ref StreamJsonReader state)
    {
        if (!state.Read())
            throw new JsonException("Read to end in invalid state.");
    }

    private static void AssertJsonToken(ref Utf8JsonReader reader, JsonTokenType expectedToken)
    {
        if (reader.TokenType != expectedToken)
            throw new JsonException($"Unexpected token {reader.TokenType}, expected {expectedToken}");
    }

    #region Converters
    private class EntityJsonConverter(JsonWorldSerializer serializer) : JsonConverter<Entity>
    {
        private readonly JsonWorldSerializer _worldSerializer = serializer;
        public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => _worldSerializer.MapEntityRead(reader.GetInt32());
        public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions _) => writer.WriteNumberValue(_worldSerializer.MapEntityWrite(value));
    }

    private class TagIDJsonConverter : JsonConverter<TagID>
    {
        internal static TagIDJsonConverter Instance { get; } = new();
        public override TagID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => 
            Tag.GetTagID(ReadSerializableType(ref reader));
        public override void Write(Utf8JsonWriter writer, TagID value, JsonSerializerOptions _) =>
            writer.WriteStringValue(value.Type.ToString());
    }

    private class ComponentIDJsonConverter : JsonConverter<ComponentID>
    {
        internal static ComponentIDJsonConverter Instance { get; } = new();
        public override ComponentID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Component.GetComponentID(ReadSerializableType(ref reader));
        public override void Write(Utf8JsonWriter writer, ComponentID value, JsonSerializerOptions _) =>
            writer.WriteStringValue(value.Type.ToString());
    }

    private class ArchetypeIDJsonConverter : JsonConverter<ArchetypeID>
    {
        internal static ArchetypeIDJsonConverter Instance { get; } = new();

        public override ArchetypeID Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions _)
        {
            // 256 bits
            ValueStack<ComponentID> componentIdBuffer = new(stackalloc ComponentID[16]);
            ValueStack<TagID> tagIdBuffer = new(stackalloc TagID[16]);

            AssertJsonToken(ref reader, JsonTokenType.StartObject);
            {
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    AssertJsonToken(ref reader, JsonTokenType.PropertyName);

                    if (reader.ValueTextEquals(Props.Components))
                    {
                        ReadAssert(ref reader, JsonTokenType.StartArray);
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            componentIdBuffer.Push(Component.GetComponentID(ReadSerializableType(ref reader)));
                    }
                    else if (reader.ValueTextEquals(Props.Tags))
                    {
                        ReadAssert(ref reader, JsonTokenType.StartArray);
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            tagIdBuffer.Push(Tag.GetTagID(ReadSerializableType(ref reader)));
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
            }
            AssertJsonToken(ref reader, JsonTokenType.EndObject);

            return Archetype.GetArchetypeID(componentIdBuffer.AsSpan(), tagIdBuffer.AsSpan());
        }

        public override void Write(Utf8JsonWriter writer, ArchetypeID value, JsonSerializerOptions _)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName(Props.Components);
                {
                    writer.WriteStartArray();
                    foreach(var componentId in value.Types)
                        writer.WriteStringValue(componentId.Type.ToString());
                    writer.WriteEndArray();
                }

                writer.WritePropertyName(Props.Tags);
                {
                    writer.WriteStartArray();
                    foreach (var tagId in value.Tags)
                        writer.WriteStringValue(tagId.Type.ToString());
                    writer.WriteEndArray();
                }
            }
            writer.WriteEndObject();
        }
    }
    #endregion
}