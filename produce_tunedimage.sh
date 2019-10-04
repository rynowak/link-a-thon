#!/usr/bin/env bash

scriptroot="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"


rm -r -f $scriptroot/src/coreclrbin/tibcdata
mkdir -p $scriptroot/src/coreclrbin/tibcdata
cp $scriptroot/tibcdata/* $scriptroot/src/coreclrbin/tibcdata

dotnet publish -c Release src/ApiTemplate/ApiTemplate.csproj -o publish -p:LinkAggressively=true -p:SelfContained=true -p:UseStaticHost=true -p:UsePublishFilterList=true -p:PublishTrimmed=true -p:PublishReadyToRun=true -p:UseTibcData=true -p:UseCrossgen2=true $1
