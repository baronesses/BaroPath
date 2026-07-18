param(
    [string]$OutputDirectory = "",
    [string]$EverythingSourceDirectory = "",
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "BaroManager\BaroManager.csproj"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "release"
}

if ([string]::IsNullOrWhiteSpace($EverythingSourceDirectory)) {
    $EverythingSourceDirectory = Join-Path $repoRoot "BaroManager\tools\everything"
}

$outputFull = [System.IO.Path]::GetFullPath($OutputDirectory)
$repoFull = [System.IO.Path]::GetFullPath($repoRoot)

if (-not $outputFull.StartsWith($repoFull, [StringComparison]::OrdinalIgnoreCase)) {
    throw "The release output must stay inside the repository: $repoFull"
}

$everythingFull = [System.IO.Path]::GetFullPath($EverythingSourceDirectory)
$everythingExe = Join-Path $everythingFull "Everything.exe"
$esExe = Join-Path $everythingFull "es.exe"
$everythingLicense = Join-Path $everythingFull "License.txt"

foreach ($requiredFile in @($everythingExe, $esExe, $everythingLicense)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required release dependency is missing: $requiredFile"
    }
}

$staging = Join-Path $repoRoot "publish\staging"
$package = Join-Path $outputFull "BaroPath"

foreach ($directory in @($staging, $package)) {
    $full = [System.IO.Path]::GetFullPath($directory)

    if (-not $full.StartsWith($repoFull, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean a directory outside the repository: $full"
    }

    if (Test-Path -LiteralPath $full) {
        Remove-Item -LiteralPath $full -Recurse -Force
    }

    New-Item -ItemType Directory -Path $full -Force | Out-Null
}

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $staging `
    -p:PublishSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$publishedExe = Join-Path $staging "BaroPath.exe"

if (-not (Test-Path -LiteralPath $publishedExe -PathType Leaf)) {
    throw "dotnet publish did not produce BaroPath.exe."
}

Copy-Item -LiteralPath $publishedExe -Destination (Join-Path $package "BaroPath.exe")
Copy-Item -LiteralPath (Join-Path $repoRoot "BaroManager\LICENSE.txt") -Destination (Join-Path $package "LICENSE.txt")
Copy-Item -LiteralPath (Join-Path $repoRoot "BaroManager\THIRD_PARTY_NOTICES.txt") -Destination (Join-Path $package "THIRD_PARTY_NOTICES.txt")

$everythingTarget = Join-Path $package "tools\everything"
New-Item -ItemType Directory -Path $everythingTarget -Force | Out-Null
Copy-Item -LiteralPath $everythingExe -Destination (Join-Path $everythingTarget "Everything.exe")
Copy-Item -LiteralPath $esExe -Destination (Join-Path $everythingTarget "es.exe")
Copy-Item -LiteralPath $everythingLicense -Destination (Join-Path $everythingTarget "License.txt")

$version = (Get-Item -LiteralPath (Join-Path $package "BaroPath.exe")).VersionInfo.ProductVersion
$version = ($version -split '\+')[0]

if (-not $SkipZip) {
    $zipPath = Join-Path $outputFull "BaroPath-v$version-win-x64.zip"

    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $package "*") -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "Release archive: $zipPath"
}

Write-Host "Clean release directory: $package"
