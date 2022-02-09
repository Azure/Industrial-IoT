<#
 .SYNOPSIS
    Installs IoT edge 

 .DESCRIPTION
    Installs IoT edge on either linux or windows vm and enrolls vm in DPS.

 .PARAMETER dpsConnString
    The Dps connection string

 .PARAMETER idScope
    The Dps id scope
#>
param(
    [Parameter(Mandatory)]
    [string] $dpsConnString,
    [Parameter(Mandatory)]
    [string] $idScope
)

$path = Split-Path $script:MyInvocation.MyCommand.Path
$enrollPath = join-path $path dps-enroll.ps1
if ($PsVersionTable.Platform -eq "Unix") {

    $file = "/etc/iotedge/config.yaml"
    if (Test-Path $file) {
        $backup = "$($file)-backup"
        if (Test-Path $backup) {
            Write-Host "Already configured."
            return
        }
        $configyml = Get-Content $file -Raw
        if ([string]::IsNullOrWhiteSpace($configyml)) {
            throw "$($file) empty."
        }
        $configyml | Out-File $backup -Force
    }
    else {
        throw "$($file) does not exist."
    }

    Write-Host "Create new IoT Edge enrollment."
    $enrollment = & $enrollPath -dpsConnString $dpsConnString -os Linux
    Write-Host "Configure and initialize IoT Edge on Linux using enrollment information."

    # comment out existing 
    $configyml = $configyml.Replace("`nprovisioning:", "`n#provisioning:")
    $configyml = $configyml.Replace("`n  source:", "`n#  source:")
    $configyml = $configyml.Replace("`n  device_connection_string:", "`n#  device_connection_string:")
    $configyml = $configyml.Replace("`n  dynamic_reprovisioning:", "`n#  dynamic_reprovisioning:")

    # add dps setting
    $configyml += "`n"
    $configyml += "`n########################################################################"
    $configyml += "`n# DPS symmetric key provisioning configuration - added by edge-setup.ps1 #"
    $configyml += "`n########################################################################"
    $configyml += "`n"
    $configyml += "`nprovisioning:"
    $configyml += "`n  source: `"dps`""
    $configyml += "`n  global_endpoint: `"https://global.azure-devices-provisioning.net`""
    $configyml += "`n  scope_id: `"$($idScope)`""
    $configyml += "`n  attestation:"
    $configyml += "`n    method: `"symmetric_key`""
    $configyml += "`n    registration_id: `"$($enrollment.registrationId)`""
    $configyml += "`n    symmetric_key: `"$($enrollment.primaryKey)`""
    $configyml += "`n"
    $configyml += "`n########################################################################"
    $configyml += "`n"

    $configyml | Out-File $file -Force
}
else {
    Start-Transcript -path (join-path $path "edge-setup.log")

    Write-Host "Create new IoT Edge enrollment."
    $enrollment = & $enrollPath -dpsConnString $dpsConnString -os Windows
    Write-Host "Add URL resrvation."
    $domainUser="Everyone"
    $ports = @('9700', '9701', '9702')
    foreach ($port in $ports) {
        netsh http add urlacl url=http://+:$port/metrics user=$domainUser
    }
    Write-Host "Configure and initialize IoT Edge on Windows using enrollment information."
    . { Invoke-WebRequest -useb https://aka.ms/iotedge-win } | Invoke-Expression; `
        Install-IoTEdge -Dps -ScopeId $idScope -ContainerOs Windows -RegistrationId `
            $enrollment.registrationId -SymmetricKey $enrollment.primaryKey
    Write-Host "Updating TLS Settings in Windows VM. Rebooting if required"
    $tlsScriptPath = join-path $path TLSSettings.ps1
    & $tlsScriptPath

}
