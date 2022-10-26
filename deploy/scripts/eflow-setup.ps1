
<#
 .SYNOPSIS
    Setup Eflow IoT edge 

 .DESCRIPTION
    Setup Eflow IoT edge on windows vm to use DPS using DSC.

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

# Set-ExecutionPolicy -ExecutionPolicy AllSigned -Force
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
Provision-EflowVm -provisioningType DpsSymmetricKey -scopeId $idScope ´
    -registrationId $enrollment.registrationId -symmKey $enrollment.primaryKey
   
Write-Host "Eflow provisioned and running."
