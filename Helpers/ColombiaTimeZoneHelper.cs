using System;

namespace Sistema_de_Verificación_IMEI.Helpers
{
    public static class ColombiaTimeZoneHelper
    {
        private static readonly TimeZoneInfo ColombiaZone = GetColombiaTimeZone();

        private static TimeZoneInfo GetColombiaTimeZone()
        {
            try
            {
                // Intenta usar el ID de Windows
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Intenta usar el ID de Linux (IANA) para producción en Render/Docker
                return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            }
        }

        public static DateTime ToColombiaTime(DateTime dateTime)
        {
            // Si ya es local o viene sin especificar, asumimos que puede ser UTC y lo forzamos
            var utc = dateTime.Kind == DateTimeKind.Utc ? dateTime : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, ColombiaZone);
        }
    }
}
