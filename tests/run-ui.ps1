param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $projectRoot
try {
    if (-not $SkipBuild) { & (Join-Path $projectRoot 'build.ps1') }
    $application = Join-Path $projectRoot 'dist\\UmiAchievementClaimer.exe'
    if (-not (Test-Path -LiteralPath $application)) { throw "Build the application first: $application" }
    $buildDirectory = Join-Path $projectRoot 'build'
    New-Item -ItemType Directory -Force -Path $buildDirectory | Out-Null
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $testApplication = Join-Path $buildDirectory 'UiRegression.exe'
    & $compiler /nologo /target:exe /platform:x64 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /out:$testApplication (Join-Path $PSScriptRoot 'UiRegression.cs')
    if ($LASTEXITCODE -ne 0) { throw "UI regression compilation failed: $LASTEXITCODE" }
    & $testApplication $application (Join-Path $projectRoot '.impeccable\review')
    if ($LASTEXITCODE -ne 0) { throw "UI regression failed: $LASTEXITCODE" }
}
finally { Pop-Location }
