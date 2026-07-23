param(
    [string]$ExpectedVersion = ""
)

$ErrorActionPreference = "Stop"
$repository = "Woonze/Fotur_Typing_Helper"
$headers = @{
    Accept = "application/vnd.github+json"
    "User-Agent" = "Fotur-Typing-Helper-Release-Verification/1.1"
    "X-GitHub-Api-Version" = "2022-11-28"
}

$release = Invoke-RestMethod `
    -Uri "https://api.github.com/repos/$repository/releases/latest" `
    -Headers $headers

$version = [string]$release.tag_name
$version = $version.TrimStart("v")
if ($ExpectedVersion -and $version -ne $ExpectedVersion) {
    throw "Latest stable release is $version, expected $ExpectedVersion."
}

$requiredSuffixes = @(
    "win-x64.exe",
    "win-x64-portable.zip",
    "macos-arm64.dmg",
    "macos-arm64.zip",
    "macos-x64.dmg",
    "macos-x64.zip"
)
$assetNames = @($release.assets | ForEach-Object { [string]$_.name })
foreach ($suffix in $requiredSuffixes) {
    if (-not ($assetNames | Where-Object { $_.EndsWith($suffix, [StringComparison]::OrdinalIgnoreCase) })) {
        throw "Release v$version does not contain an asset ending in '$suffix'."
    }
}

$requiredChecksums = @(
    "SHA256SUMS.txt",
    "SHA256SUMS-macos-arm64.txt",
    "SHA256SUMS-macos-x64.txt"
)
foreach ($checksum in $requiredChecksums) {
    if ($assetNames -notcontains $checksum) {
        throw "Release v$version does not contain '$checksum'."
    }
}

Write-Host "GitHub latest: v$version"
Write-Host "Updater feed and all platform assets are present."
