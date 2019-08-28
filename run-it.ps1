#!/usr/bin/env pwsh

param(
    [string] $app,
    [string] $url,
    [switch] $trim,
    [switch] $trimr2r,
    [switch] $aggro,
    [switch] $r2r,
    [switch] $singleFile,
    [switch] $time)

dotnet clean

if (Test-Path "obj")
{
    Write-Debug "Deleting obj"
    Remove-Item "obj" -r -for
}

if ($IsWindows)
{
    $rid = "win10-x64"
}
elseif ($IsMacOS)
{
    $rid = "osx-x64"
}
elseif ($IsLinux)
{
    $rid = "linux-x64"
}
$path = Join-Path "bin" "Release" "netcoreapp3.0" "$rid" "publish"
if (Test-Path $path)
{
    Write-Debug "Deleting $path"
    Remove-Item $path -r -for
}

dotnet publish -c Release -r $rid /p:PublishTrimmed=$trim /p:LinkAggressively=$aggro /p:LinkAwayReadyToRun=$trimr2r /p:PublishReadyToRun=$r2r /p:PublishSingleFile=$singleFile /bl

Push-Location $path
Write-Host ("Size is {0:N2} MB" -f ((Get-ChildItem . -Recurse | Measure-Object -Property Length -Sum -ErrorAction Stop).Sum / 1MB))

$app_path = Join-Path (Get-Location) $app
if (-not (Test-Path $app_path))
{
    Write-Error "app not found at $app_path"
    exit
}
$app_args = @()
$console = $url -eq $null -or $url -eq ""

$iterations = 10

if ($time)
{
    $app_args += ("--time")

    $sum = 0
    for ($i = 0; $i -lt ($iterations + 1); $i++)
    {
        Write-Debug "Starting $app"
        $result = Measure-Command {
            $proc = Start-Process -FilePath $app_path -ArgumentList $app_args -PassThru -NoNewWindow -RedirectStandardOutput '.\NUL'
            $proc.WaitForExit()
        }

        # Ignore first result
        if ($i -gt 0) {
            $sum += $result.TotalMilliseconds
            Write-Host $result.TotalMilliseconds
        }
    }

    $avg = $sum / 10
    Write-Host "Average startup time (ms): $avg"

    if (-not $console) {
        # Measure working set after startup and one request
        Write-Debug "Starting $app"
        $proc = Start-Process -FilePath $app_path -PassThru -NoNewWindow -RedirectStandardOutput '.\NUL'

        Write-Debug "Making a request to $url"
        Invoke-WebRequest $url | Out-Null

        $proc.Refresh()
        $mb = [int]$proc.WorkingSet / 1MB
        Write-Host "Working set: $mb"

        Write-Debug "Stopping"
        $proc.Kill();
    }
}
else
{
    dotnet $app
}

Pop-Location