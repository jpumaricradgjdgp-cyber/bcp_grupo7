using BCP.Academico.Models;

namespace BCP.Academico.Services;

public enum RespuestaCanal { Aceptada, Desconocida }

/// <summary>
/// Adaptador académico local de un canal de pago. En el diseño empresarial (To-Be del PESI)
/// este punto correspondería a la integración con el sistema central o con la red de pagos
/// inmediatos; aquí responde localmente para poder ensayar los escenarios.
/// </summary>
public interface IAdaptadorCanal
{
    string Canal { get; }
    RespuestaCanal Enviar(Transferencia transferencia, Escenario escenario);
}

public class AdaptadorBcpLocal : IAdaptadorCanal
{
    public string Canal => Canales.Bcp;
    public RespuestaCanal Enviar(Transferencia t, Escenario e) =>
        e == Escenario.ResultadoDesconocido ? RespuestaCanal.Desconocida : RespuestaCanal.Aceptada;
}

public class AdaptadorYapeLocal : IAdaptadorCanal
{
    public string Canal => Canales.Yape;
    public RespuestaCanal Enviar(Transferencia t, Escenario e) =>
        e == Escenario.ResultadoDesconocido ? RespuestaCanal.Desconocida : RespuestaCanal.Aceptada;
}

public class SelectorCanal
{
    private readonly Dictionary<string, IAdaptadorCanal> _adaptadores;
    public SelectorCanal(IEnumerable<IAdaptadorCanal> adaptadores) =>
        _adaptadores = adaptadores.ToDictionary(a => a.Canal);
    public IAdaptadorCanal Para(string canal) => _adaptadores[canal];
}
