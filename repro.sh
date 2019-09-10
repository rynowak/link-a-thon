#!/usr/bin/env bash

scriptroot="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

source "$scriptroot/build-coreclr.sh"

# to initialize dotnet cli
# wget https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/3.0.1xx/dotnet-sdk-latest-linux-x64.tar.gz
# tar xf dotnet-sdk-latest-linux-x64.tar.gz

# to build and run locally:
# dotnet publish -c Release src/ApiTemplate/ApiTemplate.csproj -o published
# published/ApiTemplate --time

source "$scriptroot/send-to-perflab.sh"
