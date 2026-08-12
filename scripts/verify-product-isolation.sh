#!/bin/sh
set -eu

root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$root"

require_file() {
    if [ ! -f "$1" ]; then
        echo "missing required file: $1" >&2
        exit 1
    fi
}

require_literal() {
    file=$1
    value=$2
    if ! grep -Fq "$value" "$file"; then
        echo "missing '$value' in $file" >&2
        exit 1
    fi
}

reject_literal() {
    file=$1
    value=$2
    if grep -Fq "$value" "$file"; then
        echo "forbidden '$value' in $file" >&2
        exit 1
    fi
}

reject_product_literal() {
    value=$1
    if grep -RFn \
        --exclude=NetherBattleStartTaskCapturePatch.cs \
        --exclude='*.dll' \
        --exclude='*.pdb' \
        --exclude-dir=bin \
        --exclude-dir=obj \
        "$value" AutoNether >/tmp/autonether-isolation-grep.txt; then
        echo "forbidden source ownership '$value':" >&2
        cat /tmp/autonether-isolation-grep.txt >&2
        exit 1
    fi
}

reject_path() {
    if [ -e "$1" ]; then
        echo "forbidden path: $1" >&2
        exit 1
    fi
}

require_file AutoNether.sln
require_file AutoNether/AutoNether.csproj
require_file AutoNether.Tests/AutoNether.Tests.csproj

require_literal AutoNether/AutoNether.csproj '<AssemblyName>AutoNether</AssemblyName>'
require_literal AutoNether/AutoNether.csproj '<Product>AutoNether</Product>'
require_literal AutoNether/AutoNether.csproj '<RootNamespace>AutoNether</RootNamespace>'
reject_literal AutoNether/AutoNether.csproj 'AbyssMod.dll'
reject_literal AutoNether/AutoNether.csproj 'Reference Include="AbyssMod"'

reject_path AutoNether/Services/MachineTranslator.cs
reject_path AutoNether/Services/TranslationManager.cs
reject_path AutoNether/Patches/TranslationPatch.cs
reject_path AutoNether/Patches/BattleSessionAutoSLPatch.cs
reject_path AutoNether/Services/BattleSessionAutoSL.cs

reject_product_literal 'MachineTranslator'
reject_product_literal 'TranslationManager'
reject_product_literal 'BattleSessionAutoSL'
reject_product_literal 'KeyCode.F6'
reject_product_literal 'KeyCode.F8'
reject_product_literal 'KeyCode.F9'
reject_product_literal 'KeyCode.F11'
reject_product_literal 'AbyssMod.Services'

require_literal AutoNether/Patches/NetherBattleStartTaskCapturePatch.cs '[HarmonyPriority(Priority.Last)]'
require_literal AutoNether/Patches/NetherBattleStartTaskCapturePatch.cs 'NetherRuntimeBridge.ObserveBattleStartTask(__result);'

echo 'product isolation: PASS'
