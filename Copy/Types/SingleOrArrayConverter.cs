using Newtonsoft.Json;

namespace Copy.Types
{
    internal sealed class SingleOrArrayConverter<T> : JsonConverter<T[]>
    {
        public override T[]? ReadJson(JsonReader reader, Type objectType, T[]? existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            return reader.TokenType switch
            {
                JsonToken.Null => [],
                JsonToken.StartArray => serializer.Deserialize<T[]>(reader) ?? [],
                _ =>
                [
                    serializer.Deserialize<T>(reader) ??
                    throw new JsonSerializationException($"Unable to deserialize {typeof(T).Name} value.")
                ]
            };
        }

        public override void WriteJson(JsonWriter writer, T[]? value, JsonSerializer serializer)
        {
            if (value == null || value.Length == 0)
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
                return;
            }

            if (value.Length == 1)
            {
                serializer.Serialize(writer, value[0]);
                return;
            }

            serializer.Serialize(writer, value);
        }
    }
}
