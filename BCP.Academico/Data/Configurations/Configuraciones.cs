using BCP.Academico.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BCP.Academico.Data.Configurations;

public class ClienteConfig : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.Property(x => x.Codigo).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.Codigo).IsUnique();
        b.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
        b.Property(x => x.IdentificadorEnmascarado).HasMaxLength(20);
        b.Property(x => x.SelloSesion).HasMaxLength(40).IsRequired();
    }
}

public class CuentaConfig : IEntityTypeConfiguration<Cuenta>
{
    public void Configure(EntityTypeBuilder<Cuenta> b)
    {
        b.Property(x => x.Numero).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Numero).IsUnique();
        b.Property(x => x.Tipo).HasMaxLength(40);
        b.Property(x => x.Moneda).HasMaxLength(3);
        b.Property(x => x.Titular).HasMaxLength(80);
        b.Ignore(x => x.Enmascarada);
        b.HasOne(x => x.Cliente).WithMany(c => c.Cuentas).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("CK_Cuenta_SaldoNoNegativo", "SaldoCentimos >= 0"));
    }
}

public class ContactoConfig : IEntityTypeConfiguration<Contacto>
{
    public void Configure(EntityTypeBuilder<Contacto> b)
    {
        b.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
        b.Property(x => x.CelularYapeEnmascarado).HasMaxLength(20);
        b.HasOne(x => x.Cliente).WithMany(c => c.Contactos).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.CuentaDestino).WithMany().HasForeignKey(x => x.CuentaDestinoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TransferenciaConfig : IEntityTypeConfiguration<Transferencia>
{
    public void Configure(EntityTypeBuilder<Transferencia> b)
    {
        b.Property(x => x.Referencia).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Referencia).IsUnique();
        b.Property(x => x.ClaveIdempotencia).HasMaxLength(64).IsRequired();
        // Una clave de idempotencia solo puede existir una vez por perfil que opera.
        b.HasIndex(x => new { x.ClienteId, x.ClaveIdempotencia }).IsUnique();
        b.Property(x => x.HuellaSolicitud).HasMaxLength(64).IsRequired();
        b.Property(x => x.Canal).HasMaxLength(10).IsRequired();
        b.Property(x => x.Estado).HasMaxLength(20).IsRequired();
        b.Property(x => x.Concepto).HasMaxLength(60);
        b.Property(x => x.DestinatarioNombre).HasMaxLength(80);
        b.Property(x => x.DestinoEnmascarado).HasMaxLength(20);
        b.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaOrigen).WithMany().HasForeignKey(x => x.CuentaOrigenId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaDestino).WithMany().HasForeignKey(x => x.CuentaDestinoId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("CK_Transferencia_ImportePositivo", "ImporteCentimos > 0"));
    }
}

public class MovimientoConfig : IEntityTypeConfiguration<Movimiento>
{
    public void Configure(EntityTypeBuilder<Movimiento> b)
    {
        b.Property(x => x.Tipo).HasMaxLength(30).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(120);
        b.HasOne(x => x.Cuenta).WithMany().HasForeignKey(x => x.CuentaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Transferencia).WithMany(t => t.Movimientos).HasForeignKey(x => x.TransferenciaId).OnDelete(DeleteBehavior.Restrict);
        // Evita un segundo débito, crédito o ajuste para la misma operación y cuenta.
        b.HasIndex(x => new { x.TransferenciaId, x.CuentaId, x.Tipo }).IsUnique();
        b.HasIndex(x => new { x.CuentaId, x.FechaUtc });
    }
}

public class EventoConfig : IEntityTypeConfiguration<EventoAuditoria>
{
    public void Configure(EntityTypeBuilder<EventoAuditoria> b)
    {
        b.ToTable("EventosAuditoria");
        b.Property(x => x.Tipo).HasMaxLength(40).IsRequired();
        b.Property(x => x.Detalle).HasMaxLength(300);
        b.Property(x => x.Resultado).HasMaxLength(80);
        b.Property(x => x.Referencia).HasMaxLength(20);
        b.HasIndex(x => x.Referencia);
        b.HasIndex(x => x.FechaUtc);
    }
}
