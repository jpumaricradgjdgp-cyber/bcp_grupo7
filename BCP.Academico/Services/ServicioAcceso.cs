using BCP.Academico.Data;
using BCP.Academico.Data.Seed;
using BCP.Academico.Models;
using BCP.Academico.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BCP.Academico.Services;

public class ServicioAcceso
{
    private readonly AppDbContext _db;
    private readonly ServicioAuditoria _auditoria;
    public ServicioAcceso(AppDbContext db, ServicioAuditoria auditoria) { _db = db; _auditoria = auditoria; }

    public async Task<List<PerfilOpcion>> PerfilesAsync() =>
        await _db.Clientes.AsNoTracking().OrderBy(c => c.Id).Select(c => new PerfilOpcion(c.Codigo, c.Nombre)).ToListAsync();

    /// <summary>Valida el perfil académico y el código del curso en el servidor.</summary>
    public async Task<Cliente?> ValidarAsync(string perfil, string codigo)
    {
        if (!string.Equals(codigo?.Trim(), DatosIniciales.CodigoDemo, StringComparison.OrdinalIgnoreCase)) return null;
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Codigo == perfil);
        if (cliente == null) return null;
        await _auditoria.RegistrarAsync(TiposEvento.InicioSesion, $"Ingreso del perfil {cliente.Nombre}.", "Sesión iniciada", cliente.Id);
        return cliente;
    }

    public async Task<bool> SelloVigenteAsync(int clienteId, string? sello) =>
        sello != null && await _db.Clientes.AnyAsync(c => c.Id == clienteId && c.SelloSesion == sello);

    /// <summary>Invalida todas las cookies emitidas para el perfil.</summary>
    public async Task CerrarSesionAsync(int clienteId)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId);
        if (cliente == null) return;
        cliente.SelloSesion = Guid.NewGuid().ToString("N");
        _auditoria.Agregar(TiposEvento.CierreSesion, $"Salida del perfil {cliente.Nombre}.", "Sesión invalidada", clienteId);
        await _db.SaveChangesAsync();
    }
}
