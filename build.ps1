$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDirectory = Join-Path $projectRoot 'src'
$buildDirectory = Join-Path $projectRoot 'build'
$distributionDirectory = Join-Path $projectRoot 'dist'
$assetDirectory = Join-Path $projectRoot 'assets'

$workerSource = Join-Path $sourceDirectory 'Worker\UniversalAchievementWorker.cs'
$applicationSources = Get-ChildItem -LiteralPath $sourceDirectory -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notlike (Join-Path $sourceDirectory 'Worker\*') } |
    Sort-Object FullName |
    ForEach-Object { $_.FullName }
$worker32 = Join-Path $buildDirectory 'UniversalAchievementWorker_x86.exe'
$worker64 = Join-Path $buildDirectory 'UniversalAchievementWorker_x64.exe'
$application = Join-Path $distributionDirectory 'UmiAchievementClaimer.exe'
$packageApplication = Join-Path $projectRoot 'UmiAchievementClaimer.exe'
$logo = Join-Path $assetDirectory 'umi-logo.png'
$icon = Join-Path $assetDirectory 'umi-app.ico'

$compiler32 = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$compiler64 = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $compiler32)) {
    throw "Missing 32-bit .NET Framework C# compiler: $compiler32"
}
if (-not (Test-Path -LiteralPath $compiler64)) {
    throw "Missing 64-bit .NET Framework C# compiler: $compiler64"
}
if (-not (Test-Path -LiteralPath $workerSource)) {
    throw "Missing worker source: $workerSource"
}
if ($applicationSources.Count -eq 0) {
    throw "No application source files were found under: $sourceDirectory"
}
if (-not (Test-Path -LiteralPath $logo)) {
    throw "Missing application logo: $logo (run tools\prepare-brand-assets.ps1)"
}
if (-not (Test-Path -LiteralPath $icon)) {
    throw "Missing application icon: $icon (run tools\prepare-brand-assets.ps1)"
}

New-Item -ItemType Directory -Force -Path $buildDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $distributionDirectory | Out-Null

& $compiler32 /nologo /target:exe /platform:x86 /optimize+ /out:$worker32 $workerSource
if ($LASTEXITCODE -ne 0) { throw "x86 worker compilation failed with exit code $LASTEXITCODE" }

& $compiler64 /nologo /target:exe /platform:x64 /optimize+ /out:$worker64 $workerSource
if ($LASTEXITCODE -ne 0) { throw "x64 worker compilation failed with exit code $LASTEXITCODE" }

$resource32 = '/resource:' + $worker32 + ',AchievementWorker32'
$resource64 = '/resource:' + $worker64 + ',AchievementWorker64'
$logoResource = '/resource:' + $logo + ',UmiBrandLogo'
$win32Icon = '/win32icon:' + $icon

& $compiler64 /nologo /target:winexe /platform:x64 /optimize+ `
    /reference:System.Windows.Forms.dll `
    /reference:System.Drawing.dll `
    /reference:System.Core.dll `
    /reference:System.Web.Extensions.dll `
    /reference:System.IO.Compression.dll `
    /reference:System.IO.Compression.FileSystem.dll `
    /out:$application `
    $win32Icon `
    $resource32 `
    $resource64 `
    $logoResource `
    $applicationSources
if ($LASTEXITCODE -ne 0) { throw "Application compilation failed with exit code $LASTEXITCODE" }

Copy-Item -LiteralPath $application -Destination $packageApplication -Force

$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $packageApplication
$checksumPath = Join-Path $projectRoot 'SHA256SUMS.txt'
[IO.File]::WriteAllText($checksumPath, $hash.Hash + '  UmiAchievementClaimer.exe' + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
Write-Host "Build complete: $packageApplication"
Write-Host "SHA-256: $($hash.Hash)"
