param(
    [string] $app,
    [string] $url = "https://localhost:5001/",
    [switch] $trim,
    [switch] $trimr2r,
    [switch] $aggro,
    [switch] $r2r,
    [switch] $time)

if (Test-Path "obj\")
{
    Write-Debug "Deleting obj"
    rm "obj\" -r -for
}    

$path = "bin\Release\netcoreapp3.0\win10-x64\publish\"
if (Test-Path $path)
{
    Write-Debug "Deleting $path"
    rm $path -r -for
}

dotnet publish -c Release -r win10-x64 /p:PublishTrimmed=$trim /p:LinkAggressively=$aggro /p:LinkAwayReadyToRun=$trimr2r /p:PublishReadyToRun=$r2r /bl
pushd $path
Write-Host ("Size is {0:N2} MB" -f ((Get-ChildItem . -Recurse | Measure-Object -Property Length -Sum -ErrorAction Stop).Sum / 1MB))

if ($time)
{
    $sum = 0
    for ($i = 0; $i -lt 10; $i++)
    {
        $result = Measure-Command {
            Write-Debug "Starting $app"
            $proc = Start-Process -FilePath "dotnet" -ArgumentList $app -PassThru

            Write-Debug "Making a request to $url"
            curl $url | Out-Null
        }

        $sum += $result.TotalMilliseconds
        Write-Host $result.TotalMilliseconds
        Write-Debug "Stopping"
        $proc.Kill();
    }

    $avg = $sum / 10
    Write-Host "Average: $avg"

    Write-Debug "Starting $app"
    $proc = Start-Process -FilePath "dotnet" -ArgumentList $app -PassThru

    Write-Debug "Making a request to $url"
    curl $url | Out-Null

    $proc.Refresh()
    $mb = [int]$proc.WorkingSet / 1MB
    Write-Host "Working set: $mb"
    Write-Debug "Stopping"
    $proc.Kill();
}
else
{
    dotnet $app
}

popd