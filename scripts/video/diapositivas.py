"""Diapositivas HTML (1920x1080) para el video y las imágenes de la sustentación."""
import html
import json
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[2]
IMG = (RAIZ / "BCP.Academico" / "wwwroot" / "img").as_uri()
DIAG = (RAIZ / "docs" / "diagramas").as_uri()
EVID = (RAIZ / "docs" / "evidencias").as_uri()
SALIDA = RAIZ / "video" / "_diapositivas"

CSS = """
*{box-sizing:border-box} body{margin:0;width:1920px;height:1080px;overflow:hidden;font-family:"Segoe UI",system-ui,sans-serif;color:#102747;background:#fff}
.s{position:relative;width:1920px;height:1080px;padding:80px 96px}
.tag{display:inline-block;background:#eaf5ff;color:#1451a7;font-weight:700;letter-spacing:.08em;font-size:20px;padding:8px 18px;border-radius:999px}
h1{font-size:76px;line-height:1.04;margin:28px 0 20px;letter-spacing:-.02em} h2{font-size:52px;margin:0 0 14px;letter-spacing:-.01em}
p.lead{font-size:30px;color:#4b617b;margin:0 0 26px;max-width:980px;line-height:1.35}
.pie{position:absolute;left:96px;right:96px;bottom:40px;display:flex;justify-content:space-between;font-size:20px;color:#4b617b;border-top:1px solid #d3deeb;padding-top:16px}
.logo{display:inline-block;width:180px;height:55px;background:url('IMG/bcp-logo-horizontal.png') no-repeat -49px -41px/299px 133px;mix-blend-mode:multiply}
.foto{position:absolute;right:96px;top:150px;width:760px;height:700px;border-radius:40px;overflow:hidden;box-shadow:0 30px 60px rgb(16 39 71/18%)}
.foto img{width:100%;height:100%;object-fit:cover}
.foto small{position:absolute;left:20px;bottom:18px;background:#fffd;padding:6px 12px;border-radius:10px;font-size:16px;color:#4b617b}
.datos{font-size:24px;line-height:1.6;color:#263d58;max-width:880px} .datos b{color:#102747}
.naranja{color:#ff8a24}
.dos{display:grid;grid-template-columns:1fr 1fr;gap:40px;margin-top:10px}
.col{border:2px solid #d3deeb;border-radius:28px;padding:22px;text-align:center;transition:all .4s}
.col img{height:640px} .col .nota{font-size:23px;text-align:left;margin-top:12px;line-height:1.35}
.col.activa{border-color:#ff8a24;box-shadow:0 0 0 8px #ff8a2433}
.full{display:flex;justify-content:center;align-items:center;height:1000px;margin-top:-40px} .full img{width:1720px}
.term{background:#0f1b2d;color:#d7e3f4;border-radius:24px;padding:36px 44px;font-family:Consolas,"Cascadia Mono",monospace;font-size:25px;line-height:1.5;height:760px;overflow:hidden;box-shadow:0 30px 60px rgb(16 39 71/25%)}
.term .cmd{color:#ffd29e} .term .ok{color:#8ee3a8} .term .dim{color:#8aa0bd}
table{width:100%;border-collapse:collapse;font-size:22px} th,td{text-align:left;padding:11px 14px;border-bottom:1px solid #d3deeb;vertical-align:top}
th{color:#4b617b;font-size:18px;text-transform:uppercase;letter-spacing:.04em}
.pasa{display:inline-block;background:#ecf8ef;color:#14532d;font-weight:700;border-radius:999px;padding:3px 14px}
.falla{display:inline-block;background:#fff1ef;color:#9b231b;font-weight:700;border-radius:999px;padding:3px 14px}
ul.lista{font-size:30px;line-height:1.55;padding-left:36px;margin:0} ul.lista li{margin-bottom:6px}
.caja{background:#fff3e6;border:2px solid #ff8a24;border-radius:24px;padding:26px 32px;font-size:26px;line-height:1.5}
.capturas{display:grid;grid-template-columns:1fr 1fr;gap:36px;margin-top:20px} .capturas figure{margin:0} .capturas img{width:100%;border-radius:20px;border:1px solid #d3deeb;box-shadow:0 20px 40px rgb(16 39 71/12%)}
.capturas figcaption{font-size:22px;color:#4b617b;margin-top:10px}
""".replace("IMG", IMG)


def _pagina(cuerpo: str, titulo: str) -> str:
    return f"<!doctype html><html lang='es-PE'><head><meta charset='utf-8'><title>{titulo}</title><style>{CSS}</style></head><body>{cuerpo}</body></html>"


def _pie(texto="Laboratorio BCP · Grupo 7 · Planeamiento Estratégico de las TIC") -> str:
    return f"<div class='pie'><span>{texto}</span><span>Proyecto académico · datos ficticios</span></div>"


def portada() -> str:
    return _pagina(f"""<div class='s'><span class='logo'></span>
      <div style='margin-top:40px'><span class='tag'>CASO BCP · PROYECTO ACADÉMICO</span>
      <h1>MVP de consulta<br>y transferencia <span class='naranja'>segura</span></h1>
      <p class='lead'>Plan estratégico de sistemas de información para la modernización gradual de servicios del BCP</p>
      <div class='datos'><b>Grupo 7</b> · Sección 20750 · Universidad Tecnológica del Perú<br>
      Castelo Quispe · Condori Gómez · Gonzales Pumaricra · Tafur Cortez · Ventura Ventura<br>
      Docente: Gustavo Henry Pachas Quispe · Noviembre de 2026<br>
      <b>ASP.NET Core MVC · Razor · EF Core · SQLite</b></div></div>
      <figure class='foto'><img src='{IMG}/negocio.webp' alt=''><small>Recurso gráfico generado</small></figure>{_pie()}</div>""", "Portada")


def as_is_to_be(activa: str = "") -> str:
    a = "activa" if activa == "asis" else ""
    b = "activa" if activa == "tobe" else ""
    return _pagina(f"""<div class='s' style='padding-top:50px'><h2>Del proceso actual al propuesto</h2>
      <div class='dos'>
        <div class='col {a}' id='asis'><img src='{DIAG}/as_is_proceso.svg' alt=''>
          <div class='nota'><b>Problema:</b> si no hay respuesta, no existe una clave estable; un reintento puede duplicar el cargo y el resultado incierto depende de una revisión manual.</div></div>
        <div class='col {b}' id='tobe'><img src='{DIAG}/to_be_proceso.svg' alt=''>
          <div class='nota'><b>Cambio:</b> clave de idempotencia, persistencia atómica de operación y movimientos, contingencia y una sola compensación ante rechazo confirmado.</div></div>
      </div></div>""", "As-Is y To-Be")


def diagrama(nombre: str, titulo: str) -> str:
    return _pagina(f"<div class='s'><div class='full'><img src='{DIAG}/{nombre}' alt=''></div></div>", titulo)


def terminal(lineas: list[tuple[str, str]], titulo: str = "Terminal") -> str:
    filas = "".join(f"<div class='{c}'>{html.escape(t)}</div>" for c, t in lineas)
    return _pagina(f"""<div class='s' style='padding-top:60px'><h2>{html.escape(titulo)}</h2>
      <div class='term'>{filas}</div>{_pie('Salida real del servidor durante la grabación')}</div>""", titulo)


def acceso_y_ayuda() -> str:
    return _pagina(f"""<div class='s' style='padding-top:60px'><h2>Acceso y ayuda de la demo</h2>
      <div class='capturas'><figure><img src='{EVID}/02_acceso.png' alt=''><figcaption>Acceso académico con perfil ficticio y código DEMO-BCP (ilustración: cuy futbolista).</figcaption></figure>
      <figure><img src='{EVID}/03_ayuda.png' alt=''><figcaption>Preguntas frecuentes con el cuy guía (recurso gráfico generado).</figcaption></figure></div>{_pie()}</div>""", "Acceso y ayuda")


def resultados(pruebas: list[dict], navegador: list[dict]) -> str:
    def fila(n, ok, nota):
        return f"<tr><td>{html.escape(n)}</td><td><span class='{'pasa' if ok else 'falla'}'>{'Pasó' if ok else 'Falló'}</span></td><td>{html.escape(nota)}</td></tr>"
    filas = "".join(fila(p["nombre"].split(":")[0], p["resultado"] == "Passed", p["nombre"].split(":", 1)[1].strip()) for p in pruebas)
    nav_ok = sum(1 for v in navegador if v["ok"])
    return _pagina(f"""<div class='s' style='padding-top:50px'><h2>Resultados verificados</h2>
      <p class='lead' style='font-size:24px;margin-bottom:12px'>{sum(p['resultado']=='Passed' for p in pruebas)} de {len(pruebas)} pruebas automatizadas de integración (HTTP → MVC → servicios → SQLite) · {nav_ok} de {len(navegador)} verificaciones en el navegador durante la grabación</p>
      <table><thead><tr><th>Prueba</th><th>Resultado</th><th>Qué comprueba</th></tr></thead><tbody>{filas}</tbody></table>
      {_pie('Limitaciones: entorno local, datos ficticios, adaptadores BCP/Yape simulados; Azure, AKS y CDC son propuesta del PESI')}</div>""", "Resultados")


def cierre() -> str:
    return _pagina(f"""<div class='s'><span class='logo'></span><h2 style='margin-top:30px'>Qué comprobamos</h2>
      <ul class='lista'><li>Saldo inicial S/ 5,320.00 → envío de S/ 50.00 → S/ 5,270.00 (destino S/ 420.00 → S/ 470.00)</li>
      <li>Repetir el envío devuelve la misma referencia: un débito y un abono</li>
      <li>Resultado desconocido queda <b>En verificación</b>; el rechazo confirmado crea un único ajuste</li>
      <li>Tras reiniciar el servidor, saldo, historial y constancia siguen disponibles</li></ul>
      <div class='caja' style='margin-top:34px'><b>Para repetirlo:</b> abre <b>README.md</b> · <code>dotnet run --project BCP.Academico -- --reiniciar-demo</code> · http://localhost:5080 · código <b>DEMO-BCP</b></div>
      {_pie()}</div>""", "Cierre")


def escribir(nombre: str, contenido: str) -> Path:
    SALIDA.mkdir(parents=True, exist_ok=True)
    ruta = SALIDA / f"{nombre}.html"
    ruta.write_text(contenido, encoding="utf-8")
    return ruta
