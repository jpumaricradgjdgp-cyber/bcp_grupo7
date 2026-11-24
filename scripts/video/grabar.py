"""Ejecuta el MVP real, verifica el recorrido en Edge y graba el video del tutorial.

Pasos:
 1. Arranca el servidor con --reiniciar-demo y hace una verificación previa (teclado, diálogo,
    validación de saldo, cierre de sesión, acceso ajeno) sin grabar.
 2. Reinicia los datos y graba, en una sola toma, el recorrido completo sincronizado con la narración.
 3. Guarda la grabación, la línea de tiempo, las capturas numeradas y los resultados observados.
Uso: python -I scripts/video/grabar.py
"""
import json
import os
import signal
import subprocess
import sys
import threading
import time
import urllib.request
import wave
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import diapositivas as D  # noqa: E402
from guion import TRAMOS  # noqa: E402
from playwright.sync_api import sync_playwright  # noqa: E402

RAIZ = Path(__file__).resolve().parents[2]
PROYECTO = RAIZ / "BCP.Academico"
EVID = RAIZ / "docs" / "evidencias"
VIDEO = RAIZ / "video"
AUDIO = VIDEO / "audio"
URL = "http://localhost:5080"
DOTNET_ROOT = Path.home() / ".dotnet"  # copia del SDK fuera de AppData (Python de la Store virtualiza AppData)
PAUSA = 0.55  # silencio entre tramos (s)

EVID.mkdir(parents=True, exist_ok=True)
VERIFICACIONES: list[dict] = []


def verificar(nombre, ok, observado):
    VERIFICACIONES.append({"verificacion": nombre, "ok": bool(ok), "observado": observado})
    print(("  ✔ " if ok else "  ✘ ") + nombre + " → " + observado)


# ---------------------------------------------------------------- servidor
class Servidor:
    def __init__(self):
        self.proc = None
        self.lineas: list[str] = []

    def iniciar(self, reiniciar: bool):
        self.lineas = []
        # Un solo proceso (la DLL compilada): Ctrl+C/Ctrl+Break detiene realmente el servidor.
        dll = Path("bin") / "Debug" / "net10.0" / "BCP.Academico.dll"
        args = [str(DOTNET_ROOT / "dotnet.exe"), str(dll)] + (["--reiniciar-demo"] if reiniciar else [])
        self.comando = " ".join(["dotnet"] + args[1:])
        if self._puerto_ocupado():
            raise RuntimeError("El puerto 5080 ya está en uso: detén el servidor anterior antes de grabar.")
        env = dict(os.environ, DOTNET_ROOT=str(DOTNET_ROOT), PATH=str(DOTNET_ROOT) + os.pathsep + os.environ["PATH"],
                   DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1", ASPNETCORE_ENVIRONMENT="Production")
        self.proc = subprocess.Popen(args, cwd=PROYECTO, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                     text=True, encoding="utf-8", errors="replace",
                                     creationflags=subprocess.CREATE_NEW_PROCESS_GROUP)
        threading.Thread(target=self._leer, daemon=True).start()
        limite = time.time() + 90
        while time.time() < limite:
            try:
                if urllib.request.urlopen(URL + "/", timeout=2).status == 200:
                    time.sleep(0.4)
                    return
            except Exception:
                time.sleep(0.4)
        raise RuntimeError("El servidor no respondió:\n" + "\n".join(self.lineas[-20:]))

    @staticmethod
    def _puerto_ocupado():
        import socket
        with socket.socket() as s:
            return s.connect_ex(("127.0.0.1", 5080)) == 0

    def _leer(self):
        for linea in self.proc.stdout:
            self.lineas.append(linea.rstrip())

    def detener(self):
        if not self.proc:
            return
        os.kill(self.proc.pid, signal.CTRL_BREAK_EVENT)  # equivalente a Ctrl+C en la terminal
        try:
            self.proc.wait(timeout=30)
        except subprocess.TimeoutExpired:
            self.proc.kill()
        for _ in range(40):
            if not self._puerto_ocupado():
                break
            time.sleep(0.25)
        else:
            raise RuntimeError("El servidor no liberó el puerto 5080")

    def lineas_terminal(self, prompt="PS ...\\BCP_MVP\\BCP.Academico>"):
        filas = [("dim", "# Equivale a: dotnet run -- --reiniciar-demo (después de compilar)" if "--reiniciar-demo" in self.comando else "# Equivale a: dotnet run (conserva los datos)"),
                 ("cmd", f"{prompt} {self.comando}")]
        for l in self.lineas:
            if not l.strip():
                continue
            clase = "ok" if any(k in l for k in ("restablecidos", "Now listening", "Application started", "Ahora", "escuchando")) else "dim"
            filas.append((clase, l))
        return filas[-22:]


# ---------------------------------------------------------------- ayudas de navegador
CURSOR_JS = r"""
(() => {
  if (window.__cursorListo) return; window.__cursorListo = true;
  let pos = [960, 540];
  try { pos = JSON.parse(sessionStorage.getItem('__cp') || '[960,540]'); } catch {}
  const crear = () => {
    const c = document.createElement('div'); c.id = '__cursor';
    c.style.cssText = 'position:fixed;width:30px;height:30px;z-index:2147483647;pointer-events:none;margin:-3px 0 0 -5px';
    c.style.left = pos[0] + 'px'; c.style.top = pos[1] + 'px';
    c.innerHTML = '<svg width="30" height="30" viewBox="0 0 24 24"><path d="M4 2l14 9-6.5 1.2L15 20l-3 1.4-3.4-7.6L4 18z" fill="#fff" stroke="#102747" stroke-width="1.6" stroke-linejoin="round"/></svg>';
    document.documentElement.appendChild(c);
    const anfitrion = () => document.querySelector('dialog[open]') || document.documentElement;
    document.addEventListener('mousemove', e => {
      const h = anfitrion(); if (c.parentNode !== h) h.appendChild(c);
      c.style.left = e.clientX + 'px'; c.style.top = e.clientY + 'px';
      try { sessionStorage.setItem('__cp', JSON.stringify([e.clientX, e.clientY])); } catch {}
    }, true);
    document.addEventListener('mousedown', e => {
      const r = document.createElement('div');
      r.style.cssText = `position:fixed;left:${e.clientX - 22}px;top:${e.clientY - 22}px;width:44px;height:44px;border-radius:50%;border:3px solid #ff8a24;z-index:2147483646;pointer-events:none`;
      anfitrion().appendChild(r);
      r.animate([{ transform: 'scale(.4)', opacity: 1 }, { transform: 'scale(1.4)', opacity: 0 }], { duration: 600 }).onfinish = () => r.remove();
    }, true);
  };
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', crear); else crear();
  window.__resaltar = (el, ms = 2800) => {
    if (!el) return false;
    el.animate([{ outline: '0px solid rgba(255,138,36,0)', outlineOffset: '2px' },
                { outline: '6px solid rgba(255,138,36,.9)', outlineOffset: '4px' },
                { outline: '6px solid rgba(255,138,36,.9)', outlineOffset: '4px' },
                { outline: '0px solid rgba(255,138,36,0)', outlineOffset: '2px' }], { duration: ms });
    return true;
  };
})();
"""


class Guia:
    """Acciones visibles: mover el cursor, hacer clic, escribir y resaltar."""

    def __init__(self, page):
        self.p = page

    def mover(self, loc, pasos=28):
        loc.scroll_into_view_if_needed()
        self.p.wait_for_timeout(250)
        b = loc.bounding_box()
        x, y = b["x"] + b["width"] / 2, b["y"] + b["height"] / 2
        self.p.mouse.move(x, y, steps=pasos)
        return x, y

    def clic(self, loc, espera=350):
        x, y = self.mover(loc)
        self.p.wait_for_timeout(espera)
        self.p.mouse.click(x, y)

    def escribir(self, loc, texto, retardo=95):
        self.clic(loc)
        self.p.keyboard.type(texto, delay=retardo)

    def resaltar(self, loc, ms=2800):
        loc.scroll_into_view_if_needed()
        loc.evaluate(f"el => window.__resaltar(el, {ms})")

    def desplazar(self, loc):
        loc.evaluate("el => el.scrollIntoView({block: 'center', behavior: 'smooth'})")
        self.p.wait_for_timeout(700)

    def captura(self, nombre):
        self.p.screenshot(path=str(EVID / nombre))


def duraciones():
    d = {}
    for i, _, _ in TRAMOS:
        with wave.open(str(AUDIO / f"{i}.wav")) as w:
            d[i] = w.getnframes() / w.getframerate()
    return d


# ---------------------------------------------------------------- verificación previa (sin grabar)
def verificacion_previa(browser):
    print("Verificación previa en Edge (sin grabar)…")
    ctx = browser.new_context(viewport={"width": 1600, "height": 1000})
    p = ctx.new_page()
    p.goto(URL + "/acceso"); p.wait_for_load_state("networkidle")
    p.screenshot(path=str(EVID / "02_acceso.png"))
    p.goto(URL + "/ayuda"); p.locator("details").first.evaluate("d => d.open = true")
    p.screenshot(path=str(EVID / "03_ayuda.png"))

    # Acceso solo con teclado
    p.goto(URL + "/acceso")
    p.locator("#demo-code").focus(); p.keyboard.type("DEMO-BCP"); p.keyboard.press("Enter")
    p.wait_for_url("**/banca")
    verificar("Ingreso con teclado (Enter)", p.url.endswith("/banca"), p.url)
    saldo = p.locator("[data-testid=saldo]").inner_text()
    verificar("Saldo inicial desde SQLite", saldo == "S/ 5,320.00", saldo)

    # Diálogo: foco al abrir, Escape/Editar devuelven el foco y conservan los datos
    p.goto(URL + "/banca/enviar")
    p.select_option("#bank-target", label="Elena Rojas")
    p.locator("#bank-amount").focus(); p.keyboard.type("50"); p.keyboard.press("Enter")
    p.wait_for_selector("dialog[open]")
    foco = p.evaluate("document.activeElement.id")
    verificar("Foco entra al diálogo al abrir", foco == "send-title", f"activeElement = #{foco}")
    p.keyboard.press("Escape"); p.wait_for_timeout(300)
    foco = p.evaluate("document.activeElement.id"); monto = p.input_value("#bank-amount")
    verificar("Escape cierra y devuelve el foco a Continuar", foco == "btn-continuar" and monto == "50", f"#{foco}, monto={monto}")
    p.keyboard.press("Enter"); p.wait_for_selector("dialog[open]")
    p.click("[data-cerrar-dialogo]"); p.wait_for_timeout(300)
    foco = p.evaluate("document.activeElement.id"); destino = p.locator("#bank-target option:checked").inner_text()
    verificar("Editar conserva los datos y devuelve el foco", foco == "btn-continuar" and destino == "Elena Rojas", f"#{foco}, destino={destino}")

    # Validación de saldo en el servidor
    p.fill("#bank-amount", "9999"); p.click("#btn-continuar"); p.wait_for_load_state("networkidle")
    msg = p.locator("[data-testid=error-envio]").inner_text().strip()
    verificar("Monto mayor al saldo se rechaza con motivo", "supera tu saldo" in msg, msg)
    p.screenshot(path=str(EVID / "22_validacion_saldo.png"))

    # Cierre de sesión
    p.goto(URL + "/banca"); p.click(".bank-logout button"); p.wait_for_url("**/acceso")
    p.goto(URL + "/banca")
    verificar("Tras cerrar sesión, /banca exige ingresar", "/acceso" in p.url, p.url)

    # Acceso indebido: Elena intenta ver una constancia de Mariana
    p.select_option("#profile", label="Elena Rojas"); p.fill("#demo-code", "DEMO-BCP"); p.click("#login-form button[type=submit]")
    p.wait_for_url("**/banca")
    resp = p.goto(URL + "/banca/constancia/LAB-2026-501183")
    verificar("Constancia ajena denegada", resp.status == 403, f"HTTP {resp.status}")
    p.screenshot(path=str(EVID / "21_acceso_denegado.png"))
    ctx.close()


# ---------------------------------------------------------------- grabación principal
def grabar(browser, servidor: Servidor):
    dur = duraciones()
    linea: list[dict] = []
    ctx = browser.new_context(viewport={"width": 1920, "height": 1080}, record_video_dir=str(VIDEO / "_crudo"),
                              record_video_size={"width": 1920, "height": 1080})
    ctx.add_init_script(CURSOR_JS)
    p = ctx.new_page()
    t0 = time.monotonic()
    g = Guia(p)

    class tramo:
        def __init__(self, ident):
            self.id = ident

        def __enter__(self):
            self.ini = time.monotonic() - t0
            return self

        def __exit__(self, *exc):
            if exc[0]:
                return False
            restante = self.ini + dur[self.id] + PAUSA - (time.monotonic() - t0)
            if restante > 0:
                p.wait_for_timeout(int(restante * 1000))
            linea.append({"id": self.id, "inicio": round(self.ini, 3), "fin": round(time.monotonic() - t0, 3)})
            print(f"  {self.id} {self.ini:7.2f}s → {time.monotonic() - t0:7.2f}s")

    def diapositiva(nombre, html):
        p.goto(D.escribir(nombre, html).as_uri()); p.wait_for_load_state("load")

    # S1 Portada
    diapositiva("portada", D.portada())
    with tramo("b01"):
        g.captura("01_portada.png")
    with tramo("b02"):
        pass

    # S2 As-Is / To-Be / MVC / To-Be empresarial
    diapositiva("asis_tobe", D.as_is_to_be())
    with tramo("b03"):
        g.captura("04_as_is_to_be.png")
        p.wait_for_timeout(1500); p.evaluate("document.getElementById('asis').classList.add('activa')")
    with tramo("b04"):
        p.evaluate("document.getElementById('asis').classList.remove('activa'); document.getElementById('tobe').classList.add('activa')")
    diapositiva("mvc", D.diagrama("arquitectura_mvc_mvp.svg", "Arquitectura MVC"))
    with tramo("b05"):
        g.captura("05_arquitectura_mvc.png")
    diapositiva("tobe_empresarial", D.diagrama("arquitectura_tobe_pesi.svg", "Arquitectura To-Be"))
    with tramo("b06"):
        g.captura("06_arquitectura_tobe_pesi.png")

    # S3 Arranque, acceso, saldo
    diapositiva("terminal_inicio", D.terminal(servidor.lineas_terminal(), "1. Iniciar el aplicativo"))
    with tramo("b07"):
        g.captura("15a_terminal_inicio.png")
    p.goto(URL + "/acceso")
    with tramo("b08"):
        g.mover(p.locator("#profile"))
        p.wait_for_timeout(600)
        g.escribir(p.locator("#demo-code"), "DEMO-BCP", 140)
        p.wait_for_timeout(500)
        g.clic(p.locator("#login-form button[type=submit]"))
        p.wait_for_url("**/banca")
    with tramo("b09"):
        g.mover(p.locator("[data-testid=saldo]"))
        g.resaltar(p.locator("[data-testid=saldo]"))
        g.captura("07_saldo_inicial.png")
        p.wait_for_timeout(2600)
        g.resaltar(p.locator("[data-testid=cuenta]"))
        verificar("Panel muestra S/ 5,320.00", p.locator("[data-testid=saldo]").inner_text() == "S/ 5,320.00", p.locator("[data-testid=saldo]").inner_text())

    # S4 Transferencia de S/ 50
    with tramo("b10"):
        g.clic(p.locator("a.bank-bcp")); p.wait_for_url("**/banca/enviar**")
        g.mover(p.locator("#bank-target")); p.wait_for_timeout(500)
        p.select_option("#bank-target", label="Elena Rojas")
        p.wait_for_timeout(700); g.resaltar(p.locator("#bank-mask"))
    with tramo("b11"):
        g.clic(p.locator("[data-preset='50']"))
        p.wait_for_timeout(500)
        g.escribir(p.locator("#bank-concept"), "Pago de prueba del curso", 70)
    with tramo("b12"):
        g.clic(p.locator("#btn-continuar")); p.wait_for_selector("dialog[open]")
        p.wait_for_timeout(900)
        g.resaltar(p.locator("dialog[open] .detail-list"), 4000)
        g.captura("08_confirmacion.png")
        g.mover(p.locator("#btn-confirmar"))
    with tramo("b13"):
        g.clic(p.locator("#btn-confirmar")); p.wait_for_url("**/banca/constancia/**")
        p.wait_for_timeout(600)
        ref50 = p.locator("[data-testid=referencia]").inner_text()
        g.mover(p.locator("[data-testid=referencia]")); g.resaltar(p.locator("[data-testid=referencia]"))
    with tramo("b14"):
        g.desplazar(p.locator("[data-testid=asientos]"))
        g.resaltar(p.locator("[data-testid=asientos]"), 4200)
        g.captura("09_constancia.png")
        p.wait_for_timeout(4200)
        g.resaltar(p.locator("[data-testid=saldo-actual]"))
        saldo_c = p.locator("[data-testid=saldo-actual]").inner_text()
        filas = p.locator("[data-testid=asientos] tbody tr").all_inner_texts()
        verificar("Confirmación: débito y abono de S/ 50.00, saldo S/ 5,270.00",
                  saldo_c == "S/ 5,270.00" and len(filas) == 2 and any("Crédito" in f and "Elena Rojas" in f for f in filas) and any("Débito" in f and "Mariana" in f for f in filas), f"{ref50} · {saldo_c} · {len(filas)} asientos")
    with tramo("b15"):
        g.clic(p.locator(".bank-side a[href='/banca/historial']")); p.wait_for_url("**/banca/historial")
        fila = p.locator(f"li[data-ref='{ref50}']")
        g.mover(fila); g.resaltar(fila)
        g.captura("10_historial.png")

    # S5 Reintento
    with tramo("b16"):
        g.clic(p.locator(f"li[data-ref='{ref50}'] a")); p.wait_for_url(f"**/{ref50}")
        g.desplazar(p.locator("[data-testid=btn-reintento]"))
        g.mover(p.locator("[data-testid=btn-reintento]")); p.wait_for_timeout(1200)
        g.clic(p.locator("[data-testid=btn-reintento]")); p.wait_for_url("**reintento=1")
    with tramo("b17"):
        p.evaluate("window.scrollTo({top:0, behavior:'smooth'})"); p.wait_for_timeout(600)
        g.resaltar(p.locator("[data-testid=aviso-exito]"))
        g.captura("11_reintento.png")
        p.wait_for_timeout(2400)
        g.mover(p.locator("[data-testid=referencia]")); g.resaltar(p.locator("[data-testid=referencia]"))
        p.wait_for_timeout(2400)
        g.resaltar(p.locator("[data-testid=saldo-actual]"))
        ref_r = p.locator("[data-testid=referencia]").inner_text(); saldo_r = p.locator("[data-testid=saldo-actual]").inner_text()
        n = len(p.locator("[data-testid=asientos] tbody tr").all())
        verificar("Reintento: misma referencia, sin segundo débito", ref_r == ref50 and saldo_r == "S/ 5,270.00" and n == 2, f"{ref_r} · {saldo_r} · {n} asientos")
    with tramo("b18"):
        g.clic(p.locator(".bank-side a[href='/continuidad']")); p.wait_for_url("**/continuidad")
        g.clic(p.locator("[data-testid=btn-concurrente]"))
        p.wait_for_selector("#resultado-concurrente .resumen", timeout=20000)
        g.desplazar(p.locator("#resultado-concurrente .resumen"))
        g.resaltar(p.locator("#resultado-concurrente"), 4000)
        g.captura("12_reintento_concurrente.png")
        resumen = p.locator("#resultado-concurrente .resumen").inner_text()
        verificar("10 solicitudes simultáneas → una referencia", "Una sola referencia" in resumen and "registradas: 1" in resumen, resumen)

    # S6 Falla controlada y compensación
    with tramo("b19"):
        opcion = p.locator("label.escenario", has_text="Resultado externo desconocido")
        g.desplazar(opcion); g.clic(opcion)
        g.clic(p.locator("form[action='/continuidad/escenario'] button[type=submit]")); p.wait_for_load_state("load")
        g.resaltar(p.locator("[data-testid=escenario-armado]"))
    with tramo("b20"):
        g.clic(p.locator(".bank-side a[href='/banca/enviar']")); p.wait_for_url("**/banca/enviar")
        g.clic(p.locator(".canal-switch label", has_text="Yape"))
        g.mover(p.locator("#bank-target")); p.select_option("#bank-target", label="Bodega Los Alisos")
        g.escribir(p.locator("#bank-amount"), "20", 120)
        g.clic(p.locator("#btn-continuar")); p.wait_for_selector("dialog[open]"); p.wait_for_timeout(900)
        g.clic(p.locator("#btn-confirmar")); p.wait_for_url("**/banca/constancia/**")
        ref20 = p.locator("[data-testid=referencia]").inner_text()
    with tramo("b21"):
        g.mover(p.locator("[data-testid=estado]")); g.resaltar(p.locator("[data-testid=estado]"))
        p.wait_for_timeout(2200)
        g.desplazar(p.locator("[data-testid=asientos]")); g.resaltar(p.locator("[data-testid=asientos]"))
        g.captura("13_en_verificacion.png")
        estado = p.locator("[data-testid=estado]").inner_text().strip()
        n = len(p.locator("[data-testid=asientos] tbody tr").all())
        verificar("Resultado desconocido queda En verificación (cargo sin abono)", estado == "En verificación" and n == 1, f"{ref20} · {estado} · {n} asiento")
    with tramo("b22"):
        g.clic(p.locator(".bank-side a[href='/continuidad']")); p.wait_for_url("**/continuidad")
        fila = p.locator("[data-testid=tabla-operaciones] tr", has_text=ref20)
        g.desplazar(fila); g.resaltar(fila, 3000); p.wait_for_timeout(3000)
        g.clic(fila.locator("button[value=Rechazado]")); p.wait_for_load_state("load")
        fila = p.locator("[data-testid=tabla-operaciones] tr", has_text=ref20)
        g.desplazar(fila); g.resaltar(fila)
    with tramo("b23"):
        fila = p.locator("[data-testid=tabla-operaciones] tr", has_text=ref20)
        g.clic(fila.locator("button[value=Rechazado]")); p.wait_for_load_state("load")
        fila = p.locator("[data-testid=tabla-operaciones] tr", has_text=ref20)
        texto_fila = fila.inner_text()
        g.desplazar(p.locator("[data-testid=bitacora]"))
        g.resaltar(p.locator("[data-testid=bitacora] tbody tr").first, 3000)
        g.captura("14_compensacion_bitacora.png")
        p.wait_for_timeout(2600)
        p.evaluate("window.scrollTo({top:0, behavior:'smooth'})"); p.wait_for_timeout(700)
        g.resaltar(p.locator("[data-testid=saldo-continuidad]"))
        saldo_k = p.locator("[data-testid=saldo-continuidad]").inner_text()
        verificar("Rechazo confirmado ×2 → un único ajuste; saldo vuelve a S/ 5,260.00",
                  "Extornada" in texto_fila and "1 ajuste" in texto_fila and saldo_k == "S/ 5,260.00", f"{ref20} · {' '.join(texto_fila.split())[:90]} · {saldo_k}")

    # S7 Reinicio y persistencia
    with tramo("b24"):
        diapositiva("terminal_detener", D.terminal([("cmd", "PS ...\\BCP_MVP\\BCP.Academico> (Ctrl+C) Deteniendo el servidor…")], "2. Reiniciar el servidor"))
        inicio_caida = time.time()
        servidor.detener()
        salida_apagado = [l for l in servidor.lineas if l.strip()][-3:]
        servidor.iniciar(reiniciar=False)
        recuperado = time.time()
        filas = [("cmd", "PS ...\\BCP_MVP\\BCP.Academico> (Ctrl+C)")] + [("dim", l) for l in salida_apagado] + servidor.lineas_terminal()
        diapositiva("terminal_reinicio", D.terminal(filas[-22:], "2. Reiniciar el servidor (sin --reiniciar-demo)"))
        g.captura("15_reinicio_servidor.png")
    with tramo("b25"):
        ctx.clear_cookies()
        p.goto(URL + "/acceso")
        g.escribir(p.locator("#demo-code"), "DEMO-BCP", 110)
        g.clic(p.locator("#login-form button[type=submit]")); p.wait_for_url("**/banca")
        g.resaltar(p.locator("[data-testid=saldo]"))
        saldo_p = p.locator("[data-testid=saldo]").inner_text()
        verificar("Tras reiniciar, el saldo sigue en S/ 5,260.00 (50 + 10 de la prueba concurrente)", saldo_p == "S/ 5,260.00", saldo_p)
    with tramo("b26"):
        g.clic(p.locator(".bank-side a[href='/banca/historial']")); p.wait_for_url("**/banca/historial")
        g.escribir(p.locator("#bank-query"), ref50, 80)
        p.wait_for_url("**q=*", timeout=8000); p.wait_for_load_state("load")
        g.clic(p.locator(f"li[data-ref='{ref50}'] a")); p.wait_for_url(f"**/{ref50}")
        g.resaltar(p.locator("[data-testid=referencia]"))
        g.captura("16_persistencia_constancia.png")
        ok = p.locator("[data-testid=referencia]").inner_text() == ref50
        verificar("Tras reiniciar, la constancia sigue disponible", ok, f"{ref50} · {p.locator('[data-testid=estado]').inner_text().strip()}")
    with tramo("b27"):
        g.clic(p.locator(".bank-side a[href='/continuidad']")); p.wait_for_url("**/continuidad")
        fila = p.locator("[data-testid=bitacora] tr", has_text="Servicio iniciado").first
        g.desplazar(fila); g.resaltar(fila, 4000)
        g.captura("17_bitacora_arranque.png")
        texto = " ".join(fila.inner_text().split())
        verificar("Bitácora registra el arranque con las operaciones confirmadas", "Confirmadas:" in texto, texto[:120])

    # S8 Resultados y cierre
    pruebas = json.loads((EVID / "resultados_pruebas.json").read_text(encoding="utf-8"))
    diapositiva("resultados", D.resultados(pruebas, VERIFICACIONES))
    with tramo("b28"):
        g.captura("18_resultados.png")
    diapositiva("cierre", D.cierre())
    with tramo("b29"):
        g.captura("19_cierre.png")

    video = p.video
    ctx.close()
    crudo = Path(video.path())
    destino = VIDEO / "_crudo" / "grabacion.webm"
    if destino.exists():
        destino.unlink()
    crudo.rename(destino)
    rto = {"detencion": time.strftime("%H:%M:%S", time.localtime(inicio_caida)),
           "recuperacion": time.strftime("%H:%M:%S", time.localtime(recuperado)),
           "segundos": round(recuperado - inicio_caida, 1)}
    (VIDEO / "_crudo" / "linea_tiempo.json").write_text(json.dumps({"tramos": linea, "reinicio": rto, "ref50": ref50, "ref20": ref20}, ensure_ascii=False, indent=2), encoding="utf-8")
    return rto


def main():
    servidor = Servidor()
    with sync_playwright() as pw:
        browser = pw.chromium.launch(channel="msedge", headless=True)
        try:
            servidor.iniciar(reiniciar=True)
            verificacion_previa(browser)
            servidor.detener()
            print("Reiniciando datos para la grabación…")
            servidor.iniciar(reiniciar=True)
            # Imagen compuesta de acceso y ayuda para las diapositivas
            ctx = browser.new_context(viewport={"width": 1920, "height": 1080}); pg = ctx.new_page()
            pg.goto(D.escribir("acceso_ayuda", D.acceso_y_ayuda()).as_uri()); pg.wait_for_load_state("load")
            pg.screenshot(path=str(EVID / "20_acceso_y_ayuda.png")); ctx.close()
            print("Grabando…")
            rto = grabar(browser, servidor)
            print("Reinicio del ejercicio:", rto)
        finally:
            browser.close()
            servidor.detener()
    (EVID / "verificacion_navegador.json").write_text(json.dumps(VERIFICACIONES, ensure_ascii=False, indent=2), encoding="utf-8")
    fallas = [v for v in VERIFICACIONES if not v["ok"]]
    print(f"Verificaciones: {len(VERIFICACIONES) - len(fallas)}/{len(VERIFICACIONES)} correctas")


if __name__ == "__main__":
    main()
