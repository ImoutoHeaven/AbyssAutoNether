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

require_file AutoNether.sln
require_file AutoNether/AutoNether.csproj
require_file AutoNether.Tests/AutoNether.Tests.csproj

require_literal AutoNether/AutoNether.csproj '<AssemblyName>AutoNether</AssemblyName>'
require_literal AutoNether/AutoNether.csproj '<Product>AutoNether</Product>'
require_literal AutoNether/AutoNether.csproj '<RootNamespace>AutoNether</RootNamespace>'
reject_literal AutoNether/AutoNether.csproj 'AbyssMod.dll'
reject_literal AutoNether/AutoNether.csproj 'Reference Include="AbyssMod"'

echo 'product isolation: PASS'
