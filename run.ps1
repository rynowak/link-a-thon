#!/usr/bin/env pwsh

# This is the script that we are using to collect size/startup numbers
# for the small/fast/single exe prototype.

$app = "ApiTemplate"
$url = "http://localhost:5000/WeatherForecast"


& (Join-Path "$PSScriptRoot" "run-it.ps1") -appname "$app" -url "$url" -r2r -aggro -trim -singleFile -time -trace -selfContained
