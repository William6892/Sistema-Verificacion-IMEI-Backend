using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sistema_de_Verificación_IMEI.Helpers
{
    public class ColombiaDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && DateTime.TryParse(reader.GetString(), out DateTime date))
            {
                return date;
            }
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Convertir a la hora de Colombia
            DateTime localTime = ColombiaTimeZoneHelper.ToColombiaTime(value);

            // Formatear en estándar ISO 8601 (sin el sufijo Z) para indicar hora local
            writer.WriteStringValue(localTime.ToString("yyyy-MM-ddTHH:mm:ss"));
        }
    }
}
