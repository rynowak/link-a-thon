#!/usr/bin/env pwsh

param(
    [string] $appname,
    [string] $url,
    [switch] $trim,
    [switch] $trimr2r,
    [switch] $aggro,
    [switch] $r2r,
    [switch] $singleFile,
    [switch] $time,
    [string] $trace)

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

if ($trace)
{
    if (-not $IsLinux)
    {
        Write-Error "tracing is only supported on linux"
        exit
    }
    if (Test-Path "$trace.trace.zip")
    {
        Write-Error "$trace.trace.zip already exists"
        exit
    }
}

$publish_dir = Join-Path "bin" "Release" "netcoreapp3.0" "$rid" "publish"
if (Test-Path $publish_dir)
{
    Write-Debug "Deleting $publish_dir"
    Remove-Item $publish_dir -r -for
}

& dotnet publish -c Release -r $rid /p:PublishTrimmed=$trim /p:LinkAggressively=$aggro /p:LinkAwayReadyToRun=$trimr2r /p:PublishReadyToRun=$r2r /p:PublishSingleFile=$singleFile /bl

Write-Host ("Size is {0:N2} MB" -f ((Get-ChildItem . -Recurse | Measure-Object -Property Length -Sum -ErrorAction Stop).Sum / 1MB))

$app_path = Join-Path "$publish_dir" "$appname"
if ($IsWindows)
{
    $app_path = "$app_path" + ".exe"
}
if (-not (Test-Path "$app_path"))
{
    Write-Error "app not found at $app_path"
    exit
}
$app_args = @()
$console = $url -eq $null -or $url -eq ""

$iterations = 10

if ($trace)
{
    # 1. get perfcollect

    $perfcollect = Join-Path "$PSScriptRoot" "perfcollect"
    if (-not (Test-Path "$perfcollect"))
    {
        & wget "http://aka.ms/perfcollect" -O "$perfcollect"
        & chmod +x "$perfcollect"
    }

    # 2. copy crossgen to the publish directory (used to resolve R2R symbols)

    $deps_json = Join-Path "$publish_dir" "$appname.deps.json"
    if (-not (Test-Path $deps_json))
    {
        Write-Error "deps.json not found at $deps_json"
        exit
    }
    $runtimeversion = Select-String -Path "$deps_json" -Pattern "runtimepack.Microsoft.NETCore.App.Runtime.$rid" `
      | Select-Object -First 1 | %{ $_ -split ':' } | Select-Object -Last 1 | %{ $_ -split '"' } | Select-Object -Index 1
    if (-not ($runtimeversion -and ($runtimeversion.StartsWith("3.0.0"))))
    {
        Write-Error "unable to get runtime version"
        exit
    }
    $crossgen = Join-Path "$env:HOME" ".nuget" "packages" "microsoft.netcore.app.runtime.linux-x64" "$runtimeversion" "tools" "crossgen"
    if (-not (Test-Path "$crossgen"))
    {
        Write-Error "crossgen does not exist at `"$crossgen`""
        exit
    }
    Copy-Item "$crossgen" "$publish_dir"

    # 3. install native symbols for the runtime libraries

    & dotnet tool install -g dotnet-symbol
    $env:DOTNET_ROOT = (Split-Path -Parent (Get-Command dotnet).Source)
    $dotnet_symbol = Join-Path "$env:HOME" ".dotnet" "tools" "dotnet-symbol"
    if (-not (Test-Path "$dotnet_symbol"))
    {
        Write-Error "dotnet-symbol not found at $dotnet_symbol"
        exit
    }
    & "$dotnet_symbol" --symbols "$publish_dir/lib*.so"

    # 4. run perfcollect and hold onto the perfcollect PID to wait on it later

    $pid_file = & mktemp -u
    & mkfifo "$pid_file"
    Start-Process sudo -ArgumentList @("sh", "-c", "`"echo `$`$ > `"$pid_file`"; exec $perfcollect collect $trace`"")
    $perfcollect_pid = & cat "$pid_file"
    Write-Host "perfcollect PID: $perfcollect_pid"

    # 5. set COMPlus variables for tracing

    $env:COMPlus_PerfMapEnabled = 1
    $env:COMPlus_EnableEventLog = 1
}

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

    $avg = $sum / $iterations
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
