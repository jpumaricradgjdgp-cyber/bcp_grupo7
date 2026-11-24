"""Genera docs/evidencias/resultados_pruebas.md a partir del TRX de dotnet test y de las verificaciones en el navegador.
Uso: python scripts/resultados.py
"""
import json
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
EVID = RAIZ / "docs" / "evidencias"

CASOS = {
    "P01": ("Base reiniciada; perfiles Mariana y Elena.", "Ingresar con cada perfil; Elena pide la constancia LAB-2026-501183 y la cuenta de Mariana; visitante sin sesión pide /banca; código incorrecto.",
            "Cada perfil ve solo sus datos; ajeno → 403 y evento; sin sesión → /acceso; código incorrecto → 400.", "R2 Acceso indebido", "21_acceso_denegado.png"),
    "P02": ("Mariana S/ 5,320.00; Elena S/ 420.00.", "Revisar y confirmar S/ 50.00 a Elena por BCP.",
            "La revisión no escribe; LAB-2026-501184 Confirmada; 5,270.00 y 470.00; un débito y un abono; aparece en el historial.", "R1 Integridad", "08_confirmacion.png, 09_constancia.png, 10_historial.png"),
    "P03": ("Saldo S/ 5,320.00.", "Revisar/confirmar S/ 6,000; monto negativo; contacto de otro perfil.",
            "422 con motivo; saldos y número de operaciones sin cambios.", "R1 Integridad", "22_validacion_saldo.png"),
    "P04": ("Saldo S/ 5,320.00.", "10 POST simultáneos con la misma clave; un reintento secuencial; misma clave con otro importe.",
            "1 Registrada + 9 Reintento con la misma referencia; 1 débito; reintento → misma referencia; conflicto → 409 sin afectar fondos.", "R1 Duplicidad", "11_reintento.png, 12_reintento_concurrente.png"),
    "P05": ("Escenario «Respuesta perdida» armado.", "Confirmar S/ 50.00; consultar historial; reintentar con la misma clave.",
            "504 sin referencia; el historial muestra la operación; el reintento devuelve la misma referencia; un solo cargo.", "R3 Interrupción", "—"),
    "P06": ("Escenario «Falla antes de guardar» armado.", "Confirmar S/ 50.00; volver a intentar con la misma clave.",
            "503; saldos 5,320.00 / 420.00 intactos; 0 operaciones nuevas; evento registrado; el nuevo intento registra una vez.", "R3 Interrupción", "—"),
    "P07": ("Escenario «Resultado externo desconocido» armado.", "Yape S/ 50.00 a Bodega; conciliar «Rechazo confirmado» dos veces.",
            "En verificación (cargo, sin abono) → Extornada con 1 débito + 1 ajuste; segunda conciliación sin cambios; saldo 5,320.00.", "R1/R3", "13_en_verificacion.png, 14_compensacion_bitacora.png"),
    "P08": ("Base nueva en archivo SQLite.", "Transferir S/ 50.00; detener el servidor; iniciar otro sobre el mismo archivo; reingresar.",
            "Saldo 5,270.00, historial y constancia disponibles; 2 eventos «Servicio iniciado».", "R3 Interrupción", "15_reinicio_servidor.png, 16_persistencia_constancia.png, 17_bitacora_arranque.png"),
    "P09": ("Sesión de Mariana.", "POST de confirmación sin token; cerrar sesión; pedir /banca.",
            "400 sin cambios; tras cerrar sesión /banca redirige a /acceso.", "R2 Acceso indebido", "—"),
    "P10": ("Sesión de Mariana.", "Copiar la cookie; cerrar sesión; reutilizar la cookie copiada.",
            "La cookie anterior ya no da acceso (sello rotado).", "R2 Acceso indebido", "—"),
}


def main():
    pruebas = json.loads((EVID / "resultados_pruebas.json").read_text(encoding="utf-8"))
    nav_ruta = EVID / "verificacion_navegador.json"
    navegador = json.loads(nav_ruta.read_text(encoding="utf-8")) if nav_ruta.exists() else []
    l = ["# Resultados de pruebas del MVP", "",
         f"Ejecución automatizada: `dotnet test` (TRX en `pruebas.trx`) · {sum(p['resultado'] == 'Passed' for p in pruebas)}/{len(pruebas)} pasaron.",
         f"Verificación en el navegador (Microsoft Edge con Playwright, durante la grabación): {sum(v['ok'] for v in navegador)}/{len(navegador)} correctas.", "",
         "## Pruebas automatizadas de integración (HTTP → MVC → servicios → EF Core → SQLite)", "",
         "| Prueba | Riesgo | Condición inicial | Pasos | Resultado esperado | Resultado observado | Evidencia | Estado |",
         "|---|---|---|---|---|---|---|---|"]
    for p in pruebas:
        cod = p["nombre"].split(" ")[0]
        cond, pasos, esperado, riesgo, evid = CASOS[cod]
        estado = "Pasó" if p["resultado"] == "Passed" else ("Falló" if p["resultado"] == "Failed" else "Pendiente")
        l.append(f"| {p['nombre'].split(':')[0]} | {riesgo} | {cond} | {pasos} | {esperado} | {p['salida'].replace('|', '/') or '—'} | {evid} | **{estado}** |")
    l += ["", "## Verificaciones en el navegador", "", "| Verificación | Resultado observado | Estado |", "|---|---|---|"]
    for v in navegador:
        l.append(f"| {v['verificacion']} | {v['observado'].replace('|', '/')} | **{'Pasó' if v['ok'] else 'Falló'}** |")
    l += ["", "## Relación con los riesgos del PESI", "",
          "- **R1 Integridad y duplicidad:** P02, P03, P04, P07 · clave de idempotencia única por perfil, índice único de movimientos, transacción única.",
          "- **R2 Acceso indebido:** P01, P09, P10 · autorización en el servidor por perfil, antifalsificación y sello de sesión.",
          "- **R3 Interrupción del servicio:** P05, P06, P07, P08 · falla previa sin efectos, recuperación de la referencia, verificación y compensación única, persistencia tras reinicio.",
          "", "## Limitaciones", "",
          "- RTO ≤ 15 min y RPO 0 son metas del PESI. El reinicio medido en la grabación corresponde solo al ejercicio local (ver `video/BCP_Guion_Tutorial.md`).",
          "- Los adaptadores BCP y Yape son locales; no hay integración real con el sistema central ni con la red de pagos.",
          "- Accesibilidad: se verificó el uso con teclado del acceso y del diálogo (foco al abrir y al cerrar). No se hizo una auditoría completa con lector de pantalla."]
    (EVID / "resultados_pruebas.md").write_text("\n".join(l) + "\n", encoding="utf-8")
    print("resultados_pruebas.md generado")


if __name__ == "__main__":
    main()
