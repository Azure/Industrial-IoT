<#
 .SYNOPSIS
    Installs Industrial IoT edge on windows

 .DESCRIPTION
    Installs Industrial IoT edge on windows and finishes installation.

 .PARAMETER edgeKey
    The IoT edge connection string
#>
param(
    [string] $edgeKey,
    [Switch] $Install
)

$path = $script:MyInvocation.MyCommand.Path

if ([string]::IsNullOrEmpty($edgeKey)) {
    Write-Host "Nothing to do."
    return
}
else {
    if (!$Install.IsPresent) {
        Write-Host "Deploying IoT Edge to machine."

        . { Invoke-WebRequest -useb https://aka.ms/iotedge-win } | Invoke-Expression; `
            Deploy-IoTEdge -RestartIfNeeded

        # Register ourselves to initilaize edge        
        $trigger = New-JobTrigger -AtStartup -RandomDelay 00:00:30
        $out = Register-ScheduledJob -Trigger $trigger –Name "IotEdge" -FilePath $path -ArgumentList @($script:edgeKey, "-Install")
        Write-Host $out.Command

        Write-Host "Restart to finish installation."
        Restart-Computer
    }
    else {
        Write-Host "Configure and initialize IoT Edge."
        . { Invoke-WebRequest -useb https://aka.ms/iotedge-win } | Invoke-Expression; `
            Initialize-IoTEdge -Manual -DeviceConnectionString "$script:edgeKey"
    }
}