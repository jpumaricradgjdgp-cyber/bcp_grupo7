"""Monta BCP_Tutorial_Final.mp4: grabación + narración sincronizada + subtítulos (SRT y pista suave).
Uso: python -I scripts/video/montar.py
"""
import json
import re
import subprocess
import sys
import wave
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from guion import BLOQUES, TRAMOS  # noqa: E402

RAIZ = Path(__file__).resolve().parents[2]
VIDEO = RAIZ / "video"
AUDIO = VIDEO / "audio"
CRUDO = VIDEO / "_crudo"


def dur_wav(ident):
    with wave.open(str(AUDIO / f"{ident}.wav")) as w:
        return w.getnframes() / w.getframerate()


def ts(seg):
    ms = int(round(seg * 1000))
    h, ms = divmod(ms, 3_600_000)
    m, ms = divmod(ms, 60_000)
    s, ms = divmod(ms, 1000)
    return f"{h:02}:{m:02}:{s:02},{ms:03}"


def mmss(seg):
    return f"{int(seg // 60)}:{int(seg % 60):02}"


def frases(texto, maximo=84):
    partes = [p.strip() for p in re.split(r"(?<=[.:;?!])\s+", texto) if p.strip()]
    salida = []
    for p in partes:
        while len(p) > maximo:
            corte = p.rfind(", ", 0, maximo)
            corte = corte + 1 if corte > 30 else p.rfind(" ", 0, maximo)
            salida.append(p[:corte].strip())
            p = p[corte:].strip()
        salida.append(p)
    return salida


def envolver(linea, ancho=44):
    if len(linea) <= ancho:
        return linea
    corte = linea.rfind(" ", 0, len(linea) // 2 + 8)
    return linea[:corte] + "\n" + linea[corte + 1:]


def main():
    datos = json.loads((CRUDO / "linea_tiempo.json").read_text(encoding="utf-8"))
    inicio = {t["id"]: t["inicio"] for t in datos["tramos"]}
    fin_video = datos["tramos"][-1]["fin"] + 0.8
    textos = {i: (b, t) for i, b, t in TRAMOS}

    # ---- Subtítulos
    srt, n = [], 1
    for i, _, texto in TRAMOS:
        d = dur_wav(i)
        piezas = frases(texto)
        total = sum(len(x) for x in piezas)
        t = inicio[i]
        for x in piezas:
            dt = d * len(x) / total
            srt.append(f"{n}\n{ts(t)} --> {ts(t + dt - 0.05)}\n{envolver(x)}\n")
            n += 1
            t += dt
    (VIDEO / "BCP_Tutorial_Final.srt").write_text("\n".join(srt), encoding="utf-8")

    # ---- Pista de narración (cada tramo en su instante)
    entradas, filtros = [], []
    for k, (i, _, _) in enumerate(TRAMOS):
        entradas += ["-i", str(AUDIO / f"{i}.wav")]
        ms = int(inicio[i] * 1000)
        filtros.append(f"[{k}:a]aresample=48000,adelay={ms}|{ms}[a{k}]")
    mezcla = "".join(f"[a{k}]" for k in range(len(TRAMOS)))
    filtros.append(f"{mezcla}amix=inputs={len(TRAMOS)}:normalize=0,apad,atrim=0:{fin_video:.2f},"
                   "highpass=f=70,acompressor=threshold=-18dB:ratio=2.5:attack=5:release=120,loudnorm=I=-16:TP=-1.5:LRA=9[voz]")
    narracion = CRUDO / "narracion.m4a"
    subprocess.run(["ffmpeg", "-y", "-v", "error", *entradas, "-filter_complex", ";".join(filtros), "-map", "[voz]",
                    "-ac", "2", "-c:a", "aac", "-b:a", "160k", str(narracion)], check=True)

    # ---- Video final 1080p con narración y subtítulos suaves
    salida = VIDEO / "BCP_Tutorial_Final.mp4"
    subprocess.run(["ffmpeg", "-y", "-v", "error", "-i", str(CRUDO / "grabacion.webm"), "-i", str(narracion),
                    "-i", str(VIDEO / "BCP_Tutorial_Final.srt"),
                    "-map", "0:v", "-map", "1:a", "-map", "2:s", "-t", f"{fin_video:.2f}",
                    "-vf", "fps=30,scale=1920:1080:flags=lanczos,format=yuv420p",
                    "-c:v", "libx264", "-preset", "slow", "-crf", "20", "-c:a", "copy", "-c:s", "mov_text",
                    "-metadata:s:s:0", "language=spa", "-metadata:s:a:0", "language=spa",
                    "-metadata", "title=BCP · Tutorial del MVP · Grupo 7", "-movflags", "+faststart", str(salida)], check=True)

    # ---- Guion palabra por palabra con tiempos reales y plan de edición
    lineas = ["# Guion del video tutorial · BCP_Tutorial_Final.mp4", "",
              f"Duración final: **{mmss(fin_video)}** · 1920×1080 · 30 fps · H.264 + AAC · subtítulos en español (pista suave y `BCP_Tutorial_Final.srt`).",
              "Voz: Microsoft Sabina (español de México), la voz en español disponible en este Windows. Para una voz peruana, regraba la narración con este texto y reemplaza los WAV de `video/audio/` (misma duración aproximada) antes de ejecutar `montar.py`.", ""]
    bloque_actual = None
    for t in datos["tramos"]:
        b, texto = textos[t["id"]]
        if b != bloque_actual:
            bloque_actual = b
            lineas += ["", f"## {BLOQUES[b]}", ""]
        lineas.append(f"**[{mmss(t['inicio'])}–{mmss(t['fin'])}] {t['id']}** — {texto}")
        lineas.append("")
    lineas += ["", "## Datos de la toma", "",
               f"- Referencia del envío de S/ 50.00: **{datos['ref50']}**",
               f"- Operación nueva del escenario de falla (S/ 20.00 por Yape): **{datos['ref20']}**",
               f"- Reinicio del servidor en el ejercicio: detenido {datos['reinicio']['detencion']}, disponible {datos['reinicio']['recuperacion']} "
               f"(**{datos['reinicio']['segundos']} s**, solo este ejercicio local; la meta del PESI es RTO ≤ 15 min).", ""]
    (VIDEO / "BCP_Guion_Tutorial.md").write_text("\n".join(lineas), encoding="utf-8")
    print("Listo:", salida, f"{mmss(fin_video)}")


if __name__ == "__main__":
    main()
