param(
    [switch]$SkipBuild,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $projectRoot
try {
    if (-not $SkipTests) {
        if ($SkipBuild) {
            & (Join-Path $projectRoot 'tests\run-ui.ps1') -SkipBuild
        } else {
            & (Join-Path $projectRoot 'tests\run-ui.ps1')
        }
        if ($LASTEXITCODE -ne 0) { throw "UI regression failed." }
    } elseif (-not $SkipBuild) {
        & (Join-Path $projectRoot 'build.ps1')
    }

    $binary = Join-Path $projectRoot 'dist\UmiAchievementClaimer.exe'
    if (-not (Test-Path -LiteralPath $binary)) { throw "Missing current dist executable." }
    $source = [IO.File]::ReadAllText((Join-Path $projectRoot 'src\Properties\AssemblyInfo.cs'))
    $versionMatch = [regex]::Match($source, 'AssemblyVersion\("(\d+\.\d+\.\d+)\.\d+"\)')
    if (-not $versionMatch.Success) { throw 'Could not determine application version.' }
    $version = $versionMatch.Groups[1].Value
    $output = Join-Path $projectRoot ('dist\UmiAchievementClaimer-v' + $version + '-windows-x64-portable.zip')
    $folder = Join-Path $projectRoot 'dist\portable-stage'
    if (Test-Path -LiteralPath $folder) { Remove-Item -LiteralPath $folder -Recurse -Force }
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    try {
        Copy-Item -LiteralPath $binary -Destination $folder -Force
        Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $folder -Force
        $exeHash = (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash
        [IO.File]::WriteAllText((Join-Path $folder 'SHA256SUMS.txt'),
            $exeHash + '  UmiAchievementClaimer.exe' + [Environment]::NewLine,
            [Text.UTF8Encoding]::new($false))
        if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Force }
        Compress-Archive -LiteralPath @(
            (Join-Path $folder 'UmiAchievementClaimer.exe'),
            (Join-Path $folder 'README.md'),
            (Join-Path $folder 'SHA256SUMS.txt')
        ) -DestinationPath $output -CompressionLevel Optimal
    } finally {
        if (Test-Path -LiteralPath $folder) { Remove-Item -LiteralPath $folder -Recurse -Force }
    }
    $zipHash = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash
    $checksumPath = Join-Path $projectRoot 'dist\SHA256SUMS.release.txt'
    [IO.File]::WriteAllLines($checksumPath, @(
        "$exeHash  UmiAchievementClaimer.exe",
        "$zipHash  $(Split-Path $output -Leaf)"
    ), [Text.UTF8Encoding]::new($false))
    Write-Host "Portable release prepared: $output"
    Write-Host "Checksums: $checksumPath"
}
finally {
    Pop-Location
}
