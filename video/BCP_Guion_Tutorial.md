# Guion del video tutorial · BCP_Tutorial_Final.mp4

Duración final: **6:42** · 1920×1080 · 30 fps · H.264 + AAC · subtítulos en español (pista suave y `BCP_Tutorial_Final.srt`).
Voz: Microsoft Sabina (español de México), la voz en español disponible en este Windows. Para una voz peruana, regraba la narración con este texto y reemplaza los WAV de `video/audio/` (misma duración aproximada) antes de ejecutar `montar.py`.


## 0:00–0:30 · Portada del proyecto

**[0:00–0:13] b01** — Hola. Somos el Grupo siete del curso Planeamiento Estratégico de las TIC. En este tutorial vas a ver el MVP que construimos para el caso BCP, con datos totalmente ficticios.

**[0:13–0:27] b02** — En esta demostración vamos a transferir cincuenta soles, repetir el envío y comprobar que el sistema registra un solo cargo. Al final, reiniciaremos el servidor para ver que la información sigue ahí.


## 0:30–1:15 · As-Is, To-Be y arquitectura MVC

**[0:27–0:44] b03** — Primero, el problema. En el proceso actual, si el canal no recibe respuesta, no hay una clave estable que identifique la operación. Un reintento podría generar un segundo cargo, y un resultado incierto depende de una revisión manual.

**[0:44–0:58] b04** — En el proceso propuesto, cada envío lleva una clave de idempotencia. El servicio guarda la operación y sus movimientos de forma atómica, y solo compensa una vez, cuando el rechazo externo está confirmado.

**[0:58–1:20] b05** — Así lo implementamos con el patrón Modelo, Vista, Controlador. La vista Razor muestra el formulario. El controlador recibe la solicitud, comprueba la sesión y llama al servicio de transferencias. El servicio aplica las reglas, y Entity Framework guarda todo en SQLite, dentro de una sola transacción.

**[1:20–1:40] b06** — Ojo: esta otra es la arquitectura empresarial propuesta en el PESI, con Azure API Management, microservicios en AKS, el sistema central del banco y un modelo de lectura alimentado por CDC. Nuestro MVP es la versión local de ese flujo, y no se conecta con sistemas reales.


## 1:15–1:55 · Arranque, acceso y saldo inicial

**[1:40–1:58] b07** — Para arrancar, abre una terminal en la carpeta del proyecto y ejecuta dotnet run, con la opción reiniciar demo. Así la base de práctica vuelve a los saldos iniciales. El sitio queda disponible en localhost, puerto cinco cero ocho cero.

**[1:58–2:13] b08** — En la pantalla de acceso elegimos el perfil ficticio de Mariana Torres y escribimos el código del curso: DEMO, guion, BCP. El servidor valida el código. No pedimos claves ni datos reales.

**[2:13–2:26] b09** — Este es el panel. El saldo disponible es de cinco mil trescientos veinte soles, y la cuenta aparece enmascarada. Estos datos vienen de la base SQLite, no del navegador.


## 1:55–3:15 · Transferencia de S/ 50.00 y constancia

**[2:26–2:37] b10** — Vamos a Transferir por BCP. Elegimos a nuestra contacto Elena Rojas. Fíjate que la cuenta de destino también se muestra enmascarada.

**[2:37–2:48] b11** — Escribimos cincuenta soles y un concepto opcional. Si el monto fuera mayor que el saldo, el servidor lo rechazaría y explicaría el motivo, sin tocar los fondos.

**[2:48–3:02] b12** — Al pulsar Continuar, el servidor valida los datos y nos muestra la revisión: destinatario, cuenta enmascarada, canal, monto y comisión del ejercicio. Todavía no se ha registrado nada.

**[3:02–3:13] b13** — Ahora sí: Confirmar envío. Esta es la única acción que ordena el registro. Obtenemos la constancia, con su código de operación.

**[3:13–3:25] b14** — En los asientos vemos un débito de cincuenta soles en la cuenta de Mariana y un abono de cincuenta soles en la cuenta de Elena. El saldo actual baja a cinco mil doscientos setenta soles.

**[3:25–3:37] b15** — En el historial, la operación aparece confirmada y con la misma referencia. Desde aquí podemos abrir la constancia, copiar el código o guardarla en PDF.


## 3:15–4:10 · Repetir el envío sin un segundo débito

**[3:37–3:48] b16** — Ahora repetimos exactamente el mismo envío, con la misma clave. Es lo que pasaría si el cliente pulsa dos veces, o si su aplicación reintenta después de un corte.

**[3:48–4:05] b17** — El servidor reconoce la clave, devuelve la misma referencia y no hace un nuevo cargo. El saldo sigue en cinco mil doscientos setenta soles. La clave de idempotencia es, simplemente, el identificador único de un envío.

**[4:05–4:23] b18** — Para probarlo con más presión, el panel de continuidad envía diez solicitudes simultáneas con una misma clave. Es una operación nueva, de diez soles a Carlos Salas: se registra una sola vez, y las otras nueve se resuelven como reintentos, con la misma referencia.


## 4:10–5:10 · Falla controlada, verificación y compensación

**[4:23–4:32] b19** — Ahora activamos una falla controlada: resultado externo desconocido. Se aplicará solo a la siguiente operación nueva.

**[4:32–4:40] b20** — Hacemos una operación nueva, para que puedas seguir los saldos: veinte soles por Yape, a Bodega Los Alisos.

**[4:40–4:52] b21** — El canal no confirmó ni rechazó. La operación queda En verificación: hay un cargo, pero todavía no hay abono. No compensamos por tiempo agotado.

**[4:52–5:06] b22** — En el panel vemos la referencia, el estado, el último evento y el resultado. Cuando llega un rechazo confirmado, registramos un único ajuste compensatorio, vinculado al cargo original.

**[5:06–5:23] b23** — Si la conciliación se repite, no se crea un segundo ajuste: la bitácora registra una conciliación sin cambios. El saldo vuelve a cinco mil doscientos sesenta soles, el mismo que antes de esta prueba, y el cargo original queda para la auditoría.


## 5:10–6:05 · Reinicio del servidor y persistencia

**[5:23–5:31] b24** — Para comprobar la persistencia, detenemos el servidor y lo volvemos a iniciar, esta vez sin reiniciar los datos.

**[5:31–5:42] b25** — Volvemos a ingresar con el mismo perfil. El saldo sigue en cinco mil doscientos sesenta soles: el envío de cincuenta, más la prueba concurrente de diez.

**[5:42–5:53] b26** — Buscamos la referencia del envío de cincuenta soles en el historial y abrimos su constancia. Todo sigue disponible, porque cada operación confirmada se guardó en disco.

**[5:53–6:09] b27** — En la bitácora aparece el arranque del servicio, con las operaciones confirmadas que encontró. Esto se relaciona con la meta del PESI de RPO cero para operaciones confirmadas; aquí lo comprobamos solo para este ejercicio local.


## 6:05–6:40 · Resultados y cierre

**[6:09–6:24] b28** — Este es el resumen de resultados. Las diez pruebas automatizadas de integración pasaron, junto con las verificaciones en el navegador. Cubren integridad y duplicidad, acceso indebido e interrupción del servicio.

**[6:24–6:42] b29** — Las limitaciones también quedan anotadas: es un entorno local, con datos ficticios, y la arquitectura en Azure es una propuesta. En el README encontrarás los comandos exactos para repetir este caso de cincuenta soles. Gracias por acompañarnos.


## Datos de la toma

- Referencia del envío de S/ 50.00: **LAB-2026-501184**
- Operación nueva del escenario de falla (S/ 20.00 por Yape): **LAB-2026-501186**
- Reinicio del servidor en el ejercicio: detenido 08:43:10, disponible 08:43:17 (**7.3 s**, solo este ejercicio local; la meta del PESI es RTO ≤ 15 min).
