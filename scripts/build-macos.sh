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
icon_png="$root/src/FoturTypingHelper.App/Assets/FoturTypingHelper.png"
iconset="$artifacts/FoturTypingHelper-$rid.iconset"
background="$root/assets/branding/dmg-background.png"
volume_name="Fotur Typing Helper"
rw_dmg="$artifacts/FoturTypingHelper-$version-macos-$arch-rw.dmg"
mount_point="$artifacts/mount-$rid"
mounted_device=""

cleanup() {
  if [[ -n "$mounted_device" ]]; then
    hdiutil detach "$mounted_device" -force >/dev/null 2>&1 || true
  fi
  rm -rf "$iconset" "$mount_point"
}
trap cleanup EXIT

rm -rf "$publish" "$stage" "$iconset" "$mount_point"
rm -f "$rw_dmg"
mkdir -p "$publish" "$contents/MacOS" "$contents/Resources"

dotnet test "$root/tests/FoturTypingHelper.Tests/FoturTypingHelper.Tests.csproj" -c Release
dotnet publish "$root/src/FoturTypingHelper.App/FoturTypingHelper.App.csproj" \
  -c Release -r "$rid" --self-contained true -p:PublishSingleFile=false -o "$publish"

ditto "$publish" "$contents/MacOS"
chmod +x "$contents/MacOS/FoturTypingHelper.App"
native_library="$(find "$contents/MacOS" -name libwhisper.dylib -print -quit)"
if [[ -z "$native_library" ]]; then
  echo "Whisper native library is missing from the macOS publish output" >&2
  exit 1
fi

mkdir -p "$iconset"
make_icon() {
  local pixels="$1"
  local filename="$2"
  sips -z "$pixels" "$pixels" "$icon_png" --out "$iconset/$filename" >/dev/null
}
make_icon 16 icon_16x16.png
make_icon 32 icon_16x16@2x.png
make_icon 32 icon_32x32.png
make_icon 64 icon_32x32@2x.png
make_icon 128 icon_128x128.png
make_icon 256 icon_128x128@2x.png
make_icon 256 icon_256x256.png
make_icon 512 icon_256x256@2x.png
make_icon 512 icon_512x512.png
make_icon 1024 icon_512x512@2x.png
iconutil -c icns "$iconset" -o "$contents/Resources/FoturTypingHelper.icns"

cat > "$contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleDevelopmentRegion</key><string>ru</string>
  <key>CFBundleDisplayName</key><string>Fotur Typing Helper</string>
  <key>CFBundleExecutable</key><string>FoturTypingHelper.App</string>
  <key>CFBundleIdentifier</key><string>tech.fotur.typinghelper</string>
  <key>CFBundleIconFile</key><string>FoturTypingHelper.icns</string>
  <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
  <key>CFBundleName</key><string>Fotur Typing Helper</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$version</string>
  <key>CFBundleVersion</key><string>$version</string>
  <key>CFBundleGetInfoString</key><string>Fotur Typing Helper $version</string>
  <key>LSApplicationCategoryType</key><string>public.app-category.productivity</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>NSMicrophoneUsageDescription</key><string>Fotur использует микрофон только для локального преобразования речи в текст.</string>
  <key>NSAccessibilityUsageDescription</key><string>Fotur исправляет раскладку и вставляет продиктованный текст в активное поле.</string>
</dict></plist>
PLIST

plutil -lint "$contents/Info.plist"
[[ "$(plutil -extract CFBundleDisplayName raw "$contents/Info.plist")" == "Fotur Typing Helper" ]]
[[ "$(plutil -extract CFBundleIdentifier raw "$contents/Info.plist")" == "tech.fotur.typinghelper" ]]
[[ "$(plutil -extract CFBundleShortVersionString raw "$contents/Info.plist")" == "$version" ]]
test -s "$contents/Resources/FoturTypingHelper.icns"
codesign --force --deep --sign - "$app"
codesign --verify --deep --strict "$app"

zip_path="$artifacts/FoturTypingHelper-$version-macos-$arch.zip"
dmg_path="$artifacts/FoturTypingHelper-$version-macos-$arch.dmg"
rm -f "$zip_path" "$dmg_path"
ditto -c -k --sequesterRsrc --keepParent "$app" "$zip_path"

mkdir -p "$stage/.background"
ditto "$background" "$stage/.background/dmg-background.png"
ln -s /Applications "$stage/Applications"
hdiutil create -volname "$volume_name" -srcfolder "$stage" -ov -format UDRW "$rw_dmg"
mkdir -p "$mount_point"
mounted_device="$(hdiutil attach "$rw_dmg" -readwrite -noverify -noautoopen -mountpoint "$mount_point" |
  awk '/Apple_HFS|Apple_APFS/ { print $1; exit }')"
if [[ -z "$mounted_device" ]]; then
  echo "Could not mount writable DMG" >&2
  exit 1
fi

if command -v SetFile >/dev/null 2>&1; then
  SetFile -a V "$mount_point/.background"
fi

osascript <<APPLESCRIPT
tell application "Finder"
  set dmgFolder to POSIX file "$mount_point" as alias
  open dmgFolder
  delay 2
  set current view of container window of dmgFolder to icon view
  set toolbar visible of container window of dmgFolder to false
  set statusbar visible of container window of dmgFolder to false
  set pathbar visible of container window of dmgFolder to false
  set bounds of container window of dmgFolder to {120, 120, 780, 540}
  set theViewOptions to the icon view options of container window of dmgFolder
  set arrangement of theViewOptions to not arranged
  set icon size of theViewOptions to 104
  set text size of theViewOptions to 13
  set background picture of theViewOptions to file ".background:dmg-background.png" of dmgFolder
  set position of item "Fotur Typing Helper.app" of dmgFolder to {170, 245}
  set position of item "Applications" of dmgFolder to {490, 245}
  update dmgFolder without registering applications
  delay 3
  close container window of dmgFolder
end tell
APPLESCRIPT

sync
test -f "$mount_point/.DS_Store"
test -s "$mount_point/.background/dmg-background.png"
test -L "$mount_point/Applications"
hdiutil detach "$mounted_device"
mounted_device=""
hdiutil convert "$rw_dmg" -format UDZO -imagekey zlib-level=9 -o "$dmg_path"
rm -f "$rw_dmg"
codesign --force --sign - "$dmg_path"
codesign --verify "$dmg_path"
hdiutil imageinfo "$dmg_path" >/dev/null

(
  cd "$artifacts"
  shasum -a 256 "$(basename "$zip_path")" "$(basename "$dmg_path")" > "SHA256SUMS-macos-$arch.txt"
)
file "$contents/MacOS/FoturTypingHelper.App" "$native_library"
echo "Created $dmg_path and $zip_path"
