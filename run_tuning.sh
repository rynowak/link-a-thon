#!/usr/bin/env bash

scriptroot="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

dotnet publish -c Release src/ApiTemplate/ApiTemplate.csproj -o publish -p:UseStaticHost=true -p:UsePublishFilterList=true -p:LinkAggressively=true -p:PublishTrimmed=true -p:SelfContained=true -p:ProduceTuningImage=true -p:UseCrossgen2=true -r linux-x64 -p:PublishReadyToRun=true

rm -r -f $scriptroot/rawibcdata
mkdir -p $scriptroot/rawibcdata

export COMPlus_ZapBBInstr=*
export COMPlus_ZapBBInstrDir=$scriptroot/rawibcdata
export COMPlus_ZapBBInstrR2RGenerics=2

$scriptroot/publish/ApiTemplate & PROC_ID=$!
echo $PROC_ID

sleep 1
curl http://localhost:5000/WeatherForecast > /dev/null
while [ $? -ne 0 ] 
do
  sleep 1
  curl http://localhost:5000/WeatherForecast > /dev/null
done

echo Service Live!

counter=1
while [ $counter -le 10 ]
do
  echo $counter
  curl http://localhost:5000/WeatherForecast > /dev/null
  counter=$(($counter+1))
  sleep 1
done

echo Telling the service to quit
kill -s SIGTERM $PROC_ID

sleep 1
while kill -0 "$PROC_ID" >/dev/null 2>&1; do
    echo "PROCESS IS STILL RUNNING"
    sleep 1
done

echo "PROCESS TERMINATED"

$scriptroot/produce_tibc.sh
$scriptroot/produce_tunedimage.sh

