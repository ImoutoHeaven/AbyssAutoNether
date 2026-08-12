#!/bin/sh
set -eu

root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
dll=${1:-"$root/release/Release/net6.0/AutoNether.dll"}

test -f "$dll"
test "$(basename "$dll")" = "AutoNether.dll"

strings -el "$dll" > /tmp/autonether-release-strings.txt
grep -Fq 'Abyss.AutoNether' /tmp/autonether-release-strings.txt
grep -Fq 'Abyss AutoNether' /tmp/autonether-release-strings.txt
grep -Fq '[F12][AutoNether]' /tmp/autonether-release-strings.txt

if grep -Fq 'MachineTranslator' /tmp/autonether-release-strings.txt; then
    echo 'forbidden MachineTranslator symbol in release' >&2
    exit 1
fi
if grep -Fq 'TranslationManager' /tmp/autonether-release-strings.txt; then
    echo 'forbidden TranslationManager symbol in release' >&2
    exit 1
fi
if grep -Fq 'BattleSessionAutoSL.HasActiveNetherOperation' /tmp/autonether-release-strings.txt; then
    echo 'forbidden direct F11 runtime dependency in release' >&2
    exit 1
fi

echo 'release audit: PASS'
