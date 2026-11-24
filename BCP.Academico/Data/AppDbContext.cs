using BCP.Academico.Models;
using Microsoft.EntityFrameworkCore;

namespace BCP.Academico.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<Contacto> Contactos => Set<Contacto>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<EventoAuditoria> Eventos => Set<EventoAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
