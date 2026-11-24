# Laboratorio BCP · MVP del Grupo 7

Proyecto académico del curso **Planeamiento Estratégico de las TIC** (UTP · Grupo 7 · Sección 20750).
Es el MVP del caso BCP: **consulta de saldo, transferencia, constancia, historial y reintento sin un segundo débito**,
con persistencia en base de datos y un panel académico de continuidad.

> Todos los perfiles, cuentas, contactos y operaciones son **ficticios**. El sitio no se conecta con sistemas del banco
> y no pide claves ni datos personales reales. La arquitectura en Azure es la **propuesta To-Be del PESI**, no esta implementación.

## Tecnología y versión probada

| Componente | Versión usada |
|---|---|
| .NET SDK | **10.0.404** (LTS; runtime Microsoft.AspNetCore.App **10.0.15**) |
| Framework | ASP.NET Core MVC con vistas Razor (`net10.0`) |
| Datos | Entity Framework Core **10.0.15** + SQLite (`Microsoft.EntityFrameworkCore.Sqlite`) |
| Pruebas | xUnit 2.9.3 + `Microsoft.AspNetCore.Mvc.Testing` 10.0.15 |
| Interfaz | HTML, CSS y JavaScript sin frameworks (reutilizados de la web del proyecto) |

Si no tienes el SDK: `winget install Microsoft.DotNet.SDK.10`, o sin permisos de administrador:
`powershell -c "& ([scriptblock]::Create((irm https://dot.net/v1/dotnet-install.ps1))) -Channel 10.0"`.

## Cómo ejecutarlo

Desde esta carpeta (`BCP_MVP`):

```powershell
cd BCP.Academico
dotnet run -- --reiniciar-demo
```

- URL local: **http://localhost:5080**
- Acceso: **Banca por Internet** → perfil **Mariana Torres** (o **Elena Rojas**) → código **DEMO-BCP**
- `--reiniciar-demo` restablece la base de práctica (`BCP.Academico/App_Data/bcp_academico.db`) a los saldos iniciales.
  Sin esa opción, el servidor conserva los datos de la ejecución anterior.
- Al iniciar, el servidor aplica las migraciones de EF Core y carga los datos iniciales si la base está vacía.

Para detenerlo: `Ctrl+C` en la terminal.

## Repetir el caso de S/ 50.00

| Dato | Valor esperado |
|---|---:|
| Saldo inicial de la cuenta de origen (Mariana, •••• 2048) | S/ 5,320.00 |
| Saldo inicial de la cuenta de destino (Elena, •••• 1042) | S/ 420.00 |
| Transferencia de la demostración | S/ 50.00 |
| Saldo final de la cuenta de origen | S/ 5,270.00 |
| Saldo final de la cuenta de destino | S/ 470.00 |
| Resultado al repetir el mismo envío | Un débito, un crédito y la misma referencia |

1. Ejecuta `dotnet run -- --reiniciar-demo` y abre http://localhost:5080/acceso.
2. Ingresa como **Mariana Torres** con **DEMO-BCP**. El panel muestra **S/ 5,320.00**.
3. **Transferir por BCP** → destinatario **Elena Rojas** → monto **50** → **Continuar**.
4. En **Revisa tu envío** comprueba destinatario, cuenta enmascarada, canal, monto y comisión → **Confirmar envío**.
5. La constancia muestra la referencia (la primera es **LAB-2026-501184**), el estado **Confirmada**, el saldo **S/ 5,270.00**
   y, en *Asientos registrados*, un débito en •••• 2048 y un crédito en •••• 1042.
6. Pulsa **Repetir el mismo envío** (al final de la constancia): aparece *Reintento resuelto* con la **misma referencia** y el saldo no cambia.
7. Opcional: **Continuidad (práctica)** → **Enviar 10 solicitudes a la vez** (S/ 10.00 a Carlos Salas): una sola referencia.
   *Es una operación nueva y baja el saldo en S/ 10.00.*
8. Detén el servidor (`Ctrl+C`) y vuelve a iniciarlo **sin** `--reiniciar-demo` (`dotnet run`). Ingresa de nuevo:
   saldo, historial y constancia siguen disponibles. En la bitácora del panel aparece *Servicio iniciado* con las operaciones encontradas.
9. Para ver el abono, ingresa como **Elena Rojas**: el panel muestra **S/ 470.00** y el abono recibido.

Para volver a empezar: `dotnet run -- --reiniciar-demo` o el botón **Restablecer datos de la demostración** del panel.

### Escenarios de continuidad (panel «Continuidad (práctica)»)

Se arman para **la siguiente operación nueva** (los reintentos con una clave existente no los consumen):

| Escenario | Qué hace | Qué debes ver |
|---|---|---|
| Falla antes de guardar | Revierte la transacción antes del commit | Mensaje de error, saldos intactos, evento *Falla antes de guardar* |
| Respuesta perdida después de guardar | Guarda la operación pero no muestra la respuesta (HTTP 504) | Pantalla *No recibimos la respuesta*; **Reintentar el mismo envío** recupera la referencia original |
| Resultado externo desconocido | El canal no confirma ni rechaza | Estado **En verificación** (cargo sin abono). Conciliar con **Rechazo confirmado** crea **un único** ajuste; repetirlo registra *Conciliación sin cambios* |

El panel solo existe si `Practica:Habilitada` es `true` en `appsettings.json`.

## Arquitectura MVC

```
BCP_MVP/
├── BCP.Academico/
│   ├── Controllers/   Inicio, Acceso, Cuentas, Transferencias, Historial, Continuidad
│   ├── Models/        Cliente, Cuenta, Contacto, Transferencia, Movimiento, EventoAuditoria
│   ├── ViewModels/    modelos de entrada y de vista (solo los campos que la interfaz necesita)
│   ├── Services/      ServicioTransferencias, ServicioConciliacion, ServicioCuentas, ServicioAcceso,
│   │                  ServicioAuditoria, ServicioContinuidad, adaptadores BCP/Yape, Dinero
│   ├── Data/          AppDbContext, Configurations/ (índices únicos, FK, CHECK), Seed/ (datos iniciales)
│   ├── Migrations/    migración inicial de EF Core
│   ├── Views/         Razor: _Layout, _BancaLayout y parciales (_TarjetaSaldo, _EstadoOperacion,
│   │                  _ResumenEnvio, _Mensajes, _ListaMovimientos)
│   └── wwwroot/       css/, js/site.js, img/ (cuy, fotografías y logo de referencia del curso)
├── BCP.Academico.Tests/   pruebas de integración HTTP → MVC → servicios → SQLite
├── docs/diagramas/        As-Is, To-Be, arquitectura MVC y arquitectura empresarial (SVG/PNG + Mermaid)
├── docs/evidencias/       capturas numeradas, resultados de pruebas y verificaciones en el navegador
├── scripts/video/         guion, narración, grabación automatizada y montaje del video
└── video/                 BCP_Tutorial_Final.mp4, .srt, guion y audio por tramo
```

Camino de un envío: **Vista Razor → `TransferenciasController.Confirmar` → `ServicioTransferencias.ConfirmarAsync` →
`AppDbContext` + SQLite (una transacción `BEGIN IMMEDIATE`) → redirección → `Historial/Constancia.cshtml`.**

Controles principales:

- Importes en **céntimos enteros** (`long`), con `CHECK` de saldo no negativo e importe positivo.
- **Clave de idempotencia** por envío lógico: índice único `(ClienteId, ClaveIdempotencia)`. Misma clave + mismos datos → misma referencia;
  misma clave + otros datos → conflicto (HTTP 409) sin afectar fondos. Huella SHA-256 de destino, canal, importe y concepto.
- Índice único `(TransferenciaId, CuentaId, Tipo)` en movimientos: imposible registrar dos débitos o dos ajustes para la misma operación.
- Lectura de saldo y escritura dentro de la **misma transacción**; débito, crédito, estado, referencia y auditoría se confirman juntos.
- Autenticación por **cookie**, `[Authorize]` en la banca, **token antifalsificación** en todos los POST y sello de sesión que invalida
  cookies anteriores al cerrar sesión. Cada perfil solo ve sus cuentas, movimientos y constancias (HTTP 403 y evento en la bitácora).
- `localStorage` solo guarda la preferencia de ocultar el saldo.

## Pruebas

```powershell
dotnet test
```

10 pruebas de integración (resultado y salida en `docs/evidencias/resultados_pruebas.md`):
consulta y acceso, transferencia válida, validación de saldo, reintento concurrente (10 solicitudes), pérdida de respuesta,
falla antes de confirmar, rechazo y compensación, persistencia tras reinicio, cierre de sesión con antifalsificación y sello de sesión.

## Video y evidencias

- `video/BCP_Tutorial_Final.mp4` (1080p, con subtítulos), `video/BCP_Tutorial_Final.srt` y `video/BCP_Guion_Tutorial.md`.
- Para regenerarlo: `python scripts/video/grabar.py` (requiere `pip install playwright` y Microsoft Edge) y luego `python scripts/video/montar.py` (requiere ffmpeg).
  La narración se genera con `scripts/video/narrar.ps1` a partir de `scripts/video/guion.py`.

## Limitaciones

- Entorno local con un solo servidor y SQLite; no hay alta disponibilidad real.
- Los canales BCP y Yape son **adaptadores académicos locales**; no hay integración con el sistema central ni con la red de pagos.
- Las metas **RTO ≤ 15 min** y **RPO 0 para operaciones confirmadas** son del PESI; aquí solo se mide el ejercicio local de reinicio.
- Las fotografías e ilustraciones (emprendedora, POS, cuy) son recursos gráficos generados. El logo es la referencia facilitada para el curso.
