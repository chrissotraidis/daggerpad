#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
default_app="$root/Builds/iOS/DeviceDerivedData/Build/Products/Release-iphoneos/DaggerPad.app"
default_output="$root/Builds/iOS/Artifacts/DaggerPad-0.1.0-preview.1-unsigned.ipa"
app="${1:-$default_app}"
output="${2:-$default_output}"

pass() { printf 'PASS  %s\n' "$1"; }
fail() { printf 'FAIL  %s\n' "$1" >&2; exit 1; }

(( $# <= 2 )) || fail 'Usage: bash scripts/package-ios-ipa.sh [DaggerPad.app] [output.ipa]'
[[ -d "$app" ]] || fail "missing app bundle: $app"
[[ -f "$app/Info.plist" ]] || fail "missing app Info.plist: $app/Info.plist"
[[ -f "$root/LICENSE" ]] || fail 'missing project LICENSE'
[[ -f "$root/THIRD_PARTY_NOTICES.md" ]] || fail 'missing THIRD_PARTY_NOTICES.md'

bundle_id="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$app/Info.plist")"
version="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$app/Info.plist")"
build="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleVersion' "$app/Info.plist")"
minimum_os="$(/usr/libexec/PlistBuddy -c 'Print :MinimumOSVersion' "$app/Info.plist")"
[[ "$bundle_id" == 'com.chrissotraidis.daggerpad' ]] || fail "unexpected bundle identifier: $bundle_id"
[[ "$version" == '0.1.0' ]] || fail "unexpected version: $version"
[[ "$minimum_os" == '15.0' ]] || fail "unexpected minimum iOS version: $minimum_os"

tmp="$(mktemp -d "${TMPDIR:-/tmp}/daggerpad-ipa.XXXXXX")"
trap 'rm -rf "$tmp"' EXIT
mkdir -p "$tmp/Payload" "$(dirname "$output")"
ditto "$app" "$tmp/Payload/DaggerPad.app"
packaged_app="$tmp/Payload/DaggerPad.app"

mkdir -p "$packaged_app/Licenses"
cp -p "$root/LICENSE" "$packaged_app/Licenses/DaggerPad-LICENSE.txt"
cp -p "$root/THIRD_PARTY_NOTICES.md" "$packaged_app/Licenses/THIRD_PARTY_NOTICES.md"
cp -p "$root/Assets/Game/Addons/UnityConsole/Console/LICENSE.txt" "$packaged_app/Licenses/UnityConsole-LICENSE.txt"
cp -p "$root/Assets/Resources/Fonts/OpenSans/License!.txt" "$packaged_app/Licenses/OpenSans-LICENSE.txt"
cp -p "$root/Assets/Resources/Fonts/TESFonts/Readme_TES_Fonts.txt" "$packaged_app/Licenses/TES-Font-Pack-NOTICE.txt"
cp -p "$root/Assets/Android/Textures/Resources/ATTRIBUTIONS.md" "$packaged_app/Licenses/Touch-Icons-ATTRIBUTIONS.md"
cp -p "$root/Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt" "$packaged_app/Licenses/EmojiOne-ATTRIBUTION.txt"
cp -p "$root/Packages/com.unity.postprocessing/LICENSE.md" "$packaged_app/Licenses/Unity-Post-Processing-LICENSE.md"

while IFS= read -r -d '' signed_item; do
  codesign --remove-signature "$signed_item" 2>/dev/null || true
done < <(find "$packaged_app" -depth \( -type d -o -type f \) \( -name '*.app' -o -name '*.appex' -o -name '*.framework' -o -name '*.dylib' \) -print0)
find "$packaged_app" -type d -name '_CodeSignature' -prune -exec rm -rf {} +
find "$packaged_app" -type f \( -name 'CodeResources' -o -name 'embedded.mobileprovision' \) -delete
find "$tmp/Payload" -exec touch -h -t 202608020000 {} +

rm -f "$output"
(
  cd "$tmp"
  COPYFILE_DISABLE=1 /usr/bin/zip -qry -X "$output" Payload
)

unzip -tq "$output" >/dev/null || fail 'IPA ZIP integrity check failed'
unzip -Z1 "$output" | grep -qx 'Payload/DaggerPad.app/Info.plist' \
  || fail 'IPA does not contain Payload/DaggerPad.app'
if unzip -Z1 "$output" | grep -Eq '(^|/)(_CodeSignature|embedded\.mobileprovision)(/|$)|CodeResources$'; then
  fail 'IPA still contains signing material'
fi
if unzip -Z1 "$output" | grep -Eiq '(^|/)(ARENA2|FALL\.EXE|SAVE[0-9]+)(/|$)'; then
  fail 'IPA contains user-provided game data or saves'
fi
if rg -a -q 'DaggerPadInputTrace' "$packaged_app"; then
  fail 'IPA contains temporary input trace instrumentation'
fi
codesign --verify --strict "$packaged_app" >/dev/null 2>&1 \
  && fail 'IPA payload is still signed' \
  || true
[[ "$(lipo -archs "$packaged_app/DaggerPad")" == *arm64* ]] \
  || fail 'app executable is not arm64'
cmp -s "$root/LICENSE" "$packaged_app/Licenses/DaggerPad-LICENSE.txt" \
  || fail 'packaged project license does not match source'
cmp -s "$root/THIRD_PARTY_NOTICES.md" "$packaged_app/Licenses/THIRD_PARTY_NOTICES.md" \
  || fail 'packaged third-party notices do not match source'

pass "packaged unsigned IPA: $output"
pass "bundle $bundle_id, version $version ($build), minimum iOS $minimum_os, arm64"
pass 'ZIP integrity, unsigned payload, notices, trace exclusion, and game-data exclusion verified'
shasum -a 256 "$output"
