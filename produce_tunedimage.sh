#!/usr/bin/env bash

scriptroot="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"


rm -r -f $scriptroot/src/ApiTemplate/tibcdata
mkdir -p $scriptroot/src/ApiTemplate/tibcdata
cp $scriptroot/tibcdata/* $scriptroot/src/ApiTemplate/tibcdata

dotnet publish -c Release src/ApiTemplate/ApiTemplate.csproj -o publish -p:SelfContained=true -p:UseStaticHost=true -p:UsePublishFilterList=true -p:PublishTrimmed=true -p:PublishReadyToRun=true -p:UseTibcData=true -p:UseCrossgen2=true $1
