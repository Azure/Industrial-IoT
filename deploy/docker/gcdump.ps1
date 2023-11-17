<#
 .SYNOPSIS
    Create gc dump files periodically

 .PARAMETER WaitSeconds
    The number of seconds to wait in between dumps
#>

param(
    [int] $WaitSeconds = 1,
    [string] $Path = "dumps"
)

# start with monitor and dump gc
Remove-Item -Path $Path -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $Path
$StartTime = $(get-date)
docker compose -f docker-compose.yaml -f with-monitor.yaml -f with-limits.yaml up -d
$dumpIndex = 0
while ($true) {
    Start-Sleep -Seconds $WaitSeconds
    try
    {
        curl http://localhost:9084/gcdump -o $Path/dump$($dumpIndex).gcdump
    }
    catch {
        break
    }
    $dumpIndex++
}
$elapsedTime = $(get-date) - $StartTime
Write-Host "Ran for (hh:mm:ss) $($elapsedTime.ToString("hh\:mm\:ss"))"
#docker compose -f docker-compose.yaml -f with-monitor.yaml -f with-limits.yaml down
