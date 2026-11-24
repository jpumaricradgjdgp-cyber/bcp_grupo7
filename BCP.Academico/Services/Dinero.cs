using System.Globalization;
using System.Text.RegularExpressions;

namespace BCP.Academico.Services;

/// <summary>Formato y lectura de importes. Internamente todo se maneja en céntimos enteros.</summary>
public static partial class Dinero
{
    private static readonly CultureInfo Formato = CultureInfo.GetCultureInfo("en-US");
    private static readonly TimeZoneInfo Lima = BuscarZonaLima();

    public static string Soles(long centimos) =>
        (centimos < 0 ? "−" : "") + "S/ " + (Math.Abs(centimos) / 100m).ToString("N2", Formato);

    public static string SolesConSigno(long centimos) =>
        (centimos < 0 ? "− " : "+ ") + "S/ " + (Math.Abs(centimos) / 100m).ToString("N2", Formato);

    /// <summary>Acepta "50", "50.5", "50,50". Devuelve null si el formato no es válido.</summary>
    public static long? LeerCentimos(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var limpio = texto.Trim().Replace("S/", "").Trim();
        if (!FormatoImporte().IsMatch(limpio)) return null;
        var valor = decimal.Parse(limpio.Replace(',', '.'), CultureInfo.InvariantCulture);
        return (long)Math.Round(valor * 100m, MidpointRounding.AwayFromZero);
    }

    public static string Fecha(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Lima)
            .ToString("d MMM yyyy, h:mm tt", CultureInfo.GetCultureInfo("es-PE"));

    public static string FechaHoraSegundos(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Lima)
            .ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

    private static TimeZoneInfo BuscarZonaLima()
    {
        foreach (var id in new[] { "America/Lima", "SA Pacific Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("Lima", TimeSpan.FromHours(-5), "Lima", "Lima");
    }

    [GeneratedRegex(@"^\d{1,7}([.,]\d{1,2})?$")]
    private static partial Regex FormatoImporte();
}
