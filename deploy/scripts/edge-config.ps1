
<#
 .SYNOPSIS
    Configure IoT edge 

 .DESCRIPTION
    Configure IoT edge on windows vm to use DPS using DSC.

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

# dsc configuration
Configuration DeployEflow
{
    Import-DscResource -ModuleName PsDesiredStateConfiguration

    WindowsFeature HyperV
    {
        Name = "Hyper-V"
        Ensure = "Present"
        IncludeAllSubFeature = $true
    }

    Script Reboot
    {
        TestScript = {
            return (Test-Path HKLM:\SOFTWARE\MyMainKey\RebootKey)
        }
        SetScript = {
            New-Item -Path HKLM:\SOFTWARE\MyMainKey\RebootKey -Force
            $global:DSCMachineStatus = 1 
        }
        GetScript = { return @{result = 'result'}}
        DependsOn = '[WindowsFeature]HyperV'
    }

    # todo: Use MSIPackage to install

    Script Eflow
    {
        TestScript = {
            $eflowInfo = Get-ChildItem -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' ´
                | Get-ItemProperty |  Where-Object { $_.DisplayName -match 'Azure IoT Edge *' }
            # todo test provisioning completed
            return ($null -ne $eflowInfo)
        }
        SetScript = {
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
        }
        GetScript = { return @{result = 'result'}}
        DependsOn = '[Script]Reboot'
    }
}

# deploy eflow
DeployEflow