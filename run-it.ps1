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

if (Test-Path "obj\")
{
    Write-Debug "Deleting obj"
    Remove-Item "obj\" -r -for
}    

$path = "bin\Release\netcoreapp3.0\win10-x64\publish\"
if (Test-Path $path)
{
    Write-Debug "Deleting $path"
    Remove-Item $path -r -for
}

dotnet publish -c Release -r win10-x64 /p:PublishTrimmed=$trim /p:LinkAggressively=$aggro /p:LinkAwayReadyToRun=$trimr2r /p:PublishReadyToRun=$r2r /p:PublishSingleFile=$singleFile /bl

Push-Location $path
Write-Host ("Size is {0:N2} MB" -f ((Get-ChildItem . -Recurse | Measure-Object -Property Length -Sum -ErrorAction Stop).Sum / 1MB))

$AppPath = '.\' + $app
$console = $url -eq $null -or $url -eq ""

if ($time)
{
    $sum = 0
    for ($i = 0; $i -lt 11; $i++)
    {
        Write-Debug "Starting $app"
        
        if ($console) {
            $result = Measure-Command {    
                $proc = Start-Process -FilePath $AppPath -PassThru -NoNewWindow -RedirectStandardOutput '.\NUL'
                $proc.WaitForExit()
            }
        }
        else {
            $result = Measure-Command {
                $proc = Start-Process -FilePath $AppPath -PassThru -NoNewWindow -RedirectStandardOutput '.\NUL'
                Write-Debug "Making a request to $url"
                Invoke-WebRequest $url | Out-Null
            }
        }

        # Ignore first result
        if ($i -gt 0) {
            $sum += $result.TotalMilliseconds
            Write-Host $result.TotalMilliseconds
        }

        if (-not $console) {
            Write-Debug "Stopping"
            $proc.Kill();
        }
    }

    $avg = $sum / 10
    Write-Host "Average startup time (ms): $avg"

    if (-not $console) {
        # Measure working set after startup and one request
        Write-Debug "Starting $app"
        $proc = Start-Process -FilePath $AppPath -PassThru -NoNewWindow -RedirectStandardOutput '.\NUL'

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