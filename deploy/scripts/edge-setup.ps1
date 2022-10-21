<#
 .SYNOPSIS
    Configure IoT edge 

 .DESCRIPTION
    Configure IoT edge on linux vm to use DPS.

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

$file = "/etc/aziot/config.toml"
if (Test-Path $file) {
    $backup = "$($file)-backup"
    if (Test-Path $backup) {
        Write-Host "Already configured."
        return
    }
    $configtoml = Get-Content $file -Raw
    if ([string]::IsNullOrWhiteSpace($configtoml)) {
        throw "$($file) empty."
    }
    $configtoml | Out-File $backup -Force
}
else {
    throw "$($file) does not exist."
}

Write-Host "Create new IoT Edge enrollment."
$enrollment = & $enrollPath -dpsConnString $dpsConnString -os Linux
Write-Host "Configure and initialize IoT Edge on Linux using enrollment information."

# comment out existing 
$configtoml = $configtoml.Replace("`n[provisioning]", "`n# [provisioning]")
$configtoml = $configtoml.Replace("`nsource", "`n# source")
$configtoml = $configtoml.Replace("`ndevice_connection_string", "`n# device_connection_string")
$configtoml = $configtoml.Replace("`ndynamic_reprovisioning", "`n# dynamic_reprovisioning")

# add dps setting
$configtoml += "`n"
$configtoml += "`n########################################################################"
$configtoml += "`n# DPS symmetric key provisioning configuration - added by edge-setup.ps1 #"
$configtoml += "`n########################################################################"
$configtoml += "`n"
$configtoml += "`n[provisioning]"
$configtoml += "`nsource = `"dps`""
$configtoml += "`nglobal_endpoint = `"https://global.azure-devices-provisioning.net`""
$configtoml += "`nscope_id = `"$($idScope)`""
$configtoml += "`n"
$configtoml += "`n[provisioning.attestation]"
$configtoml += "`nmethod = `"symmetric_key`""
$configtoml += "`nregistration_id = `"$($enrollment.registrationId)`""
$configtoml += "`nsymmetric_key = `"$($enrollment.primaryKey)`""
$configtoml += "`n"
$configtoml += "`n########################################################################"
$configtoml += "`n"

$configtoml | Out-File $file -Force
