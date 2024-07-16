<#
 .SYNOPSIS
    Setup Eflow IoT edge (must run as admin)

 .DESCRIPTION
    Setup Eflow IoT edge on the device. This script will install IoT Edge
    runtime, create a virtual switch, and configure DHCP.
#>

$eflowMsiUri = "https://aka.ms/AzEFLOWMSI_1_4_LTS_X64"

$ErrorActionPreference = "Stop"
$path = Split-Path $script:MyInvocation.MyCommand.Path

$setupPath = Join-Path $path "eflow-setup"
if (!(Test-Path $setupPath))
{
   New-Item -ItemType Directory -Path $setupPath | Out-Null
}

# Set-ExecutionPolicy -ExecutionPolicy AllSigned -Force
Start-Transcript -path $(join-path $setupPath "eflow-setup.log")

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force
Install-Module Subnet -Force

Write-Host "Download Azure IoT Edge eflow installer."
$msiPath = Join-Path $setupPath "AzureIoTEdge.msi"
if (!(Test-Path $msiPath))
{
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest $eflowMsiUri -OutFile $msiPath
}

Write-Host "Run Azure IoT Edge eflow installer."
Start-Process -Wait msiexec -ArgumentList "/i","$msiPath","/qn"

Write-Host "Deploy Azure IoT Edge eflow with switch $($switch)..."
Deploy-Eflow -acceptEula Yes `
   -acceptOptionalTelemetry Yes

Get-EflowVmAddr
Get-EflowVmEndpoint
Get-EflowNetwork -vSwitchName $switch

Stop-Transcript
Write-Host "Cleaning up..."
Remove-Item -Path $setupPath -Force -Recurse -ErrorAction SilentlyContinue
Write-Host "Azure IoT Edge eflow successfully installed!"
