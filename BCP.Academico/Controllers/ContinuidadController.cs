using BCP.Academico.Data;
using BCP.Academico.Data.Seed;
using BCP.Academico.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BCP.Academico.Controllers;

/// <summary>
/// Panel académico de continuidad. Solo existe en el entorno de práctica
/// (configuración Practica:Habilitada = true).
/// </summary>
[Route("continuidad")]
public class ContinuidadController : BancaControllerBase
{
    private readonly ServicioContinuidad _continuidad;
    private readonly ServicioConciliacion _conciliacion;
    private readonly EscenariosContinuidad _escenarios;
    private readonly ServicioAuditoria _auditoria;
    private readonly IConfiguration _config;

    public ContinuidadController(ServicioContinuidad continuidad, ServicioConciliacion conciliacion, EscenariosContinuidad escenarios,
        ServicioAuditoria auditoria, IConfiguration config)
    {
        _continuidad = continuidad; _conciliacion = conciliacion; _escenarios = escenarios; _auditoria = auditoria; _config = config;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!_config.GetValue<bool>("Practica:Habilitada")) context.Result = NotFound();
        base.OnActionExecuting(context);
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var vm = await _continuidad.TableroAsync(ClienteId);
        vm.Mensaje = TempData["Mensaje"] as string;
        return View(vm);
    }

    [HttpPost("escenario")]
    public async Task<IActionResult> Escenario(Escenario escenario)
    {
        _escenarios.Armar(escenario);
        await _auditoria.RegistrarAsync(Models.TiposEvento.EscenarioActivado, $"Escenario armado para la siguiente operación nueva: {EscenariosContinuidad.Nombre(escenario)}.", "Armado", ClienteId);
        TempData["Mensaje"] = $"Escenario armado: {EscenariosContinuidad.Nombre(escenario)}. Se aplicará a la siguiente operación nueva.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("conciliar")]
    public async Task<IActionResult> Conciliar(string referencia, ResultadoExterno resultado)
    {
        var r = await _conciliacion.ConciliarAsync(ClienteId, referencia, resultado);
        TempData["Mensaje"] = r.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("restablecer")]
    public async Task<IActionResult> Restablecer([FromServices] AppDbContext db)
    {
        _escenarios.Armar(Services.Escenario.Normal);
        await DatosIniciales.RestablecerAsync(db);
        // Los perfiles se recrean con un nuevo sello: hay que volver a ingresar.
        TempData["Mensaje"] = "Datos de la demostración restablecidos. Vuelve a ingresar con DEMO-BCP.";
        return RedirectToAction("Index", "Acceso");
    }
}
