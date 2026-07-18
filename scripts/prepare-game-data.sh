#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
default_source="$root/ref/The Elder Scrolls Daggerfall/DF/DAGGER"
source_dir="${1:-$default_source}"
output_file="${2:-$root/Builds/TestData/daggerfall.zip}"
output_dir="$(dirname "$output_file")"

if [[ ! -d "$source_dir/ARENA2" || ! -f "$source_dir/FALL.EXE" ]]; then
  printf 'Expected a Daggerfall folder containing ARENA2 and FALL.EXE: %s\n' "$source_dir" >&2
  printf 'Usage: bash scripts/prepare-game-data.sh "/path/to/DAGGER" [output.zip]\n' >&2
  exit 1
fi
mkdir -p "$output_dir"
rm -f "$output_file"

ditto -c -k --norsrc --noextattr --keepParent "$source_dir" "$output_file"
printf '%s\n' "$output_file"
