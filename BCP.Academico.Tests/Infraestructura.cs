using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BCP.Academico.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BCP.Academico.Tests;

/// <summary>Levanta el sitio MVC completo sobre un archivo SQLite propio de la prueba.</summary>
public class SitioDePrueba : WebApplicationFactory<Program>
{
    public string RutaDb { get; }
    public SitioDePrueba(string? rutaDb = null) =>
        RutaDb = rutaDb ?? Path.Combine(Path.GetTempPath(), "bcp_prueba_" + Guid.NewGuid().ToString("N") + ".db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("BaseDatos:Ruta", RutaDb);
        builder.UseSetting("Practica:Habilitada", "true");
    }

    public HttpClient Navegador() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    public T ConDb<T>(Func<AppDbContext, T> consulta)
    {
        using var scope = Services.CreateScope();
        return consulta(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}

public static partial class Ayudas
{
    public static async Task<string> TokenAsync(HttpClient c, string url)
    {
        var html = await (await c.GetAsync(url)).Content.ReadAsStringAsync();
        var m = Token().Match(html);
        Assert.True(m.Success, $"No se encontró token antifalsificación en {url}");
        return WebUtility.HtmlDecode(m.Groups[1].Value);
    }

    public static async Task<HttpResponseMessage> IngresarAsync(HttpClient c, string perfil, string codigo = "DEMO-BCP")
    {
        var token = await TokenAsync(c, "/acceso");
        return await c.PostAsync("/acceso", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["Perfil"] = perfil, ["Codigo"] = codigo
        }));
    }

    public static async Task<string> SaldoAsync(HttpClient c)
    {
        var html = await (await c.GetAsync("/banca")).Content.ReadAsStringAsync();
        var m = Saldo().Match(html);
        Assert.True(m.Success, "No se encontró el saldo en /banca");
        return WebUtility.HtmlDecode(m.Groups[1].Value);
    }

    public static async Task<(string Token, string Clave)> FormularioEnvioAsync(HttpClient c)
    {
        var html = await (await c.GetAsync("/banca/enviar")).Content.ReadAsStringAsync();
        return (WebUtility.HtmlDecode(Token().Match(html).Groups[1].Value), Clave().Match(html).Groups[1].Value);
    }

    public static FormUrlEncodedContent Envio(string token, int contactoId, string importe, string clave, string canal = "BCP", string concepto = "") =>
        new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["Canal"] = canal, ["ContactoId"] = contactoId.ToString(),
            ["Importe"] = importe, ["Concepto"] = concepto, ["ClaveIdempotencia"] = clave
        });

    public static async Task<JsonElement> ConfirmarJsonAsync(HttpClient c, FormUrlEncodedContent cuerpo)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/banca/enviar/confirmar") { Content = cuerpo };
        req.Headers.Accept.ParseAdd("application/json");
        var resp = await c.SendAsync(req);
        var cuerpo2 = await resp.Content.ReadAsStringAsync();
        Assert.True(cuerpo2.TrimStart().StartsWith('{'), $"Respuesta no JSON ({(int)resp.StatusCode}): {System.Text.RegularExpressions.Regex.Match(cuerpo2, "(SqliteException|Exception)[^<]{0,300}").Value}");
        return JsonDocument.Parse(cuerpo2).RootElement.Clone();
    }

    public static int Contacto(SitioDePrueba sitio, string cliente, string nombre) =>
        sitio.ConDb(db => db.Contactos.Include(x => x.Cliente).First(x => x.Cliente!.Codigo == cliente && x.Nombre == nombre).Id);

    public static long SaldoDb(SitioDePrueba sitio, string titular) =>
        sitio.ConDb(db => db.Cuentas.First(x => x.Titular == titular).SaldoCentimos);

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex Token();
    [GeneratedRegex("data-testid=\"saldo\">([^<]+)<")]
    private static partial Regex Saldo();
    [GeneratedRegex("name=\"ClaveIdempotencia\" value=\"([^\"]+)\"")]
    private static partial Regex Clave();
}
