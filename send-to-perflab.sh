#!/usr/bin/env bash

scriptroot="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
driverproject="$scriptroot/benchmarks/src/BenchmarksDriver/BenchmarksDriver.csproj"

client=
server=

if [[ -z "$client" ]] || [[ -z "$server" ]]; then
    echo "specify client and server in $BASH_SOURCE"
    exit 1
fi

crossgen2Arguments=
if [ "$1" == "crossgen2" ]; then
    echo "Using crossgen2"
    crossgen2Arguments="--build-arg /p:UseCrossgen2=true --build-arg /p:UseTibcData=true"
fi

# clone benchmarks repo
if [[ ! -e "$driverproject" ]]; then
    git clone https://github.com/aspnet/benchmarks
fi

dotnet run -p "$driverproject" -- \
       --source src \
       --client "$client"  \
       --server "$server" \
       --project-file ApiTemplate/ApiTemplate.csproj \
       --self-contained \
       --sdk 3.0.100-rc1-014176 \
       --path weatherforecast \
       --aspnetcoreversion 3.0 \
       --runtimeversion 3.0 \
       --warmup 1 \
       --duration 2 \
       --build-arg "/p:SelfContained=true" \
       --build-arg "/p:PublishTrimmed=true" \
       --build-arg "/p:LinkAggressively=true" \
       --build-arg "/p:UsePublishFilterList=true" \
       --build-arg "/p:UseStaticHost=true" \
       --build-arg "/p:PublishReadyToRun=true" \
       --display-output \
       --iterations 1 \
       --collect-counters \
       --env "COMPlus_gcServer=1" \
       $crossgen2Arguments

#       --fetch \
#       --collect-startup \
#       --collect-trace \
#       --verbose (to show all client/server communication)
