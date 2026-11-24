using BCP.Academico.Services;
using BCP.Academico.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BCP.Academico.Controllers;

[Route("banca")]
public class HistorialController : BancaControllerBase
{
    private readonly ServicioCuentas _cuentas;
    public HistorialController(ServicioCuentas cuentas) => _cuentas = cuentas;

    [HttpGet("historial")]
    public async Task<IActionResult> Index([FromQuery] FiltroHistorial filtro) => View(await _cuentas.HistorialAsync(ClienteId, filtro));

    /// <summary>Constancia de operación. Un perfil no puede ver la constancia de otro.</summary>
    [HttpGet("constancia/{referencia}")]
    public async Task<IActionResult> Constancia(string referencia, int? envio, int? reintento)
    {
        var (acceso, vm) = await _cuentas.ConstanciaAsync(ClienteId, referencia);
        if (acceso == AccesoRecurso.NoExiste) return NoEncontrado();
        if (acceso == AccesoRecurso.Ajeno) return await DenegarAsync($"la constancia {referencia}");
        vm!.EsNuevoEnvio = envio == 1;
        vm.EsReintento = reintento == 1;
        return View(vm);
    }
}
