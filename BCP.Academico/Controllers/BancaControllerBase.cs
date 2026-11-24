using System.Security.Claims;
using BCP.Academico.Models;
using BCP.Academico.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCP.Academico.Controllers;

/// <summary>Base de los controladores de banca: exige sesión y expone el cliente autenticado.</summary>
[Authorize]
public abstract class BancaControllerBase : Controller
{
    protected int ClienteId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Respuesta 403 con vista propia y registro en la bitácora.</summary>
    protected async Task<IActionResult> DenegarAsync(string recurso)
    {
        var auditoria = HttpContext.RequestServices.GetRequiredService<ServicioAuditoria>();
        await auditoria.RegistrarAsync(TiposEvento.AccesoDenegado, $"Intento de acceso a {recurso} de otro perfil.", "Denegado (403)", ClienteId);
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("~/Views/Shared/Denegado.cshtml");
    }

    protected IActionResult NoEncontrado()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("~/Views/Shared/NoEncontrado.cshtml");
    }
}
