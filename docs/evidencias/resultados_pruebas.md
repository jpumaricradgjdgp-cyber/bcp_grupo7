# Resultados de pruebas del MVP

Ejecución automatizada: `dotnet test` (TRX en `pruebas.trx`) · 10/10 pasaron.
Verificación en el navegador (Microsoft Edge con Playwright, durante la grabación): 17/17 correctas.

## Pruebas automatizadas de integración (HTTP → MVC → servicios → EF Core → SQLite)

| Prueba | Riesgo | Condición inicial | Pasos | Resultado esperado | Resultado observado | Evidencia | Estado |
|---|---|---|---|---|---|---|---|
| P01 Consulta y acceso (R2) | R2 Acceso indebido | Base reiniciada; perfiles Mariana y Elena. | Ingresar con cada perfil; Elena pide la constancia LAB-2026-501183 y la cuenta de Mariana; visitante sin sesión pide /banca; código incorrecto. | Cada perfil ve solo sus datos; ajeno → 403 y evento; sin sesión → /acceso; código incorrecto → 400. | Mariana ve S/ 5,320.00 y •••• 2048; Elena ve S/ 420.00; constancia y cuenta ajenas → 403 registrados en bitácora. | 21_acceso_denegado.png | **Pasó** |
| P02 Transferencia válida (R1) | R1 Integridad | Mariana S/ 5,320.00; Elena S/ 420.00. | Revisar y confirmar S/ 50.00 a Elena por BCP. | La revisión no escribe; LAB-2026-501184 Confirmada; 5,270.00 y 470.00; un débito y un abono; aparece en el historial. | LAB-2026-501184 Confirmada · Mariana 5,320.00 → 5,270.00 · Elena 420.00 → 470.00 · 1 débito + 1 abono. | 08_confirmacion.png, 09_constancia.png, 10_historial.png | **Pasó** |
| P03 Validación de saldo | R1 Integridad | Saldo S/ 5,320.00. | Revisar/confirmar S/ 6,000; monto negativo; contacto de otro perfil. | 422 con motivo; saldos y número de operaciones sin cambios. | S/ 6,000 > S/ 5,320 → 422 con mensaje; monto negativo y contacto ajeno → 422; saldos y operaciones sin cambios. | 22_validacion_saldo.png | **Pasó** |
| P04 Reintento concurrente (R1) | R1 Duplicidad | Saldo S/ 5,320.00. | 10 POST simultáneos con la misma clave; un reintento secuencial; misma clave con otro importe. | 1 Registrada + 9 Reintento con la misma referencia; 1 débito; reintento → misma referencia; conflicto → 409 sin afectar fondos. | Reintento, Reintento, Registrada, Reintento, Reintento, Reintento, Reintento, Reintento, Reintento, Reintento
1 Registrada + 9 Reintento → LAB-2026-501184; 1 débito; saldo 5,270.00; conflicto de clave → 409 sin afectar fondos. | 11_reintento.png, 12_reintento_concurrente.png | **Pasó** |
| P05 Pérdida de respuesta (R3) | R3 Interrupción | Escenario «Respuesta perdida» armado. | Confirmar S/ 50.00; consultar historial; reintentar con la misma clave. | 504 sin referencia; el historial muestra la operación; el reintento devuelve la misma referencia; un solo cargo. | Respuesta 504 sin referencia → historial muestra LAB-2026-501184 → reintento devuelve la misma referencia; un solo cargo. | — | **Pasó** |
| P06 Falla antes de confirmar (R3) | R3 Interrupción | Escenario «Falla antes de guardar» armado. | Confirmar S/ 50.00; volver a intentar con la misma clave. | 503; saldos 5,320.00 / 420.00 intactos; 0 operaciones nuevas; evento registrado; el nuevo intento registra una vez. | 503 con saldos 5,320.00 / 420.00 intactos, 0 operaciones nuevas, evento registrado; el reintento posterior registra una sola vez. | — | **Pasó** |
| P07 Rechazo y compensación (R1/R3) | R1/R3 | Escenario «Resultado externo desconocido» armado. | Yape S/ 50.00 a Bodega; conciliar «Rechazo confirmado» dos veces. | En verificación (cargo, sin abono) → Extornada con 1 débito + 1 ajuste; segunda conciliación sin cambios; saldo 5,320.00. | LAB-2026-501184: En verificación (cargo, sin abono) → rechazo confirmado ×2 → Extornada con 1 débito + 1 ajuste; saldo vuelve a 5,320.00. | 13_en_verificacion.png, 14_compensacion_bitacora.png | **Pasó** |
| P08 Persistencia (R3) | R3 Interrupción | Base nueva en archivo SQLite. | Transferir S/ 50.00; detener el servidor; iniciar otro sobre el mismo archivo; reingresar. | Saldo 5,270.00, historial y constancia disponibles; 2 eventos «Servicio iniciado». | Reinicio: LAB-2026-501184 y saldo S/ 5,270.00 recuperados desde el mismo archivo SQLite (2 eventos de arranque). | 15_reinicio_servidor.png, 16_persistencia_constancia.png, 17_bitacora_arranque.png | **Pasó** |
| P09 Cierre de sesión y antifalsificación | R2 Acceso indebido | Sesión de Mariana. | POST de confirmación sin token; cerrar sesión; pedir /banca. | 400 sin cambios; tras cerrar sesión /banca redirige a /acceso. | POST sin token → 400 sin cambios; tras cerrar sesión /banca redirige a /acceso. | — | **Pasó** |
| P10 Sello de sesión | R2 Acceso indebido | Sesión de Mariana. | Copiar la cookie; cerrar sesión; reutilizar la cookie copiada. | La cookie anterior ya no da acceso (sello rotado). | La cookie emitida antes del cierre de sesión es rechazada (sello rotado en SQLite). | — | **Pasó** |

## Verificaciones en el navegador

| Verificación | Resultado observado | Estado |
|---|---|---|
| Ingreso con teclado (Enter) | http://localhost:5080/banca | **Pasó** |
| Saldo inicial desde SQLite | S/ 5,320.00 | **Pasó** |
| Foco entra al diálogo al abrir | activeElement = #send-title | **Pasó** |
| Escape cierra y devuelve el foco a Continuar | #btn-continuar, monto=50 | **Pasó** |
| Editar conserva los datos y devuelve el foco | #btn-continuar, destino=Elena Rojas | **Pasó** |
| Monto mayor al saldo se rechaza con motivo | El monto supera tu saldo disponible. Ingresa un importe menor. | **Pasó** |
| Tras cerrar sesión, /banca exige ingresar | http://localhost:5080/acceso?ReturnUrl=%2Fbanca | **Pasó** |
| Constancia ajena denegada | HTTP 403 | **Pasó** |
| Panel muestra S/ 5,320.00 | S/ 5,320.00 | **Pasó** |
| Confirmación: débito y abono de S/ 50.00, saldo S/ 5,270.00 | LAB-2026-501184 · S/ 5,270.00 · 2 asientos | **Pasó** |
| Reintento: misma referencia, sin segundo débito | LAB-2026-501184 · S/ 5,270.00 · 2 asientos | **Pasó** |
| 10 solicitudes simultáneas → una referencia | Una sola referencia: LAB-2026-501185 · registradas: 1, reintentos resueltos: 9 | **Pasó** |
| Resultado desconocido queda En verificación (cargo sin abono) | LAB-2026-501186 · En verificación · 1 asiento | **Pasó** |
| Rechazo confirmado ×2 → un único ajuste; saldo vuelve a S/ 5,260.00 | LAB-2026-501186 · LAB-2026-501186 Yape S/ 20.00 Extornada 1 débito · 0 abono · 1 ajuste Conciliación sin cam · S/ 5,260.00 | **Pasó** |
| Tras reiniciar, el saldo sigue en S/ 5,260.00 (50 + 10 de la prueba concurrente) | S/ 5,260.00 | **Pasó** |
| Tras reiniciar, la constancia sigue disponible | LAB-2026-501184 · Confirmada | **Pasó** |
| Bitácora registra el arranque con las operaciones confirmadas | 24/11/2026 08:43:16 — Servicio iniciado Base SQLite abierta. Confirmadas: 4; en verificación: 0; última referencia: LAB- | **Pasó** |

## Relación con los riesgos del PESI

- **R1 Integridad y duplicidad:** P02, P03, P04, P07 · clave de idempotencia única por perfil, índice único de movimientos, transacción única.
- **R2 Acceso indebido:** P01, P09, P10 · autorización en el servidor por perfil, antifalsificación y sello de sesión.
- **R3 Interrupción del servicio:** P05, P06, P07, P08 · falla previa sin efectos, recuperación de la referencia, verificación y compensación única, persistencia tras reinicio.

## Limitaciones

- RTO ≤ 15 min y RPO 0 son metas del PESI. El reinicio medido en la grabación corresponde solo al ejercicio local (ver `video/BCP_Guion_Tutorial.md`).
- Los adaptadores BCP y Yape son locales; no hay integración real con el sistema central ni con la red de pagos.
- Accesibilidad: se verificó el uso con teclado del acceso y del diálogo (foco al abrir y al cerrar). No se hizo una auditoría completa con lector de pantalla.
