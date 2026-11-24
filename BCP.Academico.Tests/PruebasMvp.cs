using System.Net;
using BCP.Academico.Models;
using BCP.Academico.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static BCP.Academico.Tests.Ayudas;

namespace BCP.Academico.Tests;

/// <summary>
/// Pruebas de integración del MVP: HTTP → controladores MVC → servicios → EF Core → SQLite.
/// Riesgos del PESI cubiertos: R1 integridad y duplicidad, R2 acceso indebido, R3 interrupción del servicio.
/// </summary>
public class PruebasMvp : IDisposable
{
    private readonly SitioDePrueba _sitio = new();
    private readonly ITestOutputHelper _out;
    public PruebasMvp(ITestOutputHelper output) => _out = output;
    public void Dispose() => _sitio.Dispose();

    [Fact(DisplayName = "P01 Consulta y acceso (R2): cada perfil ve sus datos y se deniega la constancia y la cuenta ajena")]
    public async Task ConsultaYAcceso()
    {
        var mariana = _sitio.Navegador();
        Assert.Equal(HttpStatusCode.Redirect, (await IngresarAsync(mariana, "mariana")).StatusCode);
        Assert.Equal("S/ 5,320.00", await SaldoAsync(mariana));
        var panel = await (await mariana.GetAsync("/banca")).Content.ReadAsStringAsync();
        Assert.Contains("•••• 2048", panel);

        var elena = _sitio.Navegador();
        await IngresarAsync(elena, "elena");
        Assert.Equal("S/ 420.00", await SaldoAsync(elena));

        var constanciaAjena = await elena.GetAsync("/banca/constancia/LAB-2026-501183");
        Assert.Equal(HttpStatusCode.Forbidden, constanciaAjena.StatusCode);
        var cuentaMariana = _sitio.ConDb(db => db.Cuentas.First(c => c.Titular == "Mariana Torres").Id);
        var cuentaAjena = await elena.GetAsync($"/banca/cuentas/{cuentaMariana}");
        Assert.Equal(HttpStatusCode.Forbidden, cuentaAjena.StatusCode);
        Assert.Equal(2, _sitio.ConDb(db => db.Eventos.Count(e => e.Tipo == TiposEvento.AccesoDenegado)));

        // Sin sesión, la banca redirige al acceso; un código incorrecto no inicia sesión.
        var anonimo = _sitio.Navegador();
        var r = await anonimo.GetAsync("/banca");
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.StartsWith("/acceso", r.Headers.Location!.PathAndQuery);
        Assert.Equal(HttpStatusCode.BadRequest, (await IngresarAsync(anonimo, "mariana", "OTRO")).StatusCode);
        _out.WriteLine("Mariana ve S/ 5,320.00 y •••• 2048; Elena ve S/ 420.00; constancia y cuenta ajenas → 403 registrados en bitácora.");
    }

    [Fact(DisplayName = "P02 Transferencia válida (R1): S/ 50.00 deja 5,270.00 y 470.00 con un débito, un abono y una referencia")]
    public async Task TransferenciaValida()
    {
        var c = _sitio.Navegador();
        await IngresarAsync(c, "mariana");
        var (token, clave) = await FormularioEnvioAsync(c);
        var elena = Contacto(_sitio, "mariana", "Elena Rojas");

        var revision = await c.PostAsync("/banca/enviar/revisar", Envio(token, elena, "50", clave, concepto: "Demo S/ 50"));
        var html = await revision.Content.ReadAsStringAsync();
        Assert.Contains("Revisa tu envío", html);
        Assert.Contains("S/ 50.00", html);
        Assert.Equal(532_000, SaldoDb(_sitio, "Mariana Torres")); // la revisión no escribe

        var r = await c.PostAsync("/banca/enviar/confirmar", Envio(token, elena, "50", clave, concepto: "Demo S/ 50"));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var constancia = r.Headers.Location!.OriginalString;
        Assert.Contains("/banca/constancia/LAB-2026-501184", constancia);

        Assert.Equal(527_000, SaldoDb(_sitio, "Mariana Torres"));
        Assert.Equal(47_000, SaldoDb(_sitio, "Elena Rojas"));
        var t = _sitio.ConDb(db => db.Transferencias.Include(x => x.Movimientos).Single(x => x.Referencia == "LAB-2026-501184"));
        Assert.Equal(EstadosOperacion.Confirmada, t.Estado);
        Assert.Single(t.Movimientos, m => m.Tipo == TiposMovimiento.Debito && m.ImporteCentimos == -5_000);
        Assert.Single(t.Movimientos, m => m.Tipo == TiposMovimiento.Credito && m.ImporteCentimos == 5_000);

        var historial = await (await c.GetAsync("/banca/historial?q=LAB-2026-501184")).Content.ReadAsStringAsync();
        Assert.Contains("LAB-2026-501184", historial);
        Assert.Equal("S/ 5,270.00", await SaldoAsync(c));
        _out.WriteLine("LAB-2026-501184 Confirmada · Mariana 5,320.00 → 5,270.00 · Elena 420.00 → 470.00 · 1 débito + 1 abono.");
    }

    [Fact(DisplayName = "P03 Validación de saldo: un importe mayor al disponible se rechaza y los saldos no cambian")]
    public async Task ValidacionDeSaldo()
    {
        var c = _sitio.Navegador();
        await IngresarAsync(c, "mariana");
        var (token, clave) = await FormularioEnvioAsync(c);
        var carlos = Contacto(_sitio, "mariana", "Carlos Salas");

        var revision = await c.PostAsync("/banca/enviar/revisar", Envio(token, carlos, "6000", clave));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, revision.StatusCode);
        Assert.Contains("supera tu saldo disponible", await revision.Content.ReadAsStringAsync());

        var directo = await c.PostAsync("/banca/enviar/confirmar", Envio(token, carlos, "6000", clave));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, directo.StatusCode);
        var negativo = await c.PostAsync("/banca/enviar/confirmar", Envio(token, carlos, "-5", ServicioTransferencias.NuevaClave()));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, negativo.StatusCode);
        var ajeno = await c.PostAsync("/banca/enviar/confirmar", Envio(token, Contacto(_sitio, "elena", "Mariana Torres"), "10", ServicioTransferencias.NuevaClave()));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ajeno.StatusCode);

        Assert.Equal(532_000, SaldoDb(_sitio, "Mariana Torres"));
        Assert.Equal(180_000, SaldoDb(_sitio, "Carlos Salas"));
        Assert.Equal(3, _sitio.ConDb(db => db.Transferencias.Count()));
        _out.WriteLine("S/ 6,000 > S/ 5,320 → 422 con mensaje; monto negativo y contacto ajeno → 422; saldos y operaciones sin cambios.");
    }

    [Fact(DisplayName = "P04 Reintento concurrente (R1): 10 envíos con la misma clave generan una sola operación y la misma referencia")]
    public async Task ReintentoConcurrente()
    {
        var c = _sitio.Navegador();
        await IngresarAsync(c, "mariana");
        var (token, clave) = await FormularioEnvioAsync(c);
        var elena = Contacto(_sitio, "mariana", "Elena Rojas");

        var respuestas = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => ConfirmarJsonAsync(c, Envio(token, elena, "50.00", clave))));
        var refs = respuestas.Select(r => r.GetProperty("referencia").GetString()).Distinct().ToList();
        _out.WriteLine(string.Join(", ", respuestas.Select(r => r.GetProperty("resultado").GetString())));

        Assert.Single(refs);
        Assert.Equal(1, respuestas.Count(r => r.GetProperty("resultado").GetString() == "Registrada"));
        Assert.Equal(9, respuestas.Count(r => r.GetProperty("resultado").GetString() == "Reintento"));
        Assert.Equal(1, _sitio.ConDb(db => db.Transferencias.Count(t => t.ClaveIdempotencia == clave)));
        Assert.Equal(1, _sitio.ConDb(db => db.Movimientos.Count(m => m.Transferencia!.ClaveIdempotencia == clave && m.Tipo == TiposMovimiento.Debito)));
        Assert.Equal(527_000, SaldoDb(_sitio, "Mariana Torres"));
        Assert.Equal(47_000, SaldoDb(_sitio, "Elena Rojas"));

        // Reintento secuencial desde la constancia: misma referencia, sin nuevo cargo.
        var otra = await c.PostAsync("/banca/enviar/confirmar", Envio(token, elena, "50.00", clave));
        Assert.Contains($"/banca/constancia/{refs[0]}?reintento=1", otra.Headers.Location!.OriginalString);
        Assert.Equal(527_000, SaldoDb(_sitio, "Mariana Torres"));

        // Misma clave con otro importe: conflicto y se conserva la operación original.
        var conflicto = await c.PostAsync("/banca/enviar/confirmar", Envio(token, elena, "80.00", clave));
        Assert.Equal(HttpStatusCode.Conflict, conflicto.StatusCode);
        Assert.Equal(527_000, SaldoDb(_sitio, "Mariana Torres"));
        Assert.Equal(5_000, _sitio.ConDb(db => db.Transferencias.Single(t => t.ClaveIdempotencia == clave).ImporteCentimos));
        _out.WriteLine($"1 Registrada + 9 Reintento → {refs[0]}; 1 débito; saldo 5,270.00; conflicto de clave → 409 sin afectar fondos.");
    }

    [Fact(DisplayName = "P05 Pérdida de respuesta (R3): la operación guardada se recupera con la misma clave y desde el historial")]
    public async Task PerdidaDeRespuesta()
    {
        var c = _sitio.Navegador();
        await IngresarAsync(c, "mariana");
        _sitio.Services.GetRequiredService<EscenariosContinuidad>().Armar(Escenario.RespuestaPerdida);
        var (token, clave) = await FormularioEnvioAsync(c);
        var elena = Contacto(_sitio, "mariana", "Elena Rojas");

        var r = await c.PostAsync("/banca/enviar/confirmar", Envio(token, elena, "50", clave));
        Assert.Equal(HttpStatusCode.GatewayTimeout, r.StatusCode);
        var html = await r.Content.ReadAsStringAsync();
        Assert.Contains("No recibimos la respuesta", html);
        Assert.DoesNotContain("LAB-2026-501184", html);
        Assert.Equal(527_000, SaldoDb(_sitio, "Mariana Torres")); // sí se guardó

        var historial = await (await c.GetAsync("/banca/historial")).Content.ReadAsStringAsync();
        Assert.Contains("LAB-2026-501184", historial);

        var reintento = await c.PostAsync("/banca/enviar/confirmar", Envio(token, elena, "50", clave));
        Assert.Contains("/banca/constancia/LAB-2026-501184?reintento=1", reintento.Headers.Location!.OriginalString);
        Assert.Equal(527_000, SaldoDb(_sitio, "Mariana Torres"));
        Assert.Equal(1, _sitio.ConDb(db => db.Transferencias.Count(t => t.ClaveIdempotencia == clave)));
        _out.WriteLine("Respuesta 504 sin referencia → historial muestra LAB-2026-501184 → reintento devuelve la misma referencia; un solo cargo.");
    }

    [Fact(DisplayName = "P06 Falla antes de confirmar (R3): no quedan cargos ni abonos parciales")]
    public async Task FallaAntesDeConfirmar()
    {
        var c = _sitio.Navegador();
        await IngresarAsync(c, "mariana");
        _sitio.Services.GetRequiredService<EscenariosContinuidad>().Armar(Escenario.FallaAntesDeGuardar);
        var (token, clave) = await FormularioEnvioAsync(c);
        var elena = Contacto(_sitio, "mariana", "Elena Rojas");

        var r = await c.PostAsync("/banca/enviar/confirmar", Envio(token, elena, "50", clave));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
        Assert.Contains("No se realizó ningún cargo", await r.Content.ReadAsStringAsync());
        Assert.Equal(532_000, SaldoDb(_sitio, "Mariana Torres"));
        Assert.Equal(42_000, SaldoDb(_sitio, "Elena Rojas"));
        Assert.Equal(0, _sitio.ConDb(db => db.Transferencias.Count(t => t.ClaveIdempotencia == clave)));
        Assert.Equal(6, _sitio.ConDb(db => db.Movimientos.Count()));
        Assert.Equal(1, _sitio.ConDb(db => db.Eventos.Count(e => e.Tipo == TiposEvento.FallaAntesDeGuardar)));

        // El usuario puede volver a intentarlo con la misma clave: ahora se registra una sola vez.
        var otra = await c.PostAsync("/banca/enviar/confirmar", Envio(token, elena, "50", clave));
        Assert.Equal(HttpStatusCode.Redirect, otra.StatusCode);
        Assert.Equal(527_000, SaldoDb(_sitio, "Mariana Torres"));
        _out.WriteLine("503 con saldos 5,320.00 / 420.00 intactos, 0 operaciones nuevas, evento registrado; el reintento posterior registra una sola vez.");
    }

    [Fact(DisplayName = "P07 Rechazo y compensación (R1/R3): un único ajuste vinculado al cargo aunque se concilie dos veces")]
    public async Task RechazoYCompensacion()
    {
        var c = _sitio.Navegador();
        await IngresarAsync(c, "mariana");
        _sitio.Services.GetRequiredService<EscenariosContinuidad>().Armar(Escenario.ResultadoDesconocido);
        var (token, clave) = await FormularioEnvioAsync(c);
        var bodega = Contacto(_sitio, "mariana", "Bodega Los Alisos");

        var r = await c.PostAsync("/banca/enviar/confirmar", Envio(token, bodega, "50", clave, canal: "Yape"));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var referencia = _sitio.ConDb(db => db.Transferencias.Single(t => t.ClaveIdempotencia == clave).Referencia);
        Assert.Equal(EstadosOperacion.EnVerificacion, _sitio.ConDb(db => db.Transferencias.Single(t => t.Referencia == referencia).Estado));
        Assert.Equal(527_000, SaldoDb(_sitio, "Mariana Torres"));    // cargo previo
        Assert.Equal(950_000, SaldoDb(_sitio, "Bodega Los Alisos")); // sin abono

        var tokenPanel = await TokenAsync(c, "/continuidad");
        for (var i = 0; i < 2; i++)
        {
            var conc = await c.PostAsync("/continuidad/conciliar", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = tokenPanel, ["referencia"] = referencia, ["resultado"] = "Rechazado"
            }));
            Assert.Equal(HttpStatusCode.Redirect, conc.StatusCode);
        }

        var t = _sitio.ConDb(db => db.Transferencias.Include(x => x.Movimientos).Single(x => x.Referencia == referencia));
        Assert.Equal(EstadosOperacion.Extornada, t.Estado);
        Assert.Single(t.Movimientos, m => m.Tipo == TiposMovimiento.Debito);            // cargo original conservado
        Assert.Single(t.Movimientos, m => m.Tipo == TiposMovimiento.AjusteCompensatorio && m.ImporteCentimos == 5_000);
        Assert.DoesNotContain(t.Movimientos, m => m.Tipo == TiposMovimiento.Credito);
        Assert.Equal(532_000, SaldoDb(_sitio, "Mariana Torres"));
        Assert.Equal(1, _sitio.ConDb(db => db.Eventos.Count(e => e.Referencia == referencia && e.Tipo == TiposEvento.ConciliacionSinCambios)));
        _out.WriteLine($"{referencia}: En verificación (cargo, sin abono) → rechazo confirmado ×2 → Extornada con 1 débito + 1 ajuste; saldo vuelve a 5,320.00.");
    }

    [Fact(DisplayName = "P08 Persistencia (R3): tras reiniciar el servidor se conservan saldo, historial y constancia")]
    public async Task Persistencia()
    {
        var ruta = _sitio.RutaDb;
        string referencia;
        await using (var primero = new SitioDePrueba(ruta))
        {
            var c = primero.Navegador();
            await IngresarAsync(c, "mariana");
            var (token, clave) = await FormularioEnvioAsync(c);
            var r = await c.PostAsync("/banca/enviar/confirmar", Envio(token, Contacto(primero, "mariana", "Elena Rojas"), "50", clave));
            referencia = r.Headers.Location!.OriginalString.Split('/').Last().Split('?')[0];
        }   // se detiene el servidor

        await using var segundo = new SitioDePrueba(ruta);
        var n = segundo.Navegador();
        await IngresarAsync(n, "mariana");
        Assert.Equal("S/ 5,270.00", await SaldoAsync(n));
        var constancia = await n.GetAsync($"/banca/constancia/{referencia}");
        Assert.Equal(HttpStatusCode.OK, constancia.StatusCode);
        Assert.Contains(referencia, await constancia.Content.ReadAsStringAsync());
        Assert.Contains(referencia, await (await n.GetAsync("/banca/historial")).Content.ReadAsStringAsync());
        Assert.Equal(2, segundo.ConDb(db => db.Eventos.Count(e => e.Tipo == TiposEvento.ServicioIniciado)));
        _out.WriteLine($"Reinicio: {referencia} y saldo S/ 5,270.00 recuperados desde el mismo archivo SQLite (2 eventos de arranque).");
    }

    [Fact(DisplayName = "P09 Cierre de sesión y antifalsificación: la cookie anterior deja de servir y un POST sin token se rechaza")]
    public async Task CierreDeSesionYAntifalsificacion()
    {
        var c = _sitio.Navegador();
        await IngresarAsync(c, "mariana");

        var sinToken = await c.PostAsync("/banca/enviar/confirmar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Canal"] = "BCP", ["ContactoId"] = "1", ["Importe"] = "50", ["ClaveIdempotencia"] = ServicioTransferencias.NuevaClave()
        }));
        Assert.Equal(HttpStatusCode.BadRequest, sinToken.StatusCode);
        Assert.Equal(532_000, SaldoDb(_sitio, "Mariana Torres"));

        var token = await TokenAsync(c, "/banca");
        var salir = await c.PostAsync("/acceso/salir", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, salir.StatusCode);
        var despues = await c.GetAsync("/banca");
        Assert.Equal(HttpStatusCode.Redirect, despues.StatusCode);
        Assert.StartsWith("/acceso", despues.Headers.Location!.PathAndQuery);
        _out.WriteLine("POST sin token → 400 sin cambios; tras cerrar sesión /banca redirige a /acceso.");
    }

    [Fact(DisplayName = "P10 Sello de sesión: una cookie copiada antes del cierre de sesión ya no da acceso")]
    public async Task CookieAnteriorInvalidada()
    {
        var c = _sitio.Navegador();
        var login = await IngresarAsync(c, "mariana");
        var setCookie = login.Headers.GetValues("Set-Cookie").First(h => h.StartsWith("lab_bcp_sesion="));
        var valor = setCookie.Split(';')[0];

        var token = await TokenAsync(c, "/banca");
        await c.PostAsync("/acceso/salir", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        var copia = _sitio.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/banca");
        req.Headers.Add("Cookie", valor);
        var r = await copia.SendAsync(req);
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        _out.WriteLine("La cookie emitida antes del cierre de sesión es rechazada (sello rotado en SQLite).");
    }
}
