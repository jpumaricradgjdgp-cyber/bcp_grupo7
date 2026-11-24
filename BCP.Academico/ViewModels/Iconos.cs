using Microsoft.AspNetCore.Html;

namespace BCP.Academico.ViewModels;

/// <summary>Íconos SVG de línea reutilizados de la web del proyecto.</summary>
public static class Iconos
{
    private static readonly Dictionary<string, string> Trazos = new()
    {
        ["loan"] = "<path d=\"M3 7h18v12H3zM3 10c2 0 3-1 3-3m12 0c0 2 1 3 3 3M3 16c2 0 3 1 3 3m12 0c0-2 1-3 3-3\"/><circle cx=\"12\" cy=\"13\" r=\"2.5\"/>",
        ["pos"] = "<rect x=\"6\" y=\"2.5\" width=\"12\" height=\"19\" rx=\"2.5\"/><path d=\"M9 6h6v5H9zM9 15h.01M12 15h.01M15 15h.01M9 18h.01M12 18h.01M15 18h.01\"/>",
        ["savings"] = "<path d=\"M4 9.5c1.9-2.3 4.1-3.4 7.2-3.4 1.2 0 2.3.2 3.3.6l3.5-2v4.1c1 .8 1.7 1.7 2 2.7l2 .6V16h-3l-1.6 2v2h-3v-1.4H9V20H6v-3C4 15.8 3 14 3 12.4c0-1.1.3-2 .9-2.9zM1.6 9.7c.4 1.2 1.1 1.6 1.6 1.7M8 9h4m4.4 3.3h.01\"/>",
        ["lock"] = "<rect x=\"4\" y=\"10\" width=\"16\" height=\"11\" rx=\"2.5\"/><path d=\"M8 10V7a4 4 0 1 1 8 0v3m-4 4v3\"/>",
        ["help"] = "<path d=\"M20 11.5a8 8 0 0 1-8 8c-1 0-2-.2-3-.5l-4 1 1-3.8a8 8 0 1 1 14-4.7z\"/><path d=\"M10 10a2 2 0 1 1 3 1.7c-.8.5-1 1-1 1.8m0 2.7h.01\"/>",
        ["info"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 11.5V16m0-8h.01\"/>",
        ["check"] = "<path d=\"m5 12 4 4L19 6\"/>",
        ["card"] = "<rect x=\"2.5\" y=\"5.5\" width=\"19\" height=\"13\" rx=\"2.5\"/><path d=\"M3 10h18M6.5 15H11\"/>",
        ["user"] = "<circle cx=\"12\" cy=\"7.5\" r=\"3.5\"/><path d=\"M4.5 21v-1.5a5 5 0 0 1 5-5h5a5 5 0 0 1 5 5V21\"/>",
        ["shop"] = "<path d=\"M4 10v10h16V10M3 10l2-6h14l2 6M3 10c0 2 3 3 4.5 0 1.5 3 3 3 4.5 0 1.5 3 3 3 4.5 0 1.5 3 4.5 2 4.5 0M10 20v-5h4v5\"/>",
        ["clock"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 2\"/>",
        ["alert"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v6m0 4h.01\"/>",
        ["search"] = "<circle cx=\"10.5\" cy=\"10.5\" r=\"6.5\"/><path d=\"m16 16 4.5 4.5\"/>",
        ["shield"] = "<path d=\"M12 3 4.5 6v5.5c0 4.6 3.1 8.2 7.5 9.5 4.4-1.3 7.5-4.9 7.5-9.5V6z\"/><path d=\"m9 12 2 2 4-4\"/>",
        ["refresh"] = "<path d=\"M20 11a8 8 0 0 0-14.6-4.5M4 4v4h4M4 13a8 8 0 0 0 14.6 4.5M20 20v-4h-4\"/>"
    };

    public static IHtmlContent Svg(string clave, int tam = 24) => new HtmlString(
        $"<svg aria-hidden=\"true\" width=\"{tam}\" height=\"{tam}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Trazos[clave]}</svg>");
}
