using BCP.Academico.Data;
using BCP.Academico.Models;

namespace BCP.Academico.Services;

public class ServicioAuditoria
{
    private readonly AppDbContext _db;
    public ServicioAuditoria(AppDbContext db) => _db = db;

    /// <summary>Agrega el evento al contexto sin guardar (se guarda en la misma transacción del caso de uso).</summary>
    public void Agregar(string tipo, string detalle, string resultado, int? clienteId = null, string? referencia = null) =>
        _db.Eventos.Add(new EventoAuditoria
        {
            FechaUtc = DateTime.UtcNow, Tipo = tipo, Detalle = detalle, Resultado = resultado,
            ClienteId = clienteId, Referencia = referencia
        });

    /// <summary>Registra y guarda de inmediato (para eventos fuera de una transacción de negocio).</summary>
    public async Task RegistrarAsync(string tipo, string detalle, string resultado, int? clienteId = null, string? referencia = null)
    {
        Agregar(tipo, detalle, resultado, clienteId, referencia);
        await _db.SaveChangesAsync();
    }
}
