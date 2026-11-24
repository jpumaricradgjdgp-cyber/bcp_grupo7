namespace BCP.Academico.Services;

public enum Escenario
{
    Normal,
    /// <summary>La operación falla antes de confirmar la transacción: no debe quedar ningún cargo.</summary>
    FallaAntesDeGuardar,
    /// <summary>La operación se guarda, pero la respuesta no llega al cliente.</summary>
    RespuestaPerdida,
    /// <summary>El canal externo no confirma ni rechaza: la operación queda En verificación.</summary>
    ResultadoDesconocido
}

/// <summary>
/// Escenario controlado del entorno de práctica. Se arma desde el panel de continuidad
/// y se aplica solo a la siguiente operación nueva (no a reintentos).
/// </summary>
public class EscenariosContinuidad
{
    private int _armado = (int)Escenario.Normal;

    public Escenario Armado => (Escenario)Volatile.Read(ref _armado);

    public void Armar(Escenario escenario) => Interlocked.Exchange(ref _armado, (int)escenario);

    /// <summary>Devuelve el escenario armado y vuelve a Normal.</summary>
    public Escenario Consumir() => (Escenario)Interlocked.Exchange(ref _armado, (int)Escenario.Normal);

    public static string Nombre(Escenario e) => e switch
    {
        Escenario.FallaAntesDeGuardar => "Falla antes de guardar",
        Escenario.RespuestaPerdida => "Respuesta perdida después de guardar",
        Escenario.ResultadoDesconocido => "Resultado externo desconocido",
        _ => "Operación normal"
    };
}
