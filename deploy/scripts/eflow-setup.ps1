
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

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force
Install-Module Subnet -Force

Write-Host "Download IoT Edge installer."
$msiPath = $([io.Path]::Combine($env:TEMP, 'AzureIoTEdge.msi'))
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest "https://aka.ms/AzEFLOWMSI-CR-X64" -OutFile $msiPath

Write-Host "Run IoT Edge installer."
Start-Process -Wait msiexec -ArgumentList "/i","$([io.Path]::Combine($env:TEMP, 'AzureIoTEdge.msi'))","/qn"

Get-VmSwitch
$switch = "NestedSwitch"
Write-Host "Add virtual switch $($switch)..."
New-VMSwitch -Name $switch -SwitchType Internal
$ifIndex = (Get-NetAdapter -Name "vEthernet ($($switch))").ifIndex
$virtualSwitchIp = Get-NetIPAddress -AddressFamily IPv4 -InterfaceIndex $ifIndex
$subnet = Get-Subnet -IP $virtualSwitchIp -MaskBits 24
New-NetIPAddress -IPAddress $subnet.HostAddresses[0] -PrefixLength $subnet.MaskBits -InterfaceIndex  $ifIndex
New-NetNat -Name $switch -InternalIPInterfaceAddressPrefix "{$($subnet.NetworkAddress)}/$($subnet.MaskBits)"

Write-Host "Configure DHCP"
cmd.exe /c "netsh dhcp add securitygroups"
Restart-Service dhcpserver
# select a set of 100 addresses
$startIp = $subnet.HostAddresses[100]
$endIp = $subnet.HostAddresses[200]
Add-DhcpServerV4Scope -Name "AzureIoTEdgeScope" -StartRange $startIp -EndRange $endIp -SubnetMask $subnet.SubnetMask -State Active
Set-DhcpServerV4OptionValue -ScopeID $subnet.NetworkAddress -Router $subnet.HostAddresses[0]
Restart-service dhcpserver

ipconfig /all

Write-Host "Deploy eflow with switch $($switch)."
Deploy-Eflow -acceptEula Yes -acceptOptionalTelemetry Yes -vSwitchType "Internal" -vSwitchName $switch

Get-EflowVmAddr
Get-EflowVmEndpoint
Get-EflowNetwork -vSwitchName $switch

Write-Host "Create new IoT Edge enrollment in DPS."
$enrollment = & $enrollPath -dpsConnString $dpsConnString -os Windows

Write-Host "Provision eflow with DPS registration $($enrollment.registrationId) in DPS scope $($idScope)."
Provision-EflowVm -provisioningType DpsSymmetricKey -scopeId $idScope ´
    -registrationId $enrollment.registrationId -symmKey $enrollment.primaryKey

Write-Host "Eflow provisioned and running."
