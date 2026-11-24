using Microsoft.AspNetCore.Mvc;

namespace BCP.Academico.Controllers;

/// <summary>Portal público del laboratorio (recorridos complementarios).</summary>
public class InicioController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View();

    [HttpGet("/prestamos/capital-de-trabajo")]
    public IActionResult Prestamo() => View();

    [HttpGet("/negocios/pos")]
    public IActionResult Pos() => View();

    [HttpGet("/ayuda/bloquear-tarjeta")]
    public IActionResult Bloqueo() => View();

    [HttpGet("/ayuda")]
    public IActionResult Ayuda() => View();

    [HttpGet("/productos")] public IActionResult Productos() => View("Info", "products");
    [HttpGet("/negocios")] public IActionResult Negocios() => View("Info", "business");
    [HttpGet("/productos/ahorros")] public IActionResult Ahorros() => View("Info", "savings");
    [HttpGet("/productos/abrir-cuenta")] public IActionResult AbrirCuenta() => View("Info", "opening");
    [HttpGet("/beneficios")] public IActionResult Beneficios() => View("Info", "benefits");
    [HttpGet("/prototipo")] public IActionResult Prototipo() => View("Info", "about");

    // Acepta cualquier verbo: las páginas de estado se re-ejecutan con el método original (p. ej. un POST rechazado).
    [Route("/estado/{codigo:int}")]
    [IgnoreAntiforgeryToken]
    public IActionResult Estado(int codigo)
    {
        Response.StatusCode = codigo;
        return View(codigo switch
        {
            403 => "~/Views/Shared/Denegado.cshtml",
            404 => "~/Views/Shared/NoEncontrado.cshtml",
            _ => "~/Views/Shared/Error.cshtml"
        });
    }

    [Route("/error")]
    [IgnoreAntiforgeryToken]
    public IActionResult Error() => View("~/Views/Shared/Error.cshtml");
}
