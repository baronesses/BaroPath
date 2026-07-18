param(
    [string]$Destination = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($Destination)) {
    $Destination = Join-Path $repoRoot "BaroManager\tools\everything"
}

$destinationFull = [System.IO.Path]::GetFullPath($Destination)
$tempDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("baropath-everything-" + [Guid]::NewGuid().ToString("N"))

New-Item -ItemType Directory -Path $destinationFull -Force | Out-Null
New-Item -ItemType Directory -Path $tempDirectory -Force | Out-Null

try {
    $everythingArchive = Join-Path $tempDirectory "Everything.zip"
    $esArchive = Join-Path $tempDirectory "ES.zip"

    Invoke-WebRequest `
        -Uri "https://www.voidtools.com/Everything-1.4.1.1032.x64.zip" `
        -OutFile $everythingArchive

    Invoke-WebRequest `
        -Uri "https://www.voidtools.com/ES-1.1.0.30.x64.zip" `
        -OutFile $esArchive

    $everythingExtract = Join-Path $tempDirectory "everything"
    $esExtract = Join-Path $tempDirectory "es"

    Expand-Archive -LiteralPath $everythingArchive -DestinationPath $everythingExtract -Force
    Expand-Archive -LiteralPath $esArchive -DestinationPath $esExtract -Force

    $everythingExe = Get-ChildItem -LiteralPath $everythingExtract -Filter "Everything.exe" -Recurse | Select-Object -First 1
    $esExe = Get-ChildItem -LiteralPath $esExtract -Filter "es.exe" -Recurse | Select-Object -First 1

    if ($null -eq $everythingExe -or $null -eq $esExe) {
        throw "The official archives did not contain Everything.exe and es.exe."
    }

    $expectedEverythingHash = "F191F756996A14A11E5445FA7103D302EFD510CF2FBF920E6C0C8ED51D512E36"
    $expectedEsHash = "9A9B851F9DA14A29626126D9B5F8EF71B569B3CF7E3E70BFBF57F4F00A9B9383"
    $everythingHash = (Get-FileHash -LiteralPath $everythingExe.FullName -Algorithm SHA256).Hash
    $esHash = (Get-FileHash -LiteralPath $esExe.FullName -Algorithm SHA256).Hash

    if ($everythingHash -ne $expectedEverythingHash -or $esHash -ne $expectedEsHash) {
        throw "The downloaded voidtools binaries did not match the pinned SHA256 hashes."
    }

    Copy-Item -LiteralPath $everythingExe.FullName -Destination (Join-Path $destinationFull "Everything.exe") -Force
    Copy-Item -LiteralPath $esExe.FullName -Destination (Join-Path $destinationFull "es.exe") -Force

    Write-Host "Everything and ES are ready in $destinationFull"
}
finally {
    if (Test-Path -LiteralPath $tempDirectory) {
        Remove-Item -LiteralPath $tempDirectory -Recurse -Force
    }
}
