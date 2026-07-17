#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
source_dir="$root/ref/The Elder Scrolls Daggerfall/DF/DAGGER"
output_dir="${1:-$root/Builds/TestData}"
output_file="$output_dir/daggerfall.zip"

[[ -d "$source_dir/ARENA2" ]] || { printf 'Missing source data: %s\n' "$source_dir" >&2; exit 1; }
mkdir -p "$output_dir"
rm -f "$output_file"

ditto -c -k --norsrc --noextattr --keepParent "$source_dir" "$output_file"
printf '%s\n' "$output_file"
