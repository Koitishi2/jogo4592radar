param(
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$Change,
    [string]$DotnetPath = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $projectDir 'jogo4592radar.csproj'
$readme = Join-Path $projectDir 'README.md'
$originalProject = Get-Content $project -Raw
$match = [regex]::Match($originalProject, '<Version>(\d+)\.(\d+)\.(\d+)</Version>')
if (-not $match.Success) { throw 'Versão inválida no projeto.' }

$major = [int]$match.Groups[1].Value
$minor = [int]$match.Groups[2].Value
$patch = [int]$match.Groups[3].Value
if ($patch -ge 99) { $minor++; $patch = 0 } else { $patch++ }
$next = "$major.$minor.$patch"
$updatedProject = $originalProject.Remove($match.Index, $match.Length).Insert($match.Index, "<Version>$next</Version>")

try {
    Set-Content -Path $project -Value $updatedProject -NoNewline -Encoding UTF8
    & $DotnetPath build $project -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Compilação falhou.' }
    & (Join-Path $projectDir 'test-smoke.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Teste inicial falhou.' }
} catch {
    Set-Content -Path $project -Value $originalProject -NoNewline -Encoding UTF8
    throw
}

$originalReadme = Get-Content $readme -Raw
$date = Get-Date -Format 'yyyy-MM-dd'
$entry = "- **${next} ($date):** $Change"
$updatedReadme = $originalReadme -replace '(?m)^# jogo4592radar \d+\.\d+\.\d+', "# jogo4592radar $next"
$updatedReadme = $updatedReadme -replace '(?m)^(## Histórico de versões\r?\n)', "`$1`r`n$entry`r`n"
Set-Content -Path $readme -Value $updatedReadme -NoNewline -Encoding UTF8
Write-Output "Versão $next compilada, testada e registrada no README. Revise e faça o commit."
