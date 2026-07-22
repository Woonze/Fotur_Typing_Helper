#!/usr/bin/env bash
set -euo pipefail

rid="${1:-}"
case "$rid" in
  osx-arm64|osx-x64) ;;
  *) echo "Usage: $0 osx-arm64|osx-x64" >&2; exit 2 ;;
esac

root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$root/Directory.Build.props")"
arch="${rid#osx-}"
publish="$root/artifacts/publish-$rid"
stage="$root/artifacts/stage-$rid"
app="$stage/Fotur Typing Helper.app"
contents="$app/Contents"
artifacts="$root/artifacts"

rm -rf "$publish" "$stage"
mkdir -p "$publish" "$contents/MacOS" "$contents/Resources"

dotnet test "$root/tests/FoturTypingHelper.Tests/FoturTypingHelper.Tests.csproj" -c Release
dotnet publish "$root/src/FoturTypingHelper.App/FoturTypingHelper.App.csproj" \
  -c Release -r "$rid" --self-contained true -p:PublishSingleFile=false -o "$publish"

cp -R "$publish"/. "$contents/MacOS/"
chmod +x "$contents/MacOS/FoturTypingHelper.App"
native_library="$(find "$contents/MacOS" -name libwhisper.dylib -print -quit)"
if [[ -z "$native_library" ]]; then
  echo "Whisper native library is missing from the macOS publish output" >&2
  exit 1
fi

cat > "$contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleDevelopmentRegion</key><string>ru</string>
  <key>CFBundleDisplayName</key><string>Fotur Typing Helper</string>
  <key>CFBundleExecutable</key><string>FoturTypingHelper.App</string>
  <key>CFBundleIdentifier</key><string>tech.fotur.typinghelper</string>
  <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
  <key>CFBundleName</key><string>Fotur Typing Helper</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$version</string>
  <key>CFBundleVersion</key><string>$version</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>NSMicrophoneUsageDescription</key><string>Fotur использует микрофон только для локального преобразования речи в текст.</string>
  <key>NSAccessibilityUsageDescription</key><string>Fotur исправляет раскладку и вставляет продиктованный текст в активное поле.</string>
</dict></plist>
PLIST

plutil -lint "$contents/Info.plist"
codesign --force --deep --sign - "$app"
codesign --verify --deep --strict "$app"

zip_path="$artifacts/FoturTypingHelper-$version-macos-$arch.zip"
dmg_path="$artifacts/FoturTypingHelper-$version-macos-$arch.dmg"
rm -f "$zip_path" "$dmg_path"
ditto -c -k --sequesterRsrc --keepParent "$app" "$zip_path"
ln -s /Applications "$stage/Applications"
hdiutil create -volname "Fotur Typing Helper" -srcfolder "$stage" -ov -format UDZO "$dmg_path"

shasum -a 256 "$zip_path" "$dmg_path" > "$artifacts/SHA256SUMS-macos-$arch.txt"
file "$contents/MacOS/FoturTypingHelper.App" "$native_library"
echo "Created $dmg_path and $zip_path"
