using System.ComponentModel.DataAnnotations;

namespace BCP.Academico.ViewModels;

// ---------- Acceso ----------
public class AccesoFormulario
{
    [Required(ErrorMessage = "Elige un perfil de prueba.")]
    public string Perfil { get; set; } = "mariana";

    [Required(ErrorMessage = "Escribe el código ficticio DEMO-BCP y vuelve a intentarlo.")]
    [StringLength(16)]
    public string Codigo { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public record PerfilOpcion(string Codigo, string Nombre);

public class AccesoViewModel
{
    public AccesoFormulario Formulario { get; set; } = new();
    public IReadOnlyList<PerfilOpcion> Perfiles { get; set; } = Array.Empty<PerfilOpcion>();
    public string? Error { get; set; }
}

// ---------- Cuentas ----------
public class MovimientoItem
{
    public string? Referencia { get; set; }
    public string Canal { get; set; } = "";
    public string Contraparte { get; set; } = "";
    public string Detalle { get; set; } = "";
    public DateTime FechaUtc { get; set; }
    public string Estado { get; set; } = "";
    public string Tipo { get; set; } = "";
    public long ImporteCentimos { get; set; }   // con signo
    public bool TieneConstancia { get; set; }
}

public class TarjetaSaldoViewModel
{
    public int CuentaId { get; set; }
    public string Tipo { get; set; } = "";
    public string Moneda { get; set; } = "PEN";
    public string CuentaEnmascarada { get; set; } = "";
    public long SaldoCentimos { get; set; }
}

public class PanelViewModel
{
    public string ClienteNombre { get; set; } = "";
    public TarjetaSaldoViewModel Saldo { get; set; } = new();
    public List<MovimientoItem> Movimientos { get; set; } = new();
}

public class PerfilViewModel
{
    public string Nombre { get; set; } = "";
    public string Identificador { get; set; } = "";
    public TarjetaSaldoViewModel Cuenta { get; set; } = new();
    public List<DateTime> Accesos { get; set; } = new();
}

// ---------- Transferencias ----------
/// <summary>Modelo de entrada del envío. Solo contiene lo que el cliente puede elegir.</summary>
public class EnvioFormulario
{
    public string Canal { get; set; } = "BCP";
    public int? ContactoId { get; set; }
    [StringLength(12)]
    public string? Importe { get; set; }
    [StringLength(60)]
    public string? Concepto { get; set; }
    /// <summary>Clave de idempotencia del envío lógico. Se conserva al editar y al reintentar.</summary>
    [StringLength(64)]
    public string ClaveIdempotencia { get; set; } = "";
}

public record ContactoOpcion(int Id, string Nombre, string CuentaEnmascarada, string YapeEnmascarado);

public class ResumenEnvio
{
    public string Destinatario { get; set; } = "";
    public string DestinoEnmascarado { get; set; } = "";
    public string OrigenEnmascarado { get; set; } = "";
    public string Canal { get; set; } = "";
    public long ImporteCentimos { get; set; }
    public long ComisionCentimos { get; set; }
    public string Concepto { get; set; } = "";
}

public class EnvioViewModel
{
    public EnvioFormulario Formulario { get; set; } = new();
    public List<ContactoOpcion> Contactos { get; set; } = new();
    public long SaldoDisponibleCentimos { get; set; }
    public ResumenEnvio? Revision { get; set; }
    public string? Error { get; set; }
    public string? CampoError { get; set; }
}

public class SinRespuestaViewModel
{
    public EnvioFormulario Formulario { get; set; } = new();
    public ResumenEnvio Resumen { get; set; } = new();
}

// ---------- Historial ----------
public class FiltroHistorial
{
    public string? Q { get; set; }
    public string? Canal { get; set; }
    public string? Estado { get; set; }
}

public class HistorialViewModel
{
    public FiltroHistorial Filtro { get; set; } = new();
    public List<MovimientoItem> Operaciones { get; set; } = new();
    public List<MovimientoItem> Recibidos { get; set; } = new();
}

public class AsientoItem
{
    public string Tipo { get; set; } = "";
    public string Cuenta { get; set; } = "";
    public long ImporteCentimos { get; set; }
    public DateTime FechaUtc { get; set; }
}

public class ConstanciaViewModel
{
    public string Referencia { get; set; } = "";
    public long ImporteCentimos { get; set; }
    public long ComisionCentimos { get; set; }
    public string Estado { get; set; } = "";
    public string Canal { get; set; } = "";
    public string Destinatario { get; set; } = "";
    public string DestinoEnmascarado { get; set; } = "";
    public string OrigenEnmascarado { get; set; } = "";
    public string Concepto { get; set; } = "";
    public DateTime FechaUtc { get; set; }
    public bool EsNuevoEnvio { get; set; }
    public bool EsReintento { get; set; }
    public List<AsientoItem> Asientos { get; set; } = new();
    /// <summary>Datos para repetir exactamente el mismo envío (misma clave).</summary>
    public EnvioFormulario? Reintento { get; set; }
    public long SaldoActualCentimos { get; set; }
}

// ---------- Continuidad ----------
public class OperacionContinuidad
{
    public string Referencia { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Canal { get; set; } = "";
    public long ImporteCentimos { get; set; }
    public string UltimoEvento { get; set; } = "";
    public string Resultado { get; set; } = "";
    public int Debitos { get; set; }
    public int Creditos { get; set; }
    public int Ajustes { get; set; }
}

public class EventoItem
{
    public DateTime FechaUtc { get; set; }
    public string? Referencia { get; set; }
    public string Tipo { get; set; } = "";
    public string Detalle { get; set; } = "";
    public string Resultado { get; set; } = "";
}

public class ContinuidadViewModel
{
    public string EscenarioArmado { get; set; } = "";
    public string EscenarioArmadoClave { get; set; } = "Normal";
    public List<OperacionContinuidad> Operaciones { get; set; } = new();
    public List<EventoItem> Eventos { get; set; } = new();
    public int Confirmadas { get; set; }
    public int EnVerificacion { get; set; }
    public int Extornadas { get; set; }
    public int DuplicadosEvitados { get; set; }
    public DateTime? UltimoInicioServicioUtc { get; set; }
    public long SaldoCentimos { get; set; }
    public string? Mensaje { get; set; }
    public int? ContactoPruebaId { get; set; }
    public string ContactoPruebaNombre { get; set; } = "";
}
