using System.Security.Claims;
using BCP.Academico.Services;
using BCP.Academico.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCP.Academico.Controllers;

[Route("acceso")]
public class AccesoController : Controller
{
    private readonly ServicioAcceso _acceso;
    public AccesoController(ServicioAcceso acceso) => _acceso = acceso;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Cuentas");
        return View(new AccesoViewModel { Formulario = new AccesoFormulario { ReturnUrl = returnUrl }, Perfiles = await _acceso.PerfilesAsync() });
    }

    [HttpPost("")]
    public async Task<IActionResult> Index(AccesoFormulario formulario)
    {
        var cliente = ModelState.IsValid ? await _acceso.ValidarAsync(formulario.Perfil, formulario.Codigo) : null;
        if (cliente == null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            formulario.Codigo = "";
            return View(new AccesoViewModel
            {
                Formulario = formulario, Perfiles = await _acceso.PerfilesAsync(),
                Error = "Escribe el código ficticio DEMO-BCP y vuelve a intentarlo."
            });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, cliente.Id.ToString()),
            new(ClaimTypes.Name, cliente.Nombre),
            new("sello", cliente.SelloSesion)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        if (!string.IsNullOrEmpty(formulario.ReturnUrl) && Url.IsLocalUrl(formulario.ReturnUrl)) return LocalRedirect(formulario.ReturnUrl);
        return RedirectToAction("Index", "Cuentas");
    }

    [Authorize]
    [HttpPost("salir")]
    public async Task<IActionResult> Salir()
    {
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) await _acceso.CerrarSesionAsync(id);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Mensaje"] = "Cerraste tu sesión de prueba. Para volver a la banca, ingresa de nuevo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("denegado")]
    public IActionResult Denegado()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("~/Views/Shared/Denegado.cshtml");
    }
}
