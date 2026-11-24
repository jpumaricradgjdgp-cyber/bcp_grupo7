using BCP.Academico.Data;
using BCP.Academico.Models;
using BCP.Academico.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BCP.Academico.Services;

public enum AccesoRecurso { Permitido, NoExiste, Ajeno }

/// <summary>Consultas de saldo, movimientos, historial y constancias. Siempre filtradas por el cliente de la sesión.</summary>
public class ServicioCuentas
{
    private readonly AppDbContext _db;
    public ServicioCuentas(AppDbContext db) => _db = db;

    public async Task<TarjetaSaldoViewModel> SaldoAsync(int clienteId)
    {
        var c = await _db.Cuentas.AsNoTracking().Where(x => x.ClienteId == clienteId).OrderBy(x => x.Id).FirstAsync();
        return Tarjeta(c);
    }

    private static TarjetaSaldoViewModel Tarjeta(Cuenta c) => new()
    {
        CuentaId = c.Id, Tipo = c.Tipo, Moneda = c.Moneda, CuentaEnmascarada = c.Enmascarada, SaldoCentimos = c.SaldoCentimos
    };

    public async Task<PanelViewModel> PanelAsync(int clienteId)
    {
        var cliente = await _db.Clientes.AsNoTracking().FirstAsync(c => c.Id == clienteId);
        var saldo = await SaldoAsync(clienteId);
        return new PanelViewModel
        {
            ClienteNombre = cliente.Nombre.Split(' ')[0], Saldo = saldo,
            Movimientos = (await MovimientosDeCuentaAsync(clienteId, saldo.CuentaId)).Take(5).ToList()
        };
    }

    /// <summary>Movimientos de una cuenta. Devuelve null si la cuenta no es del cliente.</summary>
    public async Task<(AccesoRecurso Acceso, TarjetaSaldoViewModel? Cuenta, List<MovimientoItem> Items)> MovimientosAsync(int clienteId, int cuentaId)
    {
        var cuenta = await _db.Cuentas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cuentaId);
        if (cuenta == null) return (AccesoRecurso.NoExiste, null, new());
        if (cuenta.ClienteId != clienteId) return (AccesoRecurso.Ajeno, null, new());
        return (AccesoRecurso.Permitido, Tarjeta(cuenta), await MovimientosDeCuentaAsync(clienteId, cuentaId));
    }

    private async Task<List<MovimientoItem>> MovimientosDeCuentaAsync(int clienteId, int cuentaId)
    {
        var filas = await _db.Movimientos.AsNoTracking().Include(m => m.Transferencia).ThenInclude(t => t!.CuentaOrigen)
            .Where(m => m.CuentaId == cuentaId).ToListAsync();
        return filas.OrderByDescending(m => m.FechaUtc).ThenByDescending(m => m.Id).Select(m => new MovimientoItem
        {
            Referencia = m.Transferencia?.Referencia,
            Canal = m.Transferencia?.Canal ?? "BCP",
            Contraparte = m.Tipo == TiposMovimiento.Credito ? m.Transferencia?.CuentaOrigen?.Titular ?? "" : m.Transferencia?.DestinatarioNombre ?? "",
            Detalle = m.Descripcion,
            FechaUtc = m.FechaUtc,
            Estado = m.Transferencia?.Estado ?? EstadosOperacion.Confirmada,
            Tipo = m.Tipo,
            ImporteCentimos = m.ImporteCentimos,
            TieneConstancia = m.Transferencia != null && m.Transferencia.ClienteId == clienteId
        }).ToList();
    }

    public async Task<HistorialViewModel> HistorialAsync(int clienteId, FiltroHistorial filtro)
    {
        var lista = await _db.Transferencias.AsNoTracking().Where(t => t.ClienteId == clienteId).ToListAsync();
        var q = Normalizar(filtro.Q ?? "");
        var operaciones = lista
            .Where(t => string.IsNullOrEmpty(filtro.Canal) || t.Canal == filtro.Canal)
            .Where(t => string.IsNullOrEmpty(filtro.Estado) || t.Estado == filtro.Estado)
            .Where(t => q.Length == 0 || Normalizar($"{t.Referencia} {t.DestinatarioNombre} {t.Concepto}").Contains(q))
            .OrderByDescending(t => t.CreadaEnUtc).ThenByDescending(t => t.Id)
            .Select(t => new MovimientoItem
            {
                Referencia = t.Referencia, Canal = t.Canal, Contraparte = t.DestinatarioNombre, Detalle = t.Concepto,
                FechaUtc = t.CreadaEnUtc, Estado = t.Estado, Tipo = TiposMovimiento.Debito,
                ImporteCentimos = t.Estado == EstadosOperacion.Extornada ? t.ImporteCentimos : -t.ImporteCentimos, TieneConstancia = true
            }).ToList();

        var cuentaId = (await SaldoAsync(clienteId)).CuentaId;
        var recibidos = (await MovimientosDeCuentaAsync(clienteId, cuentaId)).Where(m => m.Tipo == TiposMovimiento.Credito).ToList();
        return new HistorialViewModel { Filtro = filtro, Operaciones = operaciones, Recibidos = recibidos };
    }

    public async Task<(AccesoRecurso Acceso, ConstanciaViewModel? Constancia)> ConstanciaAsync(int clienteId, string referencia)
    {
        var t = await _db.Transferencias.AsNoTracking().Include(x => x.CuentaOrigen).Include(x => x.Movimientos).ThenInclude(m => m.Cuenta)
            .FirstOrDefaultAsync(x => x.Referencia == referencia);
        if (t == null) return (AccesoRecurso.NoExiste, null);
        if (t.ClienteId != clienteId) return (AccesoRecurso.Ajeno, null);

        var contacto = await _db.Contactos.AsNoTracking().FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.CuentaDestinoId == t.CuentaDestinoId);
        return (AccesoRecurso.Permitido, new ConstanciaViewModel
        {
            Referencia = t.Referencia, ImporteCentimos = t.ImporteCentimos, ComisionCentimos = t.ComisionCentimos, Estado = t.Estado,
            Canal = t.Canal, Destinatario = t.DestinatarioNombre, DestinoEnmascarado = t.DestinoEnmascarado,
            OrigenEnmascarado = t.CuentaOrigen!.Enmascarada, Concepto = t.Concepto, FechaUtc = t.CreadaEnUtc,
            SaldoActualCentimos = t.CuentaOrigen.SaldoCentimos,
            Asientos = t.Movimientos.OrderBy(m => m.FechaUtc).ThenBy(m => m.Id).Select(m => new AsientoItem
            {
                Tipo = m.Tipo, Cuenta = m.Cuenta!.Enmascarada + " · " + m.Cuenta.Titular, ImporteCentimos = m.ImporteCentimos, FechaUtc = m.FechaUtc
            }).ToList(),
            Reintento = contacto == null || t.ClaveIdempotencia.StartsWith("seed-") ? null : new EnvioFormulario
            {
                Canal = t.Canal, ContactoId = contacto.Id, Importe = (t.ImporteCentimos / 100m).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Concepto = t.Concepto, ClaveIdempotencia = t.ClaveIdempotencia
            }
        });
    }

    public async Task<PerfilViewModel> PerfilAsync(int clienteId)
    {
        var cliente = await _db.Clientes.AsNoTracking().FirstAsync(c => c.Id == clienteId);
        var accesos = await _db.Eventos.AsNoTracking().Where(e => e.ClienteId == clienteId && e.Tipo == TiposEvento.InicioSesion)
            .OrderByDescending(e => e.Id).Take(5).Select(e => e.FechaUtc).ToListAsync();
        return new PerfilViewModel { Nombre = cliente.Nombre, Identificador = cliente.IdentificadorEnmascarado, Cuenta = await SaldoAsync(clienteId), Accesos = accesos };
    }

    private static string Normalizar(string s) =>
        new string(s.Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray()).ToLowerInvariant().Trim();
}
