#!/usr/bin/env bash
set -euo pipefail

project_dir="${1:-Builds/iOS/Simulator}"
plist="$project_dir/Info.plist"
pbxproj="$project_dir/Unity-iPhone.xcodeproj/project.pbxproj"
addressables_settings="$project_dir/Data/Raw/aa/settings.json"

pass() { printf 'PASS  %s\n' "$1"; }
fail() { printf 'FAIL  %s\n' "$1" >&2; exit 1; }

[[ -f "$plist" ]] || fail "missing generated Info.plist at $plist"
[[ -f "$pbxproj" ]] || fail "missing generated Xcode project at $pbxproj"
[[ -f "$addressables_settings" ]] \
  && pass "Addressables runtime settings exported" \
  || fail "missing Addressables runtime settings at $addressables_settings"

bundle_identifier="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$plist")"
if [[ "$bundle_identifier" == '${PRODUCT_BUNDLE_IDENTIFIER}' || "$bundle_identifier" == 'com.chrissotraidis.daggerpad' ]]; then
  pass "Info.plist has the expected bundle identifier"
else
  fail "Info.plist has an unexpected bundle identifier: $bundle_identifier"
fi
[[ "$(/usr/libexec/PlistBuddy -c 'Print :UIFileSharingEnabled' "$plist")" == 'true' ]] \
  && pass "Files sharing enabled" \
  || fail "UIFileSharingEnabled is not true"
[[ "$(/usr/libexec/PlistBuddy -c 'Print :LSSupportsOpeningDocumentsInPlace' "$plist")" == 'true' ]] \
  && pass "Files documents-in-place enabled" \
  || fail "LSSupportsOpeningDocumentsInPlace is not true"
[[ "$(/usr/libexec/PlistBuddy -c 'Print :UIApplicationSupportsIndirectInputEvents' "$plist")" == 'true' ]] \
  && pass "indirect mouse and trackpad input enabled" \
  || fail "UIApplicationSupportsIndirectInputEvents is not true"
[[ "$(/usr/libexec/PlistBuddy -c 'Print :ITSAppUsesNonExemptEncryption' "$plist")" == 'false' ]] \
  && pass "export-compliance flag configured" \
  || fail "ITSAppUsesNonExemptEncryption is not false"

grep -Eq 'IPHONEOS_DEPLOYMENT_TARGET = "?15\.0"?;' "$pbxproj" \
  && pass "Xcode deployment target is iOS 15" \
  || fail "iOS 15 deployment target missing from Xcode project"
grep -Eq 'TARGETED_DEVICE_FAMILY = "?2"?;' "$pbxproj" \
  && pass "Xcode output targets iPad" \
  || fail "iPad target family missing from Xcode project"

xcodebuild -project "$project_dir/Unity-iPhone.xcodeproj" -scheme Unity-iPhone -list >/dev/null
pass "Xcode recognizes the Unity-iPhone scheme"
