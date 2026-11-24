using BCP.Academico.Data;
using BCP.Academico.Models;
using Microsoft.EntityFrameworkCore;

namespace BCP.Academico.Services;

public enum ResultadoExterno { Confirmado, Rechazado }

public record ResultadoConciliacion(bool Cambio, string Mensaje, string? Estado);

/// <summary>
/// Cierra operaciones En verificación con un resultado externo comprobado.
/// Compensa una sola vez y solo ante un rechazo confirmado (nunca por tiempo agotado).
/// </summary>
public class ServicioConciliacion
{
    private readonly AppDbContext _db;
    private readonly ServicioAuditoria _auditoria;
    public ServicioConciliacion(AppDbContext db, ServicioAuditoria auditoria) { _db = db; _auditoria = auditoria; }

    public async Task<ResultadoConciliacion> ConciliarAsync(int clienteId, string referencia, ResultadoExterno externo)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        var t = await _db.Transferencias.Include(x => x.Movimientos)
            .FirstOrDefaultAsync(x => x.Referencia == referencia && x.ClienteId == clienteId);
        if (t == null) return new(false, "La referencia no existe o no pertenece a tu perfil.", null);

        if (t.Estado != EstadosOperacion.EnVerificacion)
        {
            _auditoria.Agregar(TiposEvento.ConciliacionSinCambios,
                $"Resultado externo recibido: {(externo == ResultadoExterno.Rechazado ? "rechazo" : "confirmación")}. La operación ya estaba {t.Estado}.",
                "Sin nuevo asiento", clienteId, t.Referencia);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return new(false, $"{t.Referencia} ya estaba {t.Estado.ToLowerInvariant()}. No se registró ningún asiento adicional.", t.Estado);
        }

        var ahora = DateTime.UtcNow;
        if (externo == ResultadoExterno.Confirmado)
        {
            var destino = await _db.Cuentas.FirstAsync(c => c.Id == t.CuentaDestinoId);
            destino.SaldoCentimos += t.ImporteCentimos;
            t.Movimientos.Add(new Movimiento
            {
                CuentaId = destino.Id, Tipo = TiposMovimiento.Credito, ImporteCentimos = t.ImporteCentimos,
                SaldoResultanteCentimos = destino.SaldoCentimos, Descripcion = $"{t.Canal} conciliado {t.Referencia}", FechaUtc = ahora
            });
            t.Estado = EstadosOperacion.Confirmada;
            _auditoria.Agregar(TiposEvento.ConfirmacionExterna, "El canal externo confirmó la operación. Se registró el abono.", "Confirmada", clienteId, t.Referencia);
        }
        else
        {
            var origen = await _db.Cuentas.FirstAsync(c => c.Id == t.CuentaOrigenId);
            origen.SaldoCentimos += t.ImporteCentimos + t.ComisionCentimos;
            t.Movimientos.Add(new Movimiento
            {
                CuentaId = origen.Id, Tipo = TiposMovimiento.AjusteCompensatorio, ImporteCentimos = t.ImporteCentimos + t.ComisionCentimos,
                SaldoResultanteCentimos = origen.SaldoCentimos, Descripcion = $"Extorno de {t.Referencia} por rechazo confirmado", FechaUtc = ahora
            });
            t.Estado = EstadosOperacion.Extornada;
            _auditoria.Agregar(TiposEvento.RechazoExterno, "El canal externo confirmó el rechazo.", "Rechazo comprobado", clienteId, t.Referencia);
            _auditoria.Agregar(TiposEvento.AjusteCompensatorio, $"Ajuste de {Dinero.Soles(t.ImporteCentimos)} vinculado al cargo original.", "Un único ajuste; cargo original conservado", clienteId, t.Referencia);
        }
        t.ActualizadaEnUtc = ahora;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return new(true, externo == ResultadoExterno.Confirmado
            ? $"{t.Referencia} quedó Confirmada y se registró el abono."
            : $"{t.Referencia} quedó Extornada con un único ajuste compensatorio.", t.Estado);
    }
}
