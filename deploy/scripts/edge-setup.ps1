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

    $file = "/etc/aziot/config.yaml"
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
}
else {
    Set-ExecutionPolicy -ExecutionPolicy AllSigned -Force
    Start-Transcript -path (join-path $path "edge-setup.log")

    Write-Host "Create new IoT Edge enrollment."
    $enrollment = & $enrollPath -dpsConnString $dpsConnString -os Windows

    Write-Host "Download IoT Edge installer."
    $msiPath = $([io.Path]::Combine($env:TEMP, 'AzureIoTEdge.msi'))
    $ProgressPreference = 'SilentlyContinue'
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest "https://aka.ms/AzEFLOWMSI-CR-X64" -OutFile $msiPath

    Write-Host "Run IoT Edge installer."
    Start-Process -Wait msiexec -ArgumentList "/i","$([io.Path]::Combine($env:TEMP, 'AzureIoTEdge.msi'))","/qn"

    Write-Host "Deploy eflow."
    Deploy-Eflow -acceptEula Yes -acceptOptionalTelemetry Yes 
    Write-Host "Provision eflow."
    Provision-EflowVm -provisioningType DpsSymmetricKey -scopeId $idScope -registrationId $enrollment.registrationId -symmKey $enrollment.primaryKey
}
