// Interacciones de la interfaz. Los datos y las reglas viven en el servidor (MVC + SQLite).
(() => {
  "use strict";
  const $ = (sel, root = document) => root.querySelector(sel);
  const $$ = (sel, root = document) => [...root.querySelectorAll(sel)];
  const pagina = document.body.dataset.pagina;
  const sinTildes = t => t.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();
  const pref = {
    get(k) { try { return localStorage.getItem("lab-bcp-" + k); } catch { return null; } },
    set(k, v) { try { localStorage.setItem("lab-bcp-" + k, v); } catch { /* sin almacenamiento */ } }
  };

  // ---------- Encabezado: menús y búsqueda ----------
  $$(".nav-products").forEach(d => d.addEventListener("toggle", () => {
    if (d.open) $$(".nav-products[open]").forEach(o => { if (o !== d) o.open = false; });
  }));
  document.addEventListener("click", e => {
    const abierto = $(".nav-products[open]");
    if (abierto && (!abierto.contains(e.target) || e.target.closest(".product-menu a"))) abierto.open = false;
  });
  document.addEventListener("keydown", e => {
    const abierto = $(".nav-products[open]");
    if (e.key === "Escape" && abierto && !$("dialog[open]")) { abierto.open = false; $("summary", abierto).focus(); }
  });
  const busqueda = $("#search-dialog"), disparador = $("#open-search");
  if (busqueda && disparador) {
    const entrada = $("#search-term", busqueda);
    const filtrar = () => {
      const q = sinTildes(entrada.value.trim());
      let n = 0;
      $$("#search-results li", busqueda).forEach(li => {
        const a = $("a", li); const ok = sinTildes(a.textContent + " " + (a.dataset.claves || "")).includes(q);
        li.hidden = !ok; if (ok) n++;
      });
      $("#search-count", busqueda).textContent = `${n} resultados`;
    };
    disparador.addEventListener("click", () => { entrada.value = ""; filtrar(); busqueda.showModal(); entrada.focus(); });
    entrada.addEventListener("input", filtrar);
    busqueda.addEventListener("close", () => disparador.focus());
  }
  $(".skip")?.addEventListener("click", e => { e.preventDefault(); $("main").focus(); });

  // ---------- Botones que envían: ayuda de interfaz contra doble pulsación ----------
  // (La protección real contra duplicados está en el servidor y en la restricción única de SQLite.)
  document.addEventListener("submit", e => {
    const form = e.target;
    if (form.dataset.confirmar && !confirm(form.dataset.confirmar)) { e.preventDefault(); return; }
    const boton = e.submitter && e.submitter.dataset.enviando ? e.submitter : $("button[data-enviando]", form);
    if (boton && !e.defaultPrevented && form.id !== "form-concurrente") {
      setTimeout(() => { boton.disabled = true; boton.textContent = boton.dataset.enviando; }, 0);
    }
  });

  // ---------- Saldo: ocultar/mostrar (preferencia local) ----------
  const saldo = $("#balance-value"), botonSaldo = $("[data-ocultar-saldo]");
  if (saldo && botonSaldo) {
    const aplicar = oculto => {
      saldo.textContent = oculto ? "S/ ••••••" : saldo.dataset.saldo;
      botonSaldo.textContent = oculto ? "Mostrar saldo" : "Ocultar saldo";
    };
    aplicar(pref.get("ocultar-saldo") === "1");
    botonSaldo.addEventListener("click", () => {
      const oculto = !saldo.textContent.includes("••");
      pref.set("ocultar-saldo", oculto ? "1" : "0"); aplicar(oculto);
    });
  }

  // ---------- Envío: canal, contacto, montos y diálogo de revisión ----------
  if (pagina === "enviar") {
    const form = $("#bank-send"), destino = $("#bank-target"), mascara = $("#bank-mask"), monto = $("#bank-amount");
    const etiqueta = $("[data-etiqueta-destino]");
    const canal = () => $("input[name=Canal]:checked", form)?.value || "BCP";
    const pintar = () => {
      const op = destino.selectedOptions[0];
      const yape = canal() === "Yape";
      etiqueta.textContent = yape ? "Celular de destino" : "Cuenta de destino";
      mascara.value = op && op.value ? (yape ? op.dataset.yape : op.dataset.bcp) : "Selecciona un contacto";
      $$(".canal-switch label").forEach(l => l.classList.toggle("activo", $("input", l).checked));
    };
    $$("input[name=Canal]", form).forEach(r => r.addEventListener("change", pintar));
    destino.addEventListener("change", pintar);
    pintar();
    const error = $("#bank-error");
    const marcar = (campo, msg) => { $("span", error).textContent = msg; error.hidden = false; campo.setAttribute("aria-invalid", "true"); campo.focus(); };
    form.addEventListener("input", () => { if (!error.hidden && !$("#bank-dialog")) { error.hidden = true; } destino.removeAttribute("aria-invalid"); monto.removeAttribute("aria-invalid"); });
    $$("[data-preset]", form).forEach(b => b.addEventListener("click", () => { monto.value = b.dataset.preset; monto.focus(); }));
    // Validación rápida en el navegador; el servidor vuelve a validar todo.
    form.addEventListener("submit", e => {
      if (!destino.value) { e.preventDefault(); return marcar(destino, "Selecciona un destinatario para continuar."); }
      if (!/^\d{1,7}([.,]\d{1,2})?$/.test(monto.value.trim()) || Number(monto.value.replace(",", ".")) <= 0) {
        e.preventDefault(); return marcar(monto, "Ingresa un monto positivo, con hasta dos decimales.");
      }
    });
    const campoError = $("[aria-invalid=true]", form);
    if (campoError && !$("#bank-dialog")) campoError.focus();

    const dialogo = $("#bank-dialog");
    if (dialogo) {
      const volver = $("#btn-continuar");
      dialogo.showModal();
      $("#send-title", dialogo).focus();              // el foco entra al diálogo
      $("[data-cerrar-dialogo]", dialogo).addEventListener("click", () => dialogo.close());
      dialogo.addEventListener("close", () => { volver.focus(); }); // y vuelve al botón que lo abrió
    }
  }

  // ---------- Historial: filtros que consultan al servidor ----------
  if (pagina === "historial") {
    const form = $("#form-filtro");
    let t;
    $$("select", form).forEach(s => s.addEventListener("change", () => form.requestSubmit()));
    $("#bank-query", form).addEventListener("input", () => { clearTimeout(t); t = setTimeout(() => form.requestSubmit(), 450); });
    const q = $("#bank-query", form);
    if (q.value) { q.focus(); q.setSelectionRange(q.value.length, q.value.length); }
  }

  // ---------- Constancia: copiar e imprimir ----------
  $("[data-copiar]")?.addEventListener("click", async e => {
    const ref = e.currentTarget.dataset.copiar, estado = $("#bank-copy-status");
    try { await navigator.clipboard.writeText(ref); estado.textContent = "Código copiado."; }
    catch { estado.textContent = `Código de la operación: ${ref}`; }
  });
  $("[data-imprimir]")?.addEventListener("click", () => window.print());

  // ---------- Continuidad: 10 solicitudes simultáneas con la misma clave ----------
  const concurrente = $("#form-concurrente");
  if (concurrente) {
    concurrente.addEventListener("submit", async e => {
      e.preventDefault();
      const salida = $("#resultado-concurrente");
      const boton = $("button", concurrente);
      boton.disabled = true; salida.innerHTML = "<li>Enviando…</li>";
      const token = $("input[name=__RequestVerificationToken]", concurrente).value;
      const clave = (crypto.randomUUID ? crypto.randomUUID() : String(Date.now()) + Math.random()).replace(/[^A-Za-z0-9-]/g, "");
      const cuerpo = () => {
        const d = new FormData();
        d.append("__RequestVerificationToken", token); d.append("Canal", "BCP"); d.append("ContactoId", concurrente.dataset.contacto);
        d.append("Importe", "10.00"); d.append("Concepto", "Prueba concurrente"); d.append("ClaveIdempotencia", clave);
        return d;
      };
      const respuestas = await Promise.all(Array.from({ length: 10 }, () =>
        fetch(concurrente.action, { method: "POST", body: cuerpo(), headers: { Accept: "application/json" } })
          .then(r => r.json()).catch(() => ({ resultado: "Error de red" }))));
      const refs = new Set(respuestas.map(r => r.referencia).filter(Boolean));
      salida.innerHTML = respuestas.map((r, i) => `<li>Solicitud ${i + 1}: <strong>${r.resultado}</strong> · ${r.referencia ?? "—"}</li>`).join("") +
        `<li class="resumen"><strong>${refs.size === 1 ? "Una sola referencia: " + [...refs][0] : refs.size + " referencias distintas"}</strong> · registradas: ${respuestas.filter(r => r.resultado === "Registrada").length}, reintentos resueltos: ${respuestas.filter(r => r.resultado === "Reintento").length}</li>`;
      boton.disabled = false;
    });
  }

  // ---------- Recorridos complementarios del portal ----------
  const etapas = (raiz, actual) => {
    $$("[data-etapa]", raiz).forEach(el => el.hidden = el.dataset.etapa !== actual);
    const h = $(`[data-etapa="${actual}"] h1, [data-etapa="${actual}"] h2`, raiz);
    if (h) { h.setAttribute("tabindex", "-1"); h.focus(); }
  };
  if (pagina === "prestamo") {
    const raiz = $("[data-flujo=prestamo]");
    const soles = v => "S/ " + v.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const valores = () => {
      const monto = Number($("input[name=monto]:checked").value), plazo = Number($("input[name=plazo]:checked").value), tasa = 0.0125;
      const cuota = Math.round(monto * tasa / (1 - Math.pow(1 + tasa, -plazo)) * 100) / 100;
      return { monto, plazo, cuota, total: Math.round(cuota * plazo * 100) / 100 };
    };
    const pintar = () => {
      const v = valores();
      $$("[data-cotizacion]", raiz).forEach(c => c.innerHTML = `<div class="quote-top"><span class="demo-tag">Entorno académico</span><p>Cuota mensual referencial</p><strong>${soles(v.cuota)}</strong></div>
        <dl class="detail-list"><div><dt>Monto</dt><dd>${soles(v.monto)}</dd></div><div><dt>Plazo</dt><dd>${v.plazo} meses</dd></div><div><dt>Total estimado a pagar</dt><dd>${soles(v.total)}</dd></div></dl>`);
      $("[data-resultado-texto]", raiz).textContent = `Calculaste un préstamo de ${soles(v.monto)} a ${v.plazo} meses, con una cuota referencial de ${soles(v.cuota)}.`;
    };
    const pasos = ["simular", "revisar", "resultado"];
    const ir = destino => {
      etapas(raiz, destino);
      const i = pasos.indexOf(destino);
      $$("[data-stepper] li", raiz).forEach((li, j) => { li.classList.toggle("complete", j < i); if (j === i) li.setAttribute("aria-current", "step"); else li.removeAttribute("aria-current"); });
    };
    raiz.addEventListener("change", pintar);
    raiz.addEventListener("click", e => { const b = e.target.closest("[data-ir]"); if (b) ir(b.dataset.ir); });
    pintar();
  }
  if (pagina === "pos") {
    const raiz = $("[data-flujo=pos]");
    const docs = { DNI: { d: 8, ej: "00018427", n: "DNI" }, CE: { d: 9, ej: "000038156", n: "Carné de extranjería" } };
    const tipo = $("#doc-type"), numero = $("#doc-number"), err = $("#doc-error");
    const marcar = msg => { $("span", err).textContent = msg; err.hidden = !msg; if (msg) { numero.setAttribute("aria-invalid", "true"); numero.focus(); } else numero.removeAttribute("aria-invalid"); };
    raiz.addEventListener("click", e => { const b = e.target.closest("[data-ir]"); if (b) etapas(raiz, b.dataset.ir); });
    tipo.addEventListener("change", () => { const d = docs[tipo.value]; numero.value = ""; $("#doc-hint").textContent = `${d.d} dígitos seguidos. Usa el ejemplo ficticio ${d.ej}.`; marcar(""); });
    numero.addEventListener("input", () => marcar(""));
    $("#pos-form").addEventListener("submit", e => {
      e.preventDefault();
      const d = docs[tipo.value], v = numero.value.trim();
      if (!new RegExp(`^\\d{${d.d}}$`).test(v)) return marcar(`Ingresa ${d.d} dígitos sin espacios ni letras. Ejemplo: ${d.ej}.`);
      if (v !== d.ej) return marcar(`Usa el número ficticio ${d.ej} para continuar con la demostración.`);
      $("[data-pos-tipo]").textContent = d.n; $("[data-pos-numero]").textContent = v; etapas(raiz, "revision");
    });
  }
  if (pagina === "bloqueo") {
    const raiz = $("[data-flujo=bloqueo]"), check = $("#demo-card"), seguir = $("#block-continue"), dlg = $("#block-confirm");
    check.addEventListener("change", () => seguir.disabled = !check.checked);
    seguir.addEventListener("click", () => {
      dlg.returnValue = ""; dlg.showModal(); $("#block-title").focus();
      dlg.addEventListener("close", () => { seguir.focus(); if (dlg.returnValue === "confirm") etapas(raiz, "hecho"); }, { once: true });
    });
  }
})();
