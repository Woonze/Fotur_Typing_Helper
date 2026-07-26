param(
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'
$workspaceRoot = Split-Path -Parent $PSScriptRoot
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    $localDotnet = Join-Path $env:LOCALAPPDATA 'FoturTypingHelper\dotnet\dotnet.exe'
    if (-not (Test-Path -LiteralPath $localDotnet)) { throw '.NET 8 SDK not found.' }
    $dotnetPath = $localDotnet
} else { $dotnetPath = $dotnet.Source }

function Assert-UnderWorkspace([string]$Path, [string]$Root) {
    $resolvedRoot = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedPath = [IO.Path]::GetFullPath($Path)
    if (-not $resolvedPath.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe path outside workspace: $resolvedPath"
    }
}

function Remove-DisallowedRuntimes([string]$PublishDir, [string[]]$AllowedRuntimeNames) {
    $runtimes = Join-Path $PublishDir 'runtimes'
    if (-not (Test-Path -LiteralPath $runtimes)) { return }
    Get-ChildItem -LiteralPath $runtimes -Directory | ForEach-Object {
        if ($AllowedRuntimeNames -notcontains $_.Name) {
            Assert-UnderWorkspace $_.FullName $PublishDir
            Remove-Item -LiteralPath $_.FullName -Recurse -Force
        }
    }
}

function Move-Symbols([string]$PublishDir, [string]$SymbolsDir) {
    if (Test-Path -LiteralPath $SymbolsDir) { Remove-Item -LiteralPath $SymbolsDir -Recurse -Force }
    New-Item -ItemType Directory -Path $SymbolsDir | Out-Null
    $publishRoot = [IO.Path]::GetFullPath($PublishDir).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    Get-ChildItem -LiteralPath $PublishDir -Filter *.pdb -Recurse | ForEach-Object {
        $fullName = [IO.Path]::GetFullPath($_.FullName)
        if (-not $fullName.StartsWith($publishRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Unsafe symbol path outside publish directory: $fullName"
        }
        $relative = $fullName.Substring($publishRoot.Length)
        $target = Join-Path $SymbolsDir $relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
        Move-Item -LiteralPath $_.FullName -Destination $target -Force
    }
}

function Invoke-Checked([scriptblock]$Command, [string]$Description) {
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

$versionFile = Get-Content (Join-Path $workspaceRoot 'Directory.Build.props') -Raw
$version = [regex]::Match($versionFile, '<Version>([^<]+)</Version>').Groups[1].Value
if (-not $version) { throw 'Version not found in Directory.Build.props.' }

$artifactsDir = Join-Path $workspaceRoot 'artifacts'
$publishDir = Join-Path $artifactsDir 'publish'
$symbolsDir = Join-Path $artifactsDir "symbols-win-x64"
$portableRoot = Join-Path $artifactsDir "FoturTypingHelper-$version-win-x64-portable"
$portableZip = Join-Path $artifactsDir "FoturTypingHelper-$version-win-x64-portable.zip"
$symbolsZip = Join-Path $artifactsDir "FoturTypingHelper-$version-win-x64-symbols.zip"

foreach ($path in @($publishDir, $symbolsDir, $portableRoot)) { Assert-UnderWorkspace $path $workspaceRoot }
foreach ($path in @($publishDir, $symbolsDir, $portableRoot)) {
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Recurse -Force }
}
foreach ($path in @($portableZip, $symbolsZip)) {
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
}
New-Item -ItemType Directory -Path $publishDir | Out-Null

Invoke-Checked { & $dotnetPath test (Join-Path $workspaceRoot 'FoturTypingHelper.sln') -c Release } 'dotnet test'
Invoke-Checked { & $dotnetPath publish (Join-Path $workspaceRoot 'src\FoturTypingHelper.App\FoturTypingHelper.App.csproj') `
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $publishDir } 'dotnet publish'

Remove-DisallowedRuntimes $publishDir @('win-x64')
Move-Symbols $publishDir $symbolsDir

$whisperRuntime = Join-Path $publishDir 'runtimes\win-x64\whisper.dll'
if (-not (Test-Path -LiteralPath $whisperRuntime)) {
    throw "Windows Whisper runtime is missing from publish output: $whisperRuntime"
}
if (Get-ChildItem -LiteralPath $publishDir -Filter *.pdb -Recurse | Select-Object -First 1) {
    throw 'PDB files are still present in Windows publish output.'
}

Copy-Item -LiteralPath (Join-Path $workspaceRoot 'LICENSE') -Destination (Join-Path $publishDir 'LICENSE') -Force
Copy-Item -LiteralPath (Join-Path $workspaceRoot 'THIRD_PARTY_NOTICES.md') -Destination (Join-Path $publishDir 'THIRD_PARTY_NOTICES.md') -Force
Copy-Item -LiteralPath $publishDir -Destination $portableRoot -Recurse
Compress-Archive -Path $portableRoot -DestinationPath $portableZip -CompressionLevel Optimal
if (Test-Path -LiteralPath $symbolsDir) {
    Compress-Archive -Path $symbolsDir -DestinationPath $symbolsZip -CompressionLevel Optimal
}

if (-not $SkipInstaller) {
    $iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if (-not $iscc) {
        $candidates = @(
            (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
            'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
        )
        foreach ($candidate in $candidates) {
            if (Test-Path -LiteralPath $candidate) { $iscc = Get-Item $candidate; break }
        }
    }
    if (-not $iscc) { throw 'Inno Setup 6 not found. Install JRSoftware.InnoSetup or use -SkipInstaller.' }
    $isccPath = if ($iscc.Source) { $iscc.Source } else { $iscc.FullName }
    Invoke-Checked { & $isccPath "/DAppVersion=$version" (Join-Path $workspaceRoot 'installer\FoturTypingHelper.iss') } 'Inno Setup'
}

$setup = Join-Path $workspaceRoot "artifacts\installer\FoturTypingHelper-Setup-$version-win-x64.exe"
$hashTargets = @($portableZip)
if (Test-Path -LiteralPath $setup) { $hashTargets = @($setup) + $hashTargets }
$hashLines = $hashTargets | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256
    "$($hash.Hash)  $([IO.Path]::GetFileName($_))"
}
[IO.File]::WriteAllLines((Join-Path $artifactsDir 'SHA256SUMS.txt'), $hashLines)
