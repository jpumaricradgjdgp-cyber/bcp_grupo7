using BCP.Academico.Models;
using BCP.Academico.Services;
using BCP.Academico.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BCP.Academico.Controllers;

/// <summary>
/// Vista Razor → TransferenciasController → ServicioTransferencias → AppDbContext/SQLite → constancia.
/// El controlador valida sesión y forma; las reglas de negocio viven en el servicio.
/// </summary>
[Route("banca/enviar")]
public class TransferenciasController : BancaControllerBase
{
    private readonly ServicioTransferencias _transferencias;
    public TransferenciasController(ServicioTransferencias transferencias) => _transferencias = transferencias;

    [HttpGet("")]
    public async Task<IActionResult> Nueva(string? canal)
    {
        var f = new EnvioFormulario
        {
            Canal = Canales.EsValido(canal) ? canal! : Canales.Bcp,
            ClaveIdempotencia = ServicioTransferencias.NuevaClave()   // una clave por envío lógico
        };
        return View(await ArmarAsync(f));
    }

    /// <summary>Continuar: valida en el servidor y muestra la revisión en el diálogo.</summary>
    [HttpPost("revisar")]
    public async Task<IActionResult> Revisar(EnvioFormulario formulario)
    {
        if (string.IsNullOrEmpty(formulario.ClaveIdempotencia)) formulario.ClaveIdempotencia = ServicioTransferencias.NuevaClave();
        var vm = await ArmarAsync(formulario);
        var (resumen, error) = await _transferencias.PrepararRevisionAsync(ClienteId, formulario);
        if (error != null)
        {
            vm.Error = error.Mensaje; vm.CampoError = error.Campo;
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        }
        vm.Revision = resumen;
        return View("Nueva", vm);
    }

    /// <summary>Confirmar envío: única acción que ordena el registro. Idempotente por clave.</summary>
    [HttpPost("confirmar")]
    public async Task<IActionResult> Confirmar(EnvioFormulario formulario, bool reintento = false)
    {
        var r = await _transferencias.ConfirmarAsync(ClienteId, formulario);
        if (QuiereJson())
            return new JsonResult(ComoJson(r)) { StatusCode = r.Tipo is TipoResultadoEnvio.Invalida or TipoResultadoEnvio.Conflicto ? 409 : 200 };

        switch (r.Tipo)
        {
            case TipoResultadoEnvio.Registrada:
                return RedirectToAction("Constancia", "Historial", new { referencia = r.Transferencia!.Referencia, envio = 1 });
            case TipoResultadoEnvio.Reintento:
                return RedirectToAction("Constancia", "Historial", new { referencia = r.Transferencia!.Referencia, reintento = 1 });
            case TipoResultadoEnvio.RespuestaPerdida:
            {
                // La operación quedó guardada, pero simulamos que la respuesta no llegó: no se muestra la referencia.
                Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                var (resumen, _) = await _transferencias.PrepararRevisionAsync(ClienteId, formulario);
                return View("SinRespuesta", new SinRespuestaViewModel
                {
                    Formulario = formulario,
                    Resumen = resumen ?? new ResumenEnvio { Canal = formulario.Canal, ImporteCentimos = r.Transferencia!.ImporteCentimos, Destinatario = r.Transferencia.DestinatarioNombre }
                });
            }
            case TipoResultadoEnvio.FallaAntesDeGuardar:
            {
                var vm = await ArmarAsync(formulario);
                vm.Error = r.Mensaje + " Tus saldos no cambiaron. Puedes volver a intentarlo.";
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return View("Nueva", vm);
            }
            default:
            {
                var vm = await ArmarAsync(formulario);
                vm.Error = r.Mensaje; vm.CampoError = r.Campo;
                Response.StatusCode = r.Tipo == TipoResultadoEnvio.Conflicto ? StatusCodes.Status409Conflict : StatusCodes.Status422UnprocessableEntity;
                return View("Nueva", vm);
            }
        }
    }

    private bool QuiereJson() => Request.Headers.Accept.Any(a => a != null && a.Contains("application/json"));

    private static object ComoJson(ResultadoEnvio r) => new
    {
        resultado = r.Tipo.ToString(),
        referencia = r.Tipo == TipoResultadoEnvio.RespuestaPerdida ? null : r.Transferencia?.Referencia,
        estado = r.Transferencia?.Estado,
        mensaje = r.Mensaje
    };

    private async Task<EnvioViewModel> ArmarAsync(EnvioFormulario f) => new()
    {
        Formulario = f,
        Contactos = await _transferencias.ContactosAsync(ClienteId),
        SaldoDisponibleCentimos = (await _transferencias.CuentaOrigenAsync(ClienteId)).SaldoCentimos
    };
}
