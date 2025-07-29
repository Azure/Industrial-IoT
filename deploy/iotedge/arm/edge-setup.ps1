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
    Write-Host "Already configured."
    return
}

Write-Host "Create new IoT Edge enrollment."
$enrollment = & $enrollPath -dpsConnString $dpsConnString -os Linux
Write-Host "Configure and initialize IoT Edge on Linux using enrollment information."

# add dps setting
$configtoml = "`nauto_reprovisioning_mode = `"OnErrorOnly`""
$configtoml += "`n"
$configtoml += "`n[aziot_keys]"
$configtoml += "`n"
$configtoml += "`n[preloaded_keys]"
$configtoml += "`n"
$configtoml += "`n[cert_issuance]"
$configtoml += "`n"
$configtoml += "`n[preloaded_certs]"
$configtoml += "`n"
$configtoml += "`n[tpm]"
$configtoml += "`n"
$configtoml += "`n[agent]"
$configtoml += "`nname = `"edgeAgent`""
$configtoml += "`ntype = `"docker`""
$configtoml += "`nimagePullPolicy = `"on-create`""
$configtoml += "`n"
$configtoml += "`n[agent.config]"
$configtoml += "`nimage = `"mcr.microsoft.com/azureiotedge-agent:1.4`""
$configtoml += "`n"
$configtoml += "`n[agent.config.createOptions]"
$configtoml += "`n"
$configtoml += "`n[agent.env]"
$configtoml += "`n"
$configtoml += "`n[connect]"
$configtoml += "`nworkload_uri = `"unix:///var/run/iotedge/workload.sock`""
$configtoml += "`nmanagement_uri = `"unix:///var/run/iotedge/mgmt.sock`""
$configtoml += "`n"
$configtoml += "`n[listen]"
$configtoml += "`nworkload_uri = `"fd://aziot-edged.workload.socket`""
$configtoml += "`nmanagement_uri = `"fd://aziot-edged.mgmt.socket`""
$configtoml += "`nmin_tls_version = `"tls1.0`""
$configtoml += "`n[watchdog]"
$configtoml += "`nmax_retries = `"infinite`""
$configtoml += "`n"
$configtoml += "`n[provisioning]"
$configtoml += "`nsource = `"dps`""
$configtoml += "`nglobal_endpoint = `"https://global.azure-devices-provisioning.net`""
$configtoml += "`nid_scope = `"$($idScope)`""
$configtoml += "`n"
$configtoml += "`n[provisioning.attestation]"
$configtoml += "`nmethod = `"symmetric_key`""
$configtoml += "`nregistration_id = `"$($enrollment.registrationId)`""
$configtoml += "`nsymmetric_key = { value = `"$($enrollment.primaryKey)`" }"
$configtoml += "`n"

$configtoml | Out-Host
$configtoml | Out-File $file -Force
