#!/usr/bin/env bash
set -euo pipefail

rid="${1:-linux-x64}"
case "$rid" in
  linux-x64) ;;
  *) echo "Usage: $0 linux-x64" >&2; exit 2 ;;
esac

root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$root/Directory.Build.props")"
publish="$root/artifacts/publish-$rid"
package_root="$root/artifacts/FoturTypingHelper-$version-$rid"
archive="$root/artifacts/FoturTypingHelper-$version-$rid.tar.gz"
symbols_dir="$root/artifacts/symbols-$rid"
symbols_archive="$root/artifacts/FoturTypingHelper-$version-$rid-symbols.tar.gz"

rm -rf "$publish" "$package_root" "$archive" "$symbols_dir" "$symbols_archive"
mkdir -p "$publish" "$package_root"

remove_disallowed_runtimes() {
  local runtime_root="$1"
  shift
  [[ -d "$runtime_root" ]] || return 0
  local allowed=" $* "
  local child
  for child in "$runtime_root"/*; do
    [[ -d "$child" ]] || continue
    local name
    name="$(basename "$child")"
    if [[ "$allowed" != *" $name "* ]]; then
      rm -rf "$child"
    fi
  done
}

move_symbols() {
  local source="$1"
  local target="$2"
  mkdir -p "$target"
  while IFS= read -r -d '' pdb; do
    local rel="${pdb#$source/}"
    mkdir -p "$target/$(dirname "$rel")"
    mv "$pdb" "$target/$rel"
  done < <(find "$source" -name '*.pdb' -print0)
}

dotnet test "$root/tests/FoturTypingHelper.Tests/FoturTypingHelper.Tests.csproj" -c Release
dotnet publish "$root/src/FoturTypingHelper.App/FoturTypingHelper.App.csproj" \
  -c Release -r "$rid" --self-contained true -p:PublishSingleFile=false -o "$publish"

remove_disallowed_runtimes "$publish/runtimes" "$rid"
move_symbols "$publish" "$symbols_dir"

if ! find "$publish" -name 'libwhisper.so' -print -quit | grep -q .; then
  echo "Whisper native library is missing from the Linux publish output" >&2
  exit 1
fi
if find "$publish/runtimes" -mindepth 1 -maxdepth 1 -type d ! -name "$rid" -print -quit | grep -q .; then
  echo "Unexpected non-Linux runtime remained in Linux package" >&2
  exit 1
fi
if find "$publish" -name '*.pdb' -print -quit | grep -q .; then
  echo "PDB files are still present in Linux package" >&2
  exit 1
fi

cp -a "$publish/." "$package_root/"
cp "$root/LICENSE" "$package_root/LICENSE"
cp "$root/THIRD_PARTY_NOTICES.md" "$package_root/THIRD_PARTY_NOTICES.md"
cat > "$package_root/README-LINUX.txt" <<EOF
Fotur Typing Helper $version for Linux x64

Run:
  ./FoturTypingHelper.App

Linux notes:
- UI and local Whisper dictation are packaged.
- Audio recording uses arecord, install alsa-utils if recording fails.
- Global hotkeys, text insertion and automatic layout correction use xinput/XKB and xdotool on X11/XWayland.
- Install xinput and xdotool for these global features.
- Wayland may block global keyboard access and synthetic typing by compositor policy.
EOF
chmod +x "$package_root/FoturTypingHelper.App"

(
  cd "$root/artifacts"
  tar -czf "$(basename "$archive")" "$(basename "$package_root")"
  if [[ -d "$symbols_dir" ]]; then
    tar -czf "$(basename "$symbols_archive")" "$(basename "$symbols_dir")"
  fi
  sha256sum "$(basename "$archive")" > SHA256SUMS-linux-x64.txt
)

echo "Created $archive"
