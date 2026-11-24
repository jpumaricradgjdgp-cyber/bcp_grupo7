using BCP.Academico.Models;
using Microsoft.EntityFrameworkCore;

namespace BCP.Academico.Data.Seed;

/// <summary>
/// Datos ficticios y reproducibles de la demostración.
/// Caso central: Mariana (origen) S/ 5,320.00 y Elena (destino) S/ 420.00.
/// </summary>
public static class DatosIniciales
{
    public const long SaldoInicialOrigen = 532_000;   // S/ 5,320.00
    public const long SaldoInicialDestino = 42_000;   // S/ 420.00
    public const string CodigoDemo = "DEMO-BCP";

    public static async Task AplicarAsync(AppDbContext db)
    {
        if (await db.Clientes.AnyAsync()) return;
        Cargar(db);
        await db.SaveChangesAsync();
    }

    /// <summary>Borra los datos de práctica y vuelve a cargar el estado inicial.</summary>
    public static async Task RestablecerAsync(AppDbContext db)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Movimientos.ExecuteDeleteAsync();
        await db.Transferencias.ExecuteDeleteAsync();
        await db.Contactos.ExecuteDeleteAsync();
        await db.Cuentas.ExecuteDeleteAsync();
        await db.Clientes.ExecuteDeleteAsync();
        await db.Eventos.ExecuteDeleteAsync();
        db.ChangeTracker.Clear();
        Cargar(db);
        await db.SaveChangesAsync();
        db.Eventos.Add(new EventoAuditoria
        {
            FechaUtc = DateTime.UtcNow, Tipo = TiposEvento.DemoRestablecida,
            Detalle = "Saldos iniciales: origen S/ 5,320.00 y destino S/ 420.00.", Resultado = "Datos de prueba cargados"
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static void Cargar(AppDbContext db)
    {
        var mariana = new Cliente { Codigo = "mariana", Nombre = "Mariana Torres", IdentificadorEnmascarado = "•••• 0101" };
        var elena = new Cliente { Codigo = "elena", Nombre = "Elena Rojas", IdentificadorEnmascarado = "•••• 0202" };

        var ctaMariana = new Cuenta { Numero = "19100042102048", Cliente = mariana, Titular = "Mariana Torres", SaldoCentimos = SaldoInicialOrigen };
        var ctaElena = new Cuenta { Numero = "19100071041042", Cliente = elena, Titular = "Elena Rojas", SaldoCentimos = SaldoInicialDestino };
        var ctaCarlos = new Cuenta { Numero = "19100055212795", Titular = "Carlos Salas", SaldoCentimos = 180_000 };
        var ctaBodega = new Cuenta { Numero = "19100039986230", Titular = "Bodega Los Alisos", Tipo = "Cuenta corriente", SaldoCentimos = 950_000 };
        db.AddRange(mariana, elena, ctaMariana, ctaElena, ctaCarlos, ctaBodega);

        db.Contactos.AddRange(
            new Contacto { Cliente = mariana, Nombre = "Elena Rojas", CuentaDestino = ctaElena, CelularYapeEnmascarado = "••• ••• 118" },
            new Contacto { Cliente = mariana, Nombre = "Carlos Salas", CuentaDestino = ctaCarlos, CelularYapeEnmascarado = "••• ••• 249" },
            new Contacto { Cliente = mariana, Nombre = "Bodega Los Alisos", CuentaDestino = ctaBodega, CelularYapeEnmascarado = "••• ••• 302" },
            new Contacto { Cliente = elena, Nombre = "Mariana Torres", CuentaDestino = ctaMariana, CelularYapeEnmascarado = "••• ••• 457" },
            new Contacto { Cliente = elena, Nombre = "Bodega Los Alisos", CuentaDestino = ctaBodega, CelularYapeEnmascarado = "••• ••• 302" });

        // Historial previo de Mariana. Los saldos de las cuentas ya reflejan estas operaciones.
        var t181 = Historica("LAB-2026-501181", mariana, ctaMariana, ctaCarlos, "Carlos Salas", "••• ••• 249", Canales.Yape, 6_500,
            "Pedido de mercadería", EstadosOperacion.Confirmada, new DateTime(2026, 11, 23, 21, 0, 0, DateTimeKind.Utc));
        var t182 = Historica("LAB-2026-501182", mariana, ctaMariana, ctaElena, "Elena Rojas", ctaElena.Enmascarada, Canales.Bcp, 14_500,
            "Devolución de compra", EstadosOperacion.Extornada, new DateTime(2026, 11, 23, 21, 15, 0, DateTimeKind.Utc));
        var t183 = Historica("LAB-2026-501183", mariana, ctaMariana, ctaBodega, "Bodega Los Alisos", "••• ••• 302", Canales.Yape, 4_800,
            "Compra de insumos", EstadosOperacion.Confirmada, new DateTime(2026, 11, 23, 22, 40, 0, DateTimeKind.Utc));
        db.Transferencias.AddRange(t181, t182, t183);

        // Saldo de Mariana antes del historial: 5,320.00 + 65.00 + 48.00 = 5,433.00
        db.Movimientos.AddRange(
            Mov(ctaMariana, t181, TiposMovimiento.Debito, -6_500, 536_800, "Yape a Carlos Salas", t181.CreadaEnUtc),
            Mov(ctaCarlos, t181, TiposMovimiento.Credito, 6_500, 180_000, "Yape de Mariana Torres", t181.CreadaEnUtc),
            Mov(ctaMariana, t182, TiposMovimiento.Debito, -14_500, 522_300, "Transferencia a Elena Rojas", t182.CreadaEnUtc),
            Mov(ctaMariana, t182, TiposMovimiento.AjusteCompensatorio, 14_500, 536_800, "Extorno por rechazo confirmado", t182.CreadaEnUtc.AddMinutes(4)),
            Mov(ctaMariana, t183, TiposMovimiento.Debito, -4_800, 532_000, "Yape a Bodega Los Alisos", t183.CreadaEnUtc),
            Mov(ctaBodega, t183, TiposMovimiento.Credito, 4_800, 950_000, "Yape de Mariana Torres", t183.CreadaEnUtc));
    }

    private static Transferencia Historica(string referencia, Cliente cliente, Cuenta origen, Cuenta destino, string nombre, string mascara,
        string canal, long centimos, string concepto, string estado, DateTime fecha) => new()
    {
        Referencia = referencia, Cliente = cliente, CuentaOrigen = origen, CuentaDestino = destino, DestinatarioNombre = nombre,
        DestinoEnmascarado = mascara, Canal = canal, ImporteCentimos = centimos, Concepto = concepto, Estado = estado,
        ClaveIdempotencia = "seed-" + referencia, HuellaSolicitud = "seed", CreadaEnUtc = fecha, ActualizadaEnUtc = fecha
    };

    private static Movimiento Mov(Cuenta cuenta, Transferencia t, string tipo, long importe, long saldo, string descripcion, DateTime fecha) => new()
    {
        Cuenta = cuenta, Transferencia = t, Tipo = tipo, ImporteCentimos = importe, SaldoResultanteCentimos = saldo,
        Descripcion = descripcion, FechaUtc = fecha
    };
}
