using BCP.Academico.Data;
using BCP.Academico.Models;
using BCP.Academico.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BCP.Academico.Services;

/// <summary>Arma el tablero académico de continuidad: operaciones, estados, eventos y contadores.</summary>
public class ServicioContinuidad
{
    private readonly AppDbContext _db;
    private readonly EscenariosContinuidad _escenarios;
    public ServicioContinuidad(AppDbContext db, EscenariosContinuidad escenarios) { _db = db; _escenarios = escenarios; }

    public async Task<ContinuidadViewModel> TableroAsync(int clienteId)
    {
        var transferencias = await _db.Transferencias.AsNoTracking().Include(t => t.Movimientos)
            .Where(t => t.ClienteId == clienteId && !t.ClaveIdempotencia.StartsWith("seed-"))
            .OrderByDescending(t => t.CreadaEnUtc).ThenByDescending(t => t.Id).Take(12).ToListAsync();
        var eventos = await _db.Eventos.AsNoTracking()
            .Where(e => e.ClienteId == clienteId || e.ClienteId == null)
            .OrderByDescending(e => e.Id).Take(40).ToListAsync();
        var refs = transferencias.Select(t => t.Referencia).ToList();
        var ultimos = (await _db.Eventos.AsNoTracking().Where(e => e.Referencia != null && refs.Contains(e.Referencia)).ToListAsync())
            .GroupBy(e => e.Referencia).Select(g => g.OrderByDescending(e => e.Id).First()).ToList();

        var contactos = await _db.Contactos.AsNoTracking().Where(c => c.ClienteId == clienteId).OrderBy(c => c.Id).ToListAsync();
        var prueba = contactos.FirstOrDefault(c => c.Nombre == "Carlos Salas") ?? contactos.LastOrDefault();
        var todas = await _db.Transferencias.AsNoTracking().Where(t => t.ClienteId == clienteId).Select(t => t.Estado).ToListAsync();
        return new ContinuidadViewModel
        {
            ContactoPruebaId = prueba?.Id, ContactoPruebaNombre = prueba?.Nombre ?? "",
            EscenarioArmado = EscenariosContinuidad.Nombre(_escenarios.Armado),
            EscenarioArmadoClave = _escenarios.Armado.ToString(),
            Operaciones = transferencias.Select(t =>
            {
                var ev = ultimos.FirstOrDefault(e => e.Referencia == t.Referencia);
                return new OperacionContinuidad
                {
                    Referencia = t.Referencia, Estado = t.Estado, Canal = t.Canal, ImporteCentimos = t.ImporteCentimos,
                    UltimoEvento = ev?.Tipo ?? "", Resultado = ev?.Resultado ?? "",
                    Debitos = t.Movimientos.Count(m => m.Tipo == TiposMovimiento.Debito),
                    Creditos = t.Movimientos.Count(m => m.Tipo == TiposMovimiento.Credito),
                    Ajustes = t.Movimientos.Count(m => m.Tipo == TiposMovimiento.AjusteCompensatorio)
                };
            }).ToList(),
            Eventos = eventos.Select(e => new EventoItem { FechaUtc = e.FechaUtc, Referencia = e.Referencia, Tipo = e.Tipo, Detalle = e.Detalle, Resultado = e.Resultado }).ToList(),
            Confirmadas = todas.Count(e => e == EstadosOperacion.Confirmada),
            EnVerificacion = todas.Count(e => e == EstadosOperacion.EnVerificacion),
            Extornadas = todas.Count(e => e == EstadosOperacion.Extornada),
            DuplicadosEvitados = await _db.Eventos.CountAsync(e => e.ClienteId == clienteId && e.Tipo == TiposEvento.ReintentoResuelto),
            UltimoInicioServicioUtc = await _db.Eventos.Where(e => e.Tipo == TiposEvento.ServicioIniciado).OrderByDescending(e => e.Id).Select(e => (DateTime?)e.FechaUtc).FirstOrDefaultAsync(),
            SaldoCentimos = (await _db.Cuentas.AsNoTracking().Where(c => c.ClienteId == clienteId).OrderBy(c => c.Id).FirstAsync()).SaldoCentimos
        };
    }

    /// <summary>Registra el arranque del servicio con las operaciones confirmadas visibles (evidencia de RPO del ejercicio).</summary>
    public static async Task RegistrarInicioAsync(AppDbContext db)
    {
        var confirmadas = await db.Transferencias.CountAsync(t => t.Estado == EstadosOperacion.Confirmada);
        var enVerificacion = await db.Transferencias.CountAsync(t => t.Estado == EstadosOperacion.EnVerificacion);
        var ultima = await db.Transferencias.OrderByDescending(t => t.Referencia).Select(t => t.Referencia).FirstOrDefaultAsync();
        db.Eventos.Add(new EventoAuditoria
        {
            FechaUtc = DateTime.UtcNow, Tipo = TiposEvento.ServicioIniciado,
            Detalle = $"Base SQLite abierta. Confirmadas: {confirmadas}; en verificación: {enVerificacion}; última referencia: {ultima ?? "ninguna"}.",
            Resultado = "Datos recuperados desde disco"
        });
        await db.SaveChangesAsync();
    }
}
