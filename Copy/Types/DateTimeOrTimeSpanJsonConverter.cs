using Newtonsoft.Json;
using System.Globalization;

namespace Copy.Types
{
    internal sealed class DateTimeOrTimeSpanJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            return reader.TokenType switch
            {
                JsonToken.Null => throw new JsonSerializationException("CreatedAfter cannot be null."),
                JsonToken.Date => Normalize(Convert.ToDateTime(reader.Value, CultureInfo.InvariantCulture)),
                JsonToken.String => ParseStringValue(Convert.ToString(reader.Value, CultureInfo.InvariantCulture)!),
                _ => throw new JsonSerializationException(
                    $"CreatedAfter must be a date or string, got {reader.TokenType}.")
            };
        }

        public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
        {
            writer.WriteValue(Normalize(value));
        }

        private static DateTime ParseStringValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonSerializationException("CreatedAfter cannot be empty.");
            }

            string trimmed = value.Trim();

            if (TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out TimeSpan timeSpan))
            {
                return Normalize(DateTime.Now + timeSpan);
            }

            if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                    out DateTimeOffset offsetValue))
            {
                return offsetValue.LocalDateTime;
            }

            if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal,
                    out DateTime dateTime))
            {
                return Normalize(dateTime);
            }

            throw new JsonSerializationException(
                $"CreatedAfter value '{value}' is invalid. Use a DateTime or a TimeSpan such as '-1.00:00:00' or '-12:00:00'.");
        }

        private static DateTime Normalize(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value.ToLocalTime(),
                DateTimeKind.Local => value,
                _ => DateTime.SpecifyKind(value, DateTimeKind.Local)
            };
        }
    }
}
