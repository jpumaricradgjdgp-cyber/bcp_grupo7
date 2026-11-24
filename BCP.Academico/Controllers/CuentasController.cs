using BCP.Academico.Services;
using Microsoft.AspNetCore.Mvc;

namespace BCP.Academico.Controllers;

[Route("banca")]
public class CuentasController : BancaControllerBase
{
    private readonly ServicioCuentas _cuentas;
    public CuentasController(ServicioCuentas cuentas) => _cuentas = cuentas;

    /// <summary>Posición consolidada: saldo y últimos movimientos desde SQLite.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await _cuentas.PanelAsync(ClienteId));

    /// <summary>Movimientos de una cuenta. Solo el titular puede consultarlos.</summary>
    [HttpGet("cuentas/{cuentaId:int}")]
    public async Task<IActionResult> Movimientos(int cuentaId)
    {
        var (acceso, cuenta, items) = await _cuentas.MovimientosAsync(ClienteId, cuentaId);
        if (acceso == AccesoRecurso.NoExiste) return NoEncontrado();
        if (acceso == AccesoRecurso.Ajeno) return await DenegarAsync($"la cuenta {cuentaId}");
        ViewBag.Cuenta = cuenta;
        return View(items);
    }

    [HttpGet("perfil")]
    public async Task<IActionResult> Perfil() => View(await _cuentas.PerfilAsync(ClienteId));
}
