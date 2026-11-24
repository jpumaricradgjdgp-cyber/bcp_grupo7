using System.Security.Claims;
using BCP.Academico.Data;
using BCP.Academico.Data.Seed;
using BCP.Academico.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Persistencia: EF Core + SQLite ----------
var rutaDb = builder.Configuration["BaseDatos:Ruta"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "bcp_academico.db");
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(rutaDb))!);
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={rutaDb};Default Timeout=30;Pooling=False"));

// ---------- Servicios de casos de uso ----------
builder.Services.AddSingleton<EscenariosContinuidad>();
builder.Services.AddSingleton<IAdaptadorCanal, AdaptadorBcpLocal>();
builder.Services.AddSingleton<IAdaptadorCanal, AdaptadorYapeLocal>();
builder.Services.AddSingleton<SelectorCanal>();
builder.Services.AddScoped<ServicioAuditoria>();
builder.Services.AddScoped<ServicioAcceso>();
builder.Services.AddScoped<ServicioCuentas>();
builder.Services.AddScoped<ServicioTransferencias>();
builder.Services.AddScoped<ServicioConciliacion>();
builder.Services.AddScoped<ServicioContinuidad>();

// ---------- Autenticación por cookie (entorno local) ----------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/acceso";
        o.LogoutPath = "/acceso/salir";
        o.AccessDeniedPath = "/acceso/denegado";
        o.Cookie.Name = "lab_bcp_sesion";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        o.SlidingExpiration = true;
        o.Events.OnValidatePrincipal = async ctx =>
        {
            // Si el sello guardado cambió (cierre de sesión), la cookie deja de ser válida.
            var id = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var sello = ctx.Principal?.FindFirstValue("sello");
            var acceso = ctx.HttpContext.RequestServices.GetRequiredService<ServicioAcceso>();
            if (!int.TryParse(id, out var clienteId) || !await acceso.SelloVigenteAsync(clienteId, sello))
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews(o =>
{
    // Toda acción POST exige el token antifalsificación.
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
// Escribe tildes, ñ y "•" tal cual en el HTML (sin entidades numéricas).
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));
builder.Services.AddAntiforgery(o => { o.Cookie.Name = "lab_bcp_af"; o.FormFieldName = "__RequestVerificationToken"; });

var app = builder.Build();

// ---------- Migraciones, datos iniciales y registro de arranque ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    if (args.Contains("--reiniciar-demo"))
    {
        await DatosIniciales.RestablecerAsync(db);
        Console.WriteLine("Datos de la demostración restablecidos: origen S/ 5,320.00 y destino S/ 420.00.");
    }
    else
    {
        await DatosIniciales.AplicarAsync(db);
    }
    await ServicioContinuidad.RegistrarInicioAsync(db);
    if (args.Contains("--solo-preparar")) return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}
app.UseStatusCodePagesWithReExecute("/estado/{0}");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
