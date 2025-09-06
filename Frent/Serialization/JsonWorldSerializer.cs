using Frent.Collections;
using Frent.Core;
using Frent.Systems;
using Frent.Updating;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Frent.Serialization;

public class JsonWorldSerializer
{
    private readonly JsonSerializerOptions _options;
    private Stack<string> _componentMetadataNames = [];
    private Stack<string?> _derivedMetadataNames = [];
    private bool _ignoreNonSerializableComponents;

    /// <summary>
    /// The JsonTypeInfo objects we get from the framework have JsonSerializerOptions.Default as their option. If our _options is different, we need to create new copies.
    /// </summary>
    private Dictionary<ComponentID, JsonTypeInfo>? _typeInfoCache;

    public JsonWorldSerializer(JsonSerializerOptions? options = null, bool ignoreNonSerializableComponents = true)
    {
        _options = options ?? JsonSerializerOptions.Default;
        _options.Converters.Add(new EntityJsonConverter(this));
        _options.Converters.Add(TagIDJsonConverter.Instance);
        _options.Converters.Add(ComponentIDJsonConverter.Instance);
        _options.Converters.Add(ArchetypeIDJsonConverter.Instance);

        _ignoreNonSerializableComponents = ignoreNonSerializableComponents;
    }

    public void Serialize(Stream stream, World world, Query? query)
    {
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream);

        SerializerState state = new SerializerState(writer, _componentMetadataNames, _derivedMetadataNames, _ignoreNonSerializableComponents);

        foreach(Entity e in (query ?? world.CreateQuery().Build())
            .EnumerateWithEntities())
        {
            state.Entity = e;
            state.HasDerivedComponent = false;

            writer.WriteStartObject();

            writer.WriteNumber("Id", e.EntityID);

            writer.WritePropertyName("Components");
            writer.WriteStartArray();

            e.EnumerateComponents(state);

            writer.WriteEndArray();


            writer.WritePropertyName("Types");
            writer.WriteStartArray();

            while (_componentMetadataNames.TryPop(out string? s))
            {
                writer.WriteStringValue(s);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("Impl");
                writer.WriteStartArray();
            if (state.HasDerivedComponent)
            {
                while (_componentMetadataNames.TryPop(out string? s))
                {
                    writer.WriteStringValue(s);
                }
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }

    private int MapEntityWrite(Entity entity)
    {

    }
    private Entity MapEntityRead(int entityId)
    {

    }

    private struct SerializerState(
        Utf8JsonWriter jsonWriter, 
        Stack<string> componentMetadataNames, 
        Stack<string?> derivedMetadataNames, 
        JsonSerializerOptions options,
        Dictionary<ComponentID, JsonTypeInfo>? typeInfoCache,
        bool ignoreAndDontThrow) : IGenericAction
    {
        public Entity Entity;
        public bool HasDerivedComponent;
        public void Invoke<T>(ref T component)
        {
            Type actualComponentType = typeof(T);

            bool componentIsDerivedType = !(typeof(T).IsValueType ||
                typeof(T).IsSealed ||
                component is null ||
                typeof(T) == (actualComponentType = component.GetType()));

            HasDerivedComponent |= componentIsDerivedType;

            if (!options.TryGetTypeInfo(actualComponentType, out JsonTypeInfo? typeInfoToUse))
            {
                typeInfoToUse = componentIsDerivedType ?
                    GenerationServices.JsonSerializers.GetValueOrDefault(actualComponentType) :
                    Component<T>.DefaultJsonTypeInfo; // fast path
            }

            if (typeInfoToUse is null)
            {
                if (ignoreAndDontThrow)
                    return;

                FrentExceptions.Throw_InvalidOperationException($"{typeof(T).Name} is not serializable.");
            }

            // RuntimeType caches the name string, so its fine to call .ToString
            componentMetadataNames.Push(typeof(T).ToString());

            derivedMetadataNames.Push(
                componentIsDerivedType ?
                component!.GetType().ToString() :
                null);
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