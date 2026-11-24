# Genera un WAV por tramo con la voz en español instalada en Windows (System.Speech / SAPI).
# Uso: powershell -File narrar.ps1 -Entrada tramos.json -Salida carpeta
param([string]$Entrada, [string]$Salida, [string]$Voz = "Microsoft Sabina Desktop", [int]$Velocidad = 0)
Add-Type -AssemblyName System.Speech
New-Item -ItemType Directory -Force $Salida | Out-Null
$tramos = Get-Content $Entrada -Raw -Encoding UTF8 | ConvertFrom-Json
$sintetizador = New-Object System.Speech.Synthesis.SpeechSynthesizer
$sintetizador.SelectVoice($Voz)
$sintetizador.Rate = $Velocidad
$formato = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(24000, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
foreach ($t in $tramos) {
    $archivo = Join-Path $Salida ($t.id + ".wav")
    $sintetizador.SetOutputToWaveFile($archivo, $formato)
    $sintetizador.Speak($t.texto)
    $sintetizador.SetOutputToNull()
    Write-Output "$($t.id) ok"
}
$sintetizador.Dispose()
