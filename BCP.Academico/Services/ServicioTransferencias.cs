using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BCP.Academico.Data;
using BCP.Academico.Models;
using BCP.Academico.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BCP.Academico.Services;

public enum TipoResultadoEnvio
{
    Registrada,          // operación nueva guardada
    Reintento,           // misma clave y mismos datos: se devuelve la referencia original
    Conflicto,           // misma clave con otros datos: se rechaza y se conserva la original
    Invalida,            // validación de negocio (saldo, importe, destino...)
    FallaAntesDeGuardar, // escenario: nada se guardó
    RespuestaPerdida     // escenario: se guardó, pero la respuesta "no llega" al cliente
}

public class ResultadoEnvio
{
    public TipoResultadoEnvio Tipo { get; init; }
    public Transferencia? Transferencia { get; init; }
    public string? Mensaje { get; init; }
    public string? Campo { get; init; }
    public static ResultadoEnvio Invalido(string mensaje, string campo) => new() { Tipo = TipoResultadoEnvio.Invalida, Mensaje = mensaje, Campo = campo };
}

/// <summary>
/// Caso de uso de transferencia: valida, aplica idempotencia y registra débito,
/// crédito, estado, referencia y auditoría en una sola transacción de base de datos.
/// </summary>
public partial class ServicioTransferencias
{
    public const long ComisionEjercicioCentimos = 0;
    private readonly AppDbContext _db;
    private readonly ServicioAuditoria _auditoria;
    private readonly EscenariosContinuidad _escenarios;
    private readonly SelectorCanal _canales;
    private readonly ILogger<ServicioTransferencias> _log;

    public ServicioTransferencias(AppDbContext db, ServicioAuditoria auditoria, EscenariosContinuidad escenarios,
        SelectorCanal canales, ILogger<ServicioTransferencias> log)
    {
        _db = db; _auditoria = auditoria; _escenarios = escenarios; _canales = canales; _log = log;
    }

    public static string NuevaClave() => Guid.NewGuid().ToString("N");

    public async Task<List<ContactoOpcion>> ContactosAsync(int clienteId) =>
        await _db.Contactos.Where(c => c.ClienteId == clienteId).OrderBy(c => c.Id)
            .Select(c => new ContactoOpcion(c.Id, c.Nombre, "•••• " + c.CuentaDestino!.Numero.Substring(c.CuentaDestino.Numero.Length - 4), c.CelularYapeEnmascarado))
            .ToListAsync();

    public async Task<Cuenta> CuentaOrigenAsync(int clienteId) =>
        await _db.Cuentas.AsNoTracking().Where(c => c.ClienteId == clienteId).OrderBy(c => c.Id).FirstAsync();

    /// <summary>Valida el formulario y arma la revisión. No escribe en la base.</summary>
    public async Task<(ResumenEnvio? Resumen, ResultadoEnvio? Error)> PrepararRevisionAsync(int clienteId, EnvioFormulario f)
    {
        var (contacto, centimos, error) = await ValidarEntradaAsync(clienteId, f);
        if (error != null) return (null, error);
        var origen = await CuentaOrigenAsync(clienteId);
        if (centimos + ComisionEjercicioCentimos > origen.SaldoCentimos)
            return (null, ResultadoEnvio.Invalido("El monto supera tu saldo disponible. Ingresa un importe menor.", "Importe"));
        return (Resumir(contacto!, origen, f, centimos), null);
    }

    public static ResumenEnvio Resumir(Contacto contacto, Cuenta origen, EnvioFormulario f, long centimos) => new()
    {
        Destinatario = contacto.Nombre,
        DestinoEnmascarado = f.Canal == Canales.Yape ? contacto.CelularYapeEnmascarado : contacto.CuentaDestino!.Enmascarada,
        OrigenEnmascarado = origen.Enmascarada,
        Canal = f.Canal, ImporteCentimos = centimos, ComisionCentimos = ComisionEjercicioCentimos,
        Concepto = (f.Concepto ?? "").Trim()
    };

    /// <summary>Confirma el envío. Es seguro llamarlo varias veces con la misma clave.</summary>
    public async Task<ResultadoEnvio> ConfirmarAsync(int clienteId, EnvioFormulario f)
    {
        if (!ClaveValida().IsMatch(f.ClaveIdempotencia ?? ""))
            return ResultadoEnvio.Invalido("La solicitud no tiene una clave de operación válida. Vuelve a abrir el formulario.", "ClaveIdempotencia");

        var (contacto, centimos, error) = await ValidarEntradaAsync(clienteId, f);
        if (error != null) return error;
        var huella = Huella(contacto!.CuentaDestinoId, f.Canal, centimos, f.Concepto);

        try
        {
            return await ConfirmarEnTransaccionAsync(clienteId, f, contacto, centimos, huella);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            // Otra solicitud con la misma clave ganó la carrera: la restricción única lo impidió.
            _db.ChangeTracker.Clear();
            var existente = await _db.Transferencias.AsNoTracking()
                .FirstOrDefaultAsync(t => t.ClienteId == clienteId && t.ClaveIdempotencia == f.ClaveIdempotencia);
            if (existente == null) throw;
            return await ResolverRepetidaAsync(clienteId, existente, huella);
        }
    }

    private async Task<ResultadoEnvio> ConfirmarEnTransaccionAsync(int clienteId, EnvioFormulario f, Contacto contacto, long centimos, string huella)
    {
        // SQLite: BEGIN IMMEDIATE. La lectura del saldo y la escritura quedan protegidas en la misma transacción.
        await using var tx = await _db.Database.BeginTransactionAsync();

        var existente = await _db.Transferencias
            .FirstOrDefaultAsync(t => t.ClienteId == clienteId && t.ClaveIdempotencia == f.ClaveIdempotencia);
        if (existente != null)
        {
            var r = await ResolverRepetidaAsync(clienteId, existente, huella, guardar: false);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return r;
        }

        var origen = await _db.Cuentas.Where(c => c.ClienteId == clienteId).OrderBy(c => c.Id).FirstAsync();
        var destino = await _db.Cuentas.FirstAsync(c => c.Id == contacto.CuentaDestinoId);
        if (centimos + ComisionEjercicioCentimos > origen.SaldoCentimos)
            return ResultadoEnvio.Invalido("El monto supera tu saldo disponible. Ingresa un importe menor.", "Importe");

        // El escenario armado se aplica solo a operaciones nuevas.
        var escenario = _escenarios.Consumir();
        var ahora = DateTime.UtcNow;
        var resumen = Resumir(contacto, origen, f, centimos);
        var t = new Transferencia
        {
            Referencia = await SiguienteReferenciaAsync(), ClienteId = clienteId, CuentaOrigenId = origen.Id, CuentaDestinoId = destino.Id,
            DestinatarioNombre = contacto.Nombre, DestinoEnmascarado = resumen.DestinoEnmascarado, Canal = f.Canal,
            ImporteCentimos = centimos, ComisionCentimos = ComisionEjercicioCentimos, Concepto = resumen.Concepto,
            ClaveIdempotencia = f.ClaveIdempotencia, HuellaSolicitud = huella, CreadaEnUtc = ahora, ActualizadaEnUtc = ahora
        };
        _db.Transferencias.Add(t);

        origen.SaldoCentimos -= centimos + ComisionEjercicioCentimos;
        t.Movimientos.Add(new Movimiento
        {
            CuentaId = origen.Id, Tipo = TiposMovimiento.Debito, ImporteCentimos = -centimos, SaldoResultanteCentimos = origen.SaldoCentimos,
            Descripcion = $"{(f.Canal == Canales.Yape ? "Yape" : "Transferencia")} a {contacto.Nombre}", FechaUtc = ahora
        });

        var respuesta = _canales.Para(f.Canal).Enviar(t, escenario);
        if (respuesta == RespuestaCanal.Aceptada)
        {
            destino.SaldoCentimos += centimos;
            t.Movimientos.Add(new Movimiento
            {
                CuentaId = destino.Id, Tipo = TiposMovimiento.Credito, ImporteCentimos = centimos, SaldoResultanteCentimos = destino.SaldoCentimos,
                Descripcion = $"{(f.Canal == Canales.Yape ? "Yape" : "Transferencia")} de {origen.Titular}", FechaUtc = ahora
            });
            t.Estado = EstadosOperacion.Confirmada;
            _auditoria.Agregar(TiposEvento.TransferenciaRegistrada, $"{f.Canal} · {Dinero.Soles(centimos)} a {contacto.Nombre}", "Débito y abono registrados", clienteId, t.Referencia);
        }
        else
        {
            t.Estado = EstadosOperacion.EnVerificacion;
            _auditoria.Agregar(TiposEvento.ResultadoDesconocido, $"{f.Canal} · {Dinero.Soles(centimos)}. El canal externo no confirmó ni rechazó.", "En verificación: sin abono ni ajuste", clienteId, t.Referencia);
        }

        if (escenario == Escenario.FallaAntesDeGuardar)
        {
            // Se simula una caída justo antes del commit: la transacción se revierte completa.
            await tx.RollbackAsync();
            _db.ChangeTracker.Clear();
            await _auditoria.RegistrarAsync(TiposEvento.FallaAntesDeGuardar,
                $"{f.Canal} · {Dinero.Soles(centimos)} a {contacto.Nombre}. Transacción revertida.", "Saldos intactos, sin cargo ni abono", clienteId);
            return new ResultadoEnvio { Tipo = TipoResultadoEnvio.FallaAntesDeGuardar, Mensaje = "No pudimos completar el envío. No se realizó ningún cargo." };
        }

        if (escenario == Escenario.RespuestaPerdida)
            _auditoria.Agregar(TiposEvento.RespuestaPerdida, "La operación quedó guardada, pero la respuesta no llegó al canal.", "Recuperable con la misma clave", clienteId, t.Referencia);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        _log.LogInformation("Transferencia {Ref} {Estado} ({Escenario})", t.Referencia, t.Estado, escenario);

        return new ResultadoEnvio
        {
            Tipo = escenario == Escenario.RespuestaPerdida ? TipoResultadoEnvio.RespuestaPerdida : TipoResultadoEnvio.Registrada,
            Transferencia = t
        };
    }

    private async Task<ResultadoEnvio> ResolverRepetidaAsync(int clienteId, Transferencia existente, string huella, bool guardar = true)
    {
        if (existente.HuellaSolicitud == huella)
        {
            _auditoria.Agregar(TiposEvento.ReintentoResuelto, "Misma clave y mismos datos: se devolvió la referencia original.", "Duplicado evitado, sin nuevo cargo", clienteId, existente.Referencia);
            if (guardar) await _db.SaveChangesAsync();
            return new ResultadoEnvio { Tipo = TipoResultadoEnvio.Reintento, Transferencia = existente };
        }
        _auditoria.Agregar(TiposEvento.ConflictoClave, "La clave ya pertenece a otra operación con distinto importe o destino.", "Rechazado; se conserva la operación original", clienteId, existente.Referencia);
        if (guardar) await _db.SaveChangesAsync();
        return new ResultadoEnvio
        {
            Tipo = TipoResultadoEnvio.Conflicto, Transferencia = existente, Campo = "ClaveIdempotencia",
            Mensaje = $"Esta solicitud ya se registró como {existente.Referencia} con otros datos. No se hizo un nuevo cargo; inicia un envío nuevo si quieres cambiar el importe o el destino."
        };
    }

    private async Task<(Contacto? Contacto, long Centimos, ResultadoEnvio? Error)> ValidarEntradaAsync(int clienteId, EnvioFormulario f)
    {
        if (!Canales.EsValido(f.Canal))
            return (null, 0, ResultadoEnvio.Invalido("Elige el canal BCP o Yape.", "Canal"));
        if (f.ContactoId is null)
            return (null, 0, ResultadoEnvio.Invalido("Selecciona un destinatario para continuar.", "ContactoId"));
        // El contacto debe pertenecer al cliente que opera: autorización en el servidor.
        var contacto = await _db.Contactos.Include(c => c.CuentaDestino).AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == f.ContactoId && c.ClienteId == clienteId);
        if (contacto == null)
            return (null, 0, ResultadoEnvio.Invalido("El destinatario no pertenece a tus contactos.", "ContactoId"));
        var centimos = Dinero.LeerCentimos(f.Importe);
        if (centimos is null or <= 0)
            return (null, 0, ResultadoEnvio.Invalido("Ingresa un monto positivo, con hasta dos decimales.", "Importe"));
        if ((f.Concepto ?? "").Length > 60)
            return (null, 0, ResultadoEnvio.Invalido("El concepto admite hasta 60 caracteres.", "Concepto"));
        var origen = await CuentaOrigenAsync(clienteId);
        if (origen.Id == contacto.CuentaDestinoId)
            return (null, 0, ResultadoEnvio.Invalido("La cuenta de destino debe ser distinta de la de origen.", "ContactoId"));
        return (contacto, centimos.Value, null);
    }

    private async Task<string> SiguienteReferenciaAsync()
    {
        var ultima = await _db.Transferencias.Where(t => t.Referencia.StartsWith("LAB-2026-"))
            .OrderByDescending(t => t.Referencia).Select(t => t.Referencia).FirstOrDefaultAsync();
        var numero = ultima == null ? 501183 : int.Parse(ultima["LAB-2026-".Length..]);
        return $"LAB-2026-{numero + 1}";
    }

    public static string Huella(int cuentaDestinoId, string canal, long centimos, string? concepto)
    {
        var texto = $"{cuentaDestinoId}|{canal}|{centimos}|{(concepto ?? "").Trim()}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
    }

    [GeneratedRegex("^[A-Za-z0-9-]{16,64}$")]
    private static partial Regex ClaveValida();
}
