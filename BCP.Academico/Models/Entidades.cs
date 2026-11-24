namespace BCP.Academico.Models;

/// <summary>Cliente académico (perfil ficticio) que puede iniciar sesión en la demo.</summary>
public class Cliente
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";          // "mariana", "elena"
    public string Nombre { get; set; } = "";
    public string IdentificadorEnmascarado { get; set; } = "";
    /// <summary>Se cambia al cerrar sesión para invalidar cookies emitidas antes.</summary>
    public string SelloSesion { get; set; } = Guid.NewGuid().ToString("N");
    public List<Cuenta> Cuentas { get; set; } = new();
    public List<Contacto> Contactos { get; set; } = new();
}

/// <summary>Cuenta ficticia. Los importes se guardan en céntimos enteros.</summary>
public class Cuenta
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";           // número ficticio completo (nunca se muestra entero)
    public string Tipo { get; set; } = "Cuenta de ahorros";
    public string Moneda { get; set; } = "PEN";
    public long SaldoCentimos { get; set; }
    public int? ClienteId { get; set; }                // null = cuenta de un tercero ficticio
    public Cliente? Cliente { get; set; }
    public string Titular { get; set; } = "";

    public string Enmascarada => "•••• " + Numero[^4..];
}

/// <summary>Contacto frecuente de un cliente. Apunta a una cuenta de destino.</summary>
public class Contacto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public string Nombre { get; set; } = "";
    public int CuentaDestinoId { get; set; }
    public Cuenta? CuentaDestino { get; set; }
    public string CelularYapeEnmascarado { get; set; } = "";
}

public static class Canales
{
    public const string Bcp = "BCP";
    public const string Yape = "Yape";
    public static bool EsValido(string? canal) => canal is Bcp or Yape;
}

public static class EstadosOperacion
{
    public const string Confirmada = "Confirmada";
    public const string EnVerificacion = "En verificación";
    public const string Extornada = "Extornada";
    public static readonly string[] Todos = { Confirmada, EnVerificacion, Extornada };
}

/// <summary>Una transferencia lógica. La clave de idempotencia es única por cliente.</summary>
public class Transferencia
{
    public int Id { get; set; }
    public string Referencia { get; set; } = "";
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public int CuentaOrigenId { get; set; }
    public Cuenta? CuentaOrigen { get; set; }
    public int CuentaDestinoId { get; set; }
    public Cuenta? CuentaDestino { get; set; }
    public string DestinatarioNombre { get; set; } = "";
    public string DestinoEnmascarado { get; set; } = "";
    public string Canal { get; set; } = Canales.Bcp;
    public long ImporteCentimos { get; set; }
    public long ComisionCentimos { get; set; }
    public string Concepto { get; set; } = "";
    public string Estado { get; set; } = EstadosOperacion.Confirmada;
    public string ClaveIdempotencia { get; set; } = "";
    /// <summary>Huella de los datos de la solicitud (origen, destino, canal, importe, concepto).</summary>
    public string HuellaSolicitud { get; set; } = "";
    public DateTime CreadaEnUtc { get; set; }
    public DateTime ActualizadaEnUtc { get; set; }
    public List<Movimiento> Movimientos { get; set; } = new();
}

public static class TiposMovimiento
{
    public const string Debito = "Débito";
    public const string Credito = "Crédito";
    public const string AjusteCompensatorio = "Ajuste compensatorio";
}

/// <summary>Asiento sobre una cuenta. Un (transferencia, cuenta, tipo) solo puede existir una vez.</summary>
public class Movimiento
{
    public int Id { get; set; }
    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }
    public int? TransferenciaId { get; set; }
    public Transferencia? Transferencia { get; set; }
    public string Tipo { get; set; } = TiposMovimiento.Debito;
    /// <summary>Con signo: negativo para cargos, positivo para abonos.</summary>
    public long ImporteCentimos { get; set; }
    public long SaldoResultanteCentimos { get; set; }
    public string Descripcion { get; set; } = "";
    public DateTime FechaUtc { get; set; }
}

/// <summary>Bitácora de auditoría: accesos, operaciones, fallas y conciliaciones.</summary>
public class EventoAuditoria
{
    public long Id { get; set; }
    public DateTime FechaUtc { get; set; }
    public int? ClienteId { get; set; }
    public string? Referencia { get; set; }
    public string Tipo { get; set; } = "";
    public string Detalle { get; set; } = "";
    public string Resultado { get; set; } = "";
}

public static class TiposEvento
{
    public const string InicioSesion = "Inicio de sesión";
    public const string CierreSesion = "Cierre de sesión";
    public const string AccesoDenegado = "Acceso denegado";
    public const string TransferenciaRegistrada = "Transferencia registrada";
    public const string ReintentoResuelto = "Reintento resuelto";
    public const string ConflictoClave = "Conflicto de clave";
    public const string FallaAntesDeGuardar = "Falla antes de guardar";
    public const string RespuestaPerdida = "Respuesta perdida";
    public const string ResultadoDesconocido = "Resultado externo desconocido";
    public const string ConfirmacionExterna = "Confirmación externa";
    public const string RechazoExterno = "Rechazo externo confirmado";
    public const string AjusteCompensatorio = "Ajuste compensatorio";
    public const string ConciliacionSinCambios = "Conciliación sin cambios";
    public const string ServicioIniciado = "Servicio iniciado";
    public const string EscenarioActivado = "Escenario activado";
    public const string DemoRestablecida = "Demostración restablecida";
}
