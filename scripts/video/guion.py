"""Guion del video tutorial: única fuente para la narración, la grabación y los subtítulos.

Cada "tramo" (beat) tiene un identificador, el bloque del guion al que pertenece y el texto
que se narra palabra por palabra. La acción visual de cada tramo está en grabar.py.
"""

BLOQUES = {
    "S1": "0:00–0:30 · Portada del proyecto",
    "S2": "0:30–1:15 · As-Is, To-Be y arquitectura MVC",
    "S3": "1:15–1:55 · Arranque, acceso y saldo inicial",
    "S4": "1:55–3:15 · Transferencia de S/ 50.00 y constancia",
    "S5": "3:15–4:10 · Repetir el envío sin un segundo débito",
    "S6": "4:10–5:10 · Falla controlada, verificación y compensación",
    "S7": "5:10–6:05 · Reinicio del servidor y persistencia",
    "S8": "6:05–6:40 · Resultados y cierre",
}

TRAMOS = [
    ("b01", "S1", "Hola. Somos el Grupo siete del curso Planeamiento Estratégico de las TIC. En este tutorial vas a ver el MVP que construimos para el caso BCP, con datos totalmente ficticios."),
    ("b02", "S1", "En esta demostración vamos a transferir cincuenta soles, repetir el envío y comprobar que el sistema registra un solo cargo. Al final, reiniciaremos el servidor para ver que la información sigue ahí."),
    ("b03", "S2", "Primero, el problema. En el proceso actual, si el canal no recibe respuesta, no hay una clave estable que identifique la operación. Un reintento podría generar un segundo cargo, y un resultado incierto depende de una revisión manual."),
    ("b04", "S2", "En el proceso propuesto, cada envío lleva una clave de idempotencia. El servicio guarda la operación y sus movimientos de forma atómica, y solo compensa una vez, cuando el rechazo externo está confirmado."),
    ("b05", "S2", "Así lo implementamos con el patrón Modelo, Vista, Controlador. La vista Razor muestra el formulario. El controlador recibe la solicitud, comprueba la sesión y llama al servicio de transferencias. El servicio aplica las reglas, y Entity Framework guarda todo en SQLite, dentro de una sola transacción."),
    ("b06", "S2", "Ojo: esta otra es la arquitectura empresarial propuesta en el PESI, con Azure API Management, microservicios en AKS, el sistema central del banco y un modelo de lectura alimentado por CDC. Nuestro MVP es la versión local de ese flujo, y no se conecta con sistemas reales."),
    ("b07", "S3", "Para arrancar, abre una terminal en la carpeta del proyecto y ejecuta dotnet run, con la opción reiniciar demo. Así la base de práctica vuelve a los saldos iniciales. El sitio queda disponible en localhost, puerto cinco cero ocho cero."),
    ("b08", "S3", "En la pantalla de acceso elegimos el perfil ficticio de Mariana Torres y escribimos el código del curso: DEMO, guion, BCP. El servidor valida el código. No pedimos claves ni datos reales."),
    ("b09", "S3", "Este es el panel. El saldo disponible es de cinco mil trescientos veinte soles, y la cuenta aparece enmascarada. Estos datos vienen de la base SQLite, no del navegador."),
    ("b10", "S4", "Vamos a Transferir por BCP. Elegimos a nuestra contacto Elena Rojas. Fíjate que la cuenta de destino también se muestra enmascarada."),
    ("b11", "S4", "Escribimos cincuenta soles y un concepto opcional. Si el monto fuera mayor que el saldo, el servidor lo rechazaría y explicaría el motivo, sin tocar los fondos."),
    ("b12", "S4", "Al pulsar Continuar, el servidor valida los datos y nos muestra la revisión: destinatario, cuenta enmascarada, canal, monto y comisión del ejercicio. Todavía no se ha registrado nada."),
    ("b13", "S4", "Ahora sí: Confirmar envío. Esta es la única acción que ordena el registro. Obtenemos la constancia, con su código de operación."),
    ("b14", "S4", "En los asientos vemos un débito de cincuenta soles en la cuenta de Mariana y un abono de cincuenta soles en la cuenta de Elena. El saldo actual baja a cinco mil doscientos setenta soles."),
    ("b15", "S4", "En el historial, la operación aparece confirmada y con la misma referencia. Desde aquí podemos abrir la constancia, copiar el código o guardarla en PDF."),
    ("b16", "S5", "Ahora repetimos exactamente el mismo envío, con la misma clave. Es lo que pasaría si el cliente pulsa dos veces, o si su aplicación reintenta después de un corte."),
    ("b17", "S5", "El servidor reconoce la clave, devuelve la misma referencia y no hace un nuevo cargo. El saldo sigue en cinco mil doscientos setenta soles. La clave de idempotencia es, simplemente, el identificador único de un envío."),
    ("b18", "S5", "Para probarlo con más presión, el panel de continuidad envía diez solicitudes simultáneas con una misma clave. Es una operación nueva, de diez soles a Carlos Salas: se registra una sola vez, y las otras nueve se resuelven como reintentos, con la misma referencia."),
    ("b19", "S6", "Ahora activamos una falla controlada: resultado externo desconocido. Se aplicará solo a la siguiente operación nueva."),
    ("b20", "S6", "Hacemos una operación nueva, para que puedas seguir los saldos: veinte soles por Yape, a Bodega Los Alisos."),
    ("b21", "S6", "El canal no confirmó ni rechazó. La operación queda En verificación: hay un cargo, pero todavía no hay abono. No compensamos por tiempo agotado."),
    ("b22", "S6", "En el panel vemos la referencia, el estado, el último evento y el resultado. Cuando llega un rechazo confirmado, registramos un único ajuste compensatorio, vinculado al cargo original."),
    ("b23", "S6", "Si la conciliación se repite, no se crea un segundo ajuste: la bitácora registra una conciliación sin cambios. El saldo vuelve a cinco mil doscientos sesenta soles, el mismo que antes de esta prueba, y el cargo original queda para la auditoría."),
    ("b24", "S7", "Para comprobar la persistencia, detenemos el servidor y lo volvemos a iniciar, esta vez sin reiniciar los datos."),
    ("b25", "S7", "Volvemos a ingresar con el mismo perfil. El saldo sigue en cinco mil doscientos sesenta soles: el envío de cincuenta, más la prueba concurrente de diez."),
    ("b26", "S7", "Buscamos la referencia del envío de cincuenta soles en el historial y abrimos su constancia. Todo sigue disponible, porque cada operación confirmada se guardó en disco."),
    ("b27", "S7", "En la bitácora aparece el arranque del servicio, con las operaciones confirmadas que encontró. Esto se relaciona con la meta del PESI de RPO cero para operaciones confirmadas; aquí lo comprobamos solo para este ejercicio local."),
    ("b28", "S8", "Este es el resumen de resultados. Las diez pruebas automatizadas de integración pasaron, junto con las verificaciones en el navegador. Cubren integridad y duplicidad, acceso indebido e interrupción del servicio."),
    ("b29", "S8", "Las limitaciones también quedan anotadas: es un entorno local, con datos ficticios, y la arquitectura en Azure es una propuesta. En el README encontrarás los comandos exactos para repetir este caso de cincuenta soles. Gracias por acompañarnos."),
]
