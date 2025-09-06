using Frent.Collections;
using Frent.Core;
using Frent.Systems;
using Frent.Updating;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Frent.Serialization;

public class JsonWorldSerializer
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static JsonWorldSerializer CreateSerializerSingleton()
    {
        var instance = new JsonWorldSerializer();
        return Interlocked.CompareExchange(ref s_default, instance, null) ?? instance;
    }

    private static JsonWorldSerializer? s_default;
    public static JsonWorldSerializer Default
    {
        get
        {
            if (s_default != null)
                return s_default;
            return s_default = CreateSerializerSingleton();
        }
    }

    private readonly JsonSerializerOptions _options;

    private readonly Queue<string> _componentMetadataNames = [];
    private readonly Queue<string?> _derivedMetadataNames = [];

    private bool _ignoreNonSerializableComponents;
    private int _nextEntityId;
    private RefDictionary<int, int> _entityMap = new();

    private World? _activeWorld;

    public JsonWorldSerializer(JsonSerializerOptions? options = null, bool ignoreNonSerializableComponents = true)
    {
        _options = options ?? new JsonSerializerOptions(JsonSerializerOptions.Default);

        _options.Converters.Add(new EntityJsonConverter(this));
        _options.Converters.Add(TagIDJsonConverter.Instance);
        _options.Converters.Add(ComponentIDJsonConverter.Instance);
        _options.Converters.Add(ArchetypeIDJsonConverter.Instance);

        _options.TypeInfoResolverChain.Add(new SourceGeneratedTypeInfoResolver());

        _options.MakeReadOnly();

        _ignoreNonSerializableComponents = ignoreNonSerializableComponents;
    }

    public void Serialize(Stream stream, World world, Query? query = null)
    {
        _entityMap.Clear();

        _activeWorld = world;

        using Utf8JsonWriter writer = new Utf8JsonWriter(stream);

        writer.WriteStartArray();

        SerializerState state = new SerializerState(writer, this);

        foreach(Entity e in (query ?? world.CreateQuery().Build())
            .EnumerateWithEntities())
        {
            state.Entity = e;
            state.HasDerivedComponent = false;

            writer.WriteStartObject();

            writer.WriteNumber("Id", MapEntityWrite(e));

            writer.WritePropertyName("Components");
            writer.WriteStartArray();

            e.EnumerateComponents(state);

            writer.WriteEndArray();


            writer.WritePropertyName("Types");
            writer.WriteStartArray();

            while (_componentMetadataNames.TryDequeue(out string? s))
            {
                writer.WriteStringValue(s);
            }

            writer.WriteEndArray();

            if (state.HasDerivedComponent)
            {
                writer.WritePropertyName("Impl");
                writer.WriteStartArray();
                while (_componentMetadataNames.TryDequeue(out string? s))
                {
                    writer.WriteStringValue(s);
                }
                writer.WriteEndArray();
            }
            else
            {
                _componentMetadataNames.Clear();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        _activeWorld = null;
    }

    private int MapEntityWrite(Entity entity)
    {
        ref int id = ref _entityMap.GetValueRefOrAddDefault(entity.EntityID, out bool exists);
        if(!exists)
            id = _nextEntityId++;
        return id;
    }

    private Entity MapEntityRead(int entityId)
    {
        return new Entity(_activeWorld!.WorldID, 0, entityId);
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

            // RuntimeType caches the name string, so its fine to call .ToString
            serializer._componentMetadataNames.Enqueue(typeof(T).ToString());

            serializer._derivedMetadataNames.Enqueue(
                componentIsDerivedType ?
                component!.GetType().ToString() :
                null);

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

    private class SourceGeneratedTypeInfoResolver : IJsonTypeInfoResolver
    {
        private readonly Dictionary<Type, JsonTypeInfo> _cachedTypeInfo = [];
#if DEBUG
        private JsonSerializerOptions? _parent;
#endif
        public JsonTypeInfo? GetTypeInfo(Type t, JsonSerializerOptions options)
        {
#if DEBUG
            _parent ??= options;
            Debug.Assert(_parent == options);
#endif

            if (_cachedTypeInfo.TryGetValue(t, out JsonTypeInfo? value))
                return value;

            if (!GenerationServices.JsonTypeInfoFactories.TryGetValue(t, out var factory))
                return null;

            var info = factory(options);
            _cachedTypeInfo.Add(t, info);
            return info;
        }
    }

    private static Type ReadSerializableType(ref Utf8JsonReader reader)
    {
        string name = reader.GetString() ?? string.Empty;
        Type? type = GenerationServices.SerializableTypesMap.GetValueOrDefault(name);
        return type is null ? throw new SerializationException($"Type {name} not marked as serializable.") : type;
    }

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
        private static readonly JsonEncodedText ComponentsPropertyName = JsonEncodedText.Encode("ComponentTypes");
        private static readonly JsonEncodedText TagsPropertyName = JsonEncodedText.Encode("TagTypes");

        internal static ArchetypeIDJsonConverter Instance { get; } = new();

        public override ArchetypeID Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions _)
        {
            // 256 bits
            ValueStack<ComponentID> componentIdBuffer = new(stackalloc ComponentID[16]);
            ValueStack<TagID> tagIdBuffer = new(stackalloc TagID[16]);

            ReadAssert(ref reader, JsonTokenType.StartObject);
            {
                ReadAssert(ref reader, JsonTokenType.PropertyName);
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException($"Unexpected token {reader.TokenType}, expected PropertyName");

                    if (reader.ValueTextEquals(ComponentsPropertyName.EncodedUtf8Bytes))
                    {
                        ReadAssert(ref reader, JsonTokenType.StartArray);
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            componentIdBuffer.Push(Component.GetComponentID(ReadSerializableType(ref reader)));
                    }
                    else if (reader.ValueTextEquals(TagsPropertyName.EncodedUtf8Bytes))
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
            ReadAssert(ref reader, JsonTokenType.EndObject);

            return Archetype.GetArchetypeID(componentIdBuffer.AsSpan(), tagIdBuffer.AsSpan());
        }

        public override void Write(Utf8JsonWriter writer, ArchetypeID value, JsonSerializerOptions _)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName(ComponentsPropertyName);
                {
                    writer.WriteStartArray();
                    foreach(var componentId in value.Types)
                        writer.WriteStringValue(componentId.Type.ToString());
                    writer.WriteEndArray();
                }

                writer.WritePropertyName(TagsPropertyName);
                {
                    writer.WriteStartArray();
                    foreach (var tagId in value.Tags)
                        writer.WriteStringValue(tagId.Type.ToString());
                    writer.WriteEndArray();
                }
            }
            writer.WriteEndObject();
        }

        private void ReadAssert(ref Utf8JsonReader reader, JsonTokenType expectedToken)
        {
            if (!reader.Read())
                throw new JsonException("Read to end. Is EntityType malformed?");
            if (reader.TokenType != expectedToken)
                throw new JsonException($"Unexpected token {reader.TokenType}, expected {expectedToken}");
        }
    }
}