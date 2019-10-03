#!/usr/bin/env bash

scriptroot="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

coreclrbuilddir="$scriptroot/coreclr/bin/Product/Linux.x64.Release"
coreclrbinariesdir="$scriptroot/src/coreclrbin"

# build coreclr
if [[ ! -e "$coreclrbuilddir" ]]; then
    # apt install cmake clang llvm libicu-dev liblttng-ust-dev libkrb5-dev
    pushd coreclr
    export LANG=en_US.UTF-8
    ./build.sh release -stripsymbols
    popd
fi

# place necessary coreclr binaries into a separate directory inside src
# this makes it easy to submit the whole src folder to the perf lab
# local builds of the project use the same layout for consistency
mkdir -p "$coreclrbinariesdir"
cp "$coreclrbuilddir/bundle.dll" "$coreclrbinariesdir"
cp "$coreclrbuilddir/bundle.runtimeconfig.json" "$coreclrbinariesdir"
cp "$coreclrbuilddir/Microsoft.NET.HostModel.dll" "$coreclrbinariesdir"
cp "$coreclrbuilddir/corebundle" "$coreclrbinariesdir"
cp "$coreclrbuilddir/System.Private.CoreLib.dll" "$coreclrbinariesdir"
cp "$coreclrbuilddir/crossgen" "$coreclrbinariesdir"
cp -r "$coreclrbuilddir/crossgen2" "$coreclrbinariesdir/crossgen2"
cp -r "$coreclrbuilddir/tibcmgr" "$coreclrbinariesdir/tibcmgr"
