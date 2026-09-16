#!/bin/sh
set -eu
cd "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
if [ -x .tools/dotnet/dotnet ]; then
    DOTNET_ROOT="$PWD/.tools/dotnet"
    export DOTNET_ROOT
    PATH="$DOTNET_ROOT:$PATH"
    export PATH
fi
if [ -x .tools/godot/Godot_v4.5.1-stable_mono_linux.x86_64 ]; then
    VALE_GODOT="$PWD/.tools/godot/Godot_v4.5.1-stable_mono_linux.x86_64"
else
    VALE_GODOT="${GODOT_BIN:-godot}"
fi
if ! command -v dotnet >/dev/null 2>&1; then
    echo 'Instale o SDK .NET 8 ou superior. Veja README.md.' >&2
    exit 1
fi
if ! command -v "$VALE_GODOT" >/dev/null 2>&1; then
    echo 'Instale Godot 4.5.1 .NET e defina GODOT_BIN com o caminho do executável. Veja README.md.' >&2
    exit 1
fi
DOTNET_CLI_HOME="$PWD/.tools/dotnet-home"
NUGET_PACKAGES="$PWD/.tools/nuget"
export DOTNET_CLI_HOME NUGET_PACKAGES
dotnet build --nologo
exec "$VALE_GODOT" --path "$PWD" "$@"
