param([string]$Configuration = 'Release')

$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $projectDir 'jogo4592radar.csproj'
$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version
$exe = Join-Path $projectDir "bin\$Configuration\net9.0-windows\jogo4592radar.exe"
if (-not (Test-Path $exe)) { throw "Executável não encontrado: $exe" }

$process = Start-Process -FilePath $exe -WindowStyle Hidden -PassThru
try {
    Start-Sleep -Seconds 2
    $process.Refresh()
    if ($process.HasExited) { throw "Aplicativo encerrou logo após iniciar (código $($process.ExitCode))." }
    if ($process.MainWindowTitle -notlike "*${version}*Simulação*") {
        throw "Título inesperado: $($process.MainWindowTitle)"
    }
    Write-Output "PASS: aplicativo $version iniciou e abriu a janela de controle."
} finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id }
}
