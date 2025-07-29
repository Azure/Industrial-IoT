
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

$eflowMsiUri = "https://aka.ms/AzEFLOWMSI_1_4_LTS_X64"

$ErrorActionPreference = "Stop"
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
Invoke-WebRequest $eflowMsiUri -OutFile $msiPath

Write-Host "Run IoT Edge installer."
Start-Process -Wait msiexec -ArgumentList "/i","$([io.Path]::Combine($env:TEMP, 'AzureIoTEdge.msi'))","/qn"

Write-Host "Existing virtual switches:"
Get-VmSwitch

$switch = "NestedSwitch"
Write-Host "Add virtual switch $($switch)..."
New-VMSwitch -Name $switch -SwitchType Internal

$switchAlias = "vEthernet ($($switch))"
Write-Host "Network Adapter for '$($switchAlias)'"
$itf = Get-NetAdapter -Name $switchAlias -ErrorAction SilentlyContinue
while (!$itf)
{
   Start-Sleep -Seconds 3
   $itf = Get-NetAdapter -Name $switchAlias -ErrorAction SilentlyContinue
}
$itf | Out-Host

$ifIndex = $itf.ifIndex
$virtualSwitchIp = Get-NetIPAddress -AddressFamily IPv4 -InterfaceIndex $ifIndex -ErrorAction SilentlyContinue
while (!$virtualSwitchIp)
{
   Start-Sleep -Seconds 3
   $virtualSwitchIp = Get-NetIPAddress -AddressFamily IPv4 -InterfaceIndex $ifIndex -ErrorAction SilentlyContinue
}
$virtualSwitchIp | Out-Host
$subnet = Get-Subnet -IP $virtualSwitchIp.IPAddress -MaskBits 24
Write-Host "Create new ip address $($subnet.HostAddresses[0])/$($subnet.MaskBits)"
New-NetIPAddress -IPAddress $subnet.HostAddresses[0] -PrefixLength $subnet.MaskBits -InterfaceIndex  $ifIndex
Write-Host "Create NAT $($subnet.NetworkAddress)}/$($subnet.MaskBits)"
New-NetNat -Name $switch -InternalIPInterfaceAddressPrefix "$($subnet.NetworkAddress)/$($subnet.MaskBits)"

Start-Sleep -Seconds 10
Write-Host "Configure DHCP"
cmd.exe /c "netsh dhcp add securitygroups"
Restart-Service dhcpserver
# select a set of 100 addresses
$startIp = $subnet.HostAddresses[100]
$endIp = $subnet.HostAddresses[200]
Write-Host "Add DHCP scope to $startIp - $endIp ..."
Add-DhcpServerV4Scope -Name "AzureIoTEdgeScope" -StartRange $startIp -EndRange $endIp -SubnetMask $subnet.SubnetMask -State Active
Set-DhcpServerV4OptionValue -ScopeID $subnet.NetworkAddress -Router $subnet.HostAddresses[0]
Restart-service dhcpserver

Write-Host "ipconfig:"
ipconfig /all

Write-Host "Deploy eflow with switch $($switch)."
Deploy-Eflow -acceptEula Yes -acceptOptionalTelemetry Yes -vSwitchType "Internal" -vSwitchName $switch

Get-EflowVmAddr
Get-EflowVmEndpoint
Get-EflowNetwork -vSwitchName $switch

Write-Host "Create new IoT Edge enrollment in DPS."
$enrollment = & $enrollPath -dpsConnString $dpsConnString -os Windows

Write-Host "Provision eflow with DPS registration $($enrollment.registrationId) in DPS scope $($idScope)."
Provision-EflowVm -provisioningType DpsSymmetricKey -scopeId $idScope -registrationId $enrollment.registrationId -symmKey $enrollment.primaryKey
Write-Host "Eflow provisioned."

Start-EflowVm
Verify-EflowVm
Write-Host "Eflow running."
