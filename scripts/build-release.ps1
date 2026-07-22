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

$publishDir = Join-Path $workspaceRoot 'artifacts\publish'
$resolvedWorkspace = [IO.Path]::GetFullPath($workspaceRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$resolvedPublish = [IO.Path]::GetFullPath($publishDir)
if (-not $resolvedPublish.StartsWith($resolvedWorkspace, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe publish directory: $resolvedPublish"
}
if (Test-Path -LiteralPath $resolvedPublish) { Remove-Item -LiteralPath $resolvedPublish -Recurse -Force }
New-Item -ItemType Directory -Path $resolvedPublish | Out-Null
& $dotnetPath test (Join-Path $workspaceRoot 'FoturTypingHelper.sln') -c Release
& $dotnetPath publish (Join-Path $workspaceRoot 'src\FoturTypingHelper.App\FoturTypingHelper.App.csproj') `
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $publishDir

$whisperRuntime = Join-Path $publishDir 'runtimes\win-x64\whisper.dll'
if (-not (Test-Path -LiteralPath $whisperRuntime)) {
    throw "Windows Whisper runtime is missing from publish output: $whisperRuntime"
}

$versionFile = Get-Content (Join-Path $workspaceRoot 'Directory.Build.props') -Raw
$version = [regex]::Match($versionFile, '<Version>([^<]+)</Version>').Groups[1].Value
if (-not $version) { throw 'Version not found in Directory.Build.props.' }
$artifactsDir = Join-Path $workspaceRoot 'artifacts'
$portableZip = Join-Path $artifactsDir "FoturTypingHelper-$version-win-x64-portable.zip"
if (Test-Path -LiteralPath $portableZip) { Remove-Item -LiteralPath $portableZip -Force }
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $portableZip -CompressionLevel Optimal

if ($SkipInstaller) { return }
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
& $isccPath (Join-Path $workspaceRoot 'installer\FoturTypingHelper.iss')

$setup = Join-Path $workspaceRoot "artifacts\installer\FoturTypingHelper-Setup-$version-win-x64.exe"
$hashLines = @($setup, $portableZip) | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256
    "$($hash.Hash)  $([IO.Path]::GetFileName($_))"
}
[IO.File]::WriteAllLines((Join-Path $artifactsDir 'SHA256SUMS.txt'), $hashLines)
