<#
   .SYNOPSIS
      Setup Eflow IoT edge (must run as admin)
   .DESCRIPTION
      Setup Eflow IoT edge on the device. This script will install the
      Azure IoT Edge runtime and deploy the Eflow IoT edge modules
      specified in the eflow-setup.json manifest.
   .NOTES
      DO NOT USE FOR PRODUCTION SYSTEMS. This script is intended for
      development and testing purposes only.

   .PARAMETER IotHubName
      The IoT Hub name.
   .PARAMETER TenantId
      The tenant id to use when logging into Azure.
   .PARAMETER SubscriptionId
      The subscription id to scope all activity to.
   .PARAMETER SharedFolderPath
      The shared folder path on the host system to mount into the guest.
   .PARAMETER ProvisioningOnly
      Only provision an existing eflow vm to an Azure IoT Hub.
   .PARAMETER DebuggingSupport
      Enable debugging support in eflow.
   .PARAMETER NoModules
      Do not deploy any modules.
   .PARAMETER NoCleanup
      Perform no cleanup after successfuly run.
#>

param(
   [string] $IotHubName,
   [string] $TenantId = "6e54c408-5edd-4f87-b3bb-360788b7ca18",
   [string] $SubscriptionId,
   [string] $SharedFolderPath,
   [switch] $ProvisioningOnly,
   [switch] $DebuggingSupport,
   [ValidateSet("Debug", "Release")]
   [string] $Configuration = "Debug",
   [switch] $NoModules,
   [switch] $NoCleanup
)

$eflowMsiUri = "https://aka.ms/AzEFLOWMSI_1_4_LTS_X64"

$ErrorActionPreference = "Stop"
$path = Split-Path $script:MyInvocation.MyCommand.Path
$Configuration = "Release"

$setupPath = Join-Path $path "eflow-setup"
if (!(Test-Path $setupPath)) {
   New-Item -ItemType Directory -Path $setupPath | Out-Null
}

# Set-ExecutionPolicy -ExecutionPolicy AllSigned -Force
Start-Transcript -path $(join-path $setupPath "eflow-setup.log") -Append

Update-AzConfig -DisplayBreakingChangeWarning $false | Out-Null
if (![string]::IsNullOrWhiteSpace($SubscriptionId)) {
   Update-AzConfig -DefaultSubscriptionForLogin $SubscriptionId
}
$azargs = @{}
if (![string]::IsNullOrWhiteSpace($TenantId)) {
   $azargs.Add("-Tenant", $TenantId)
}
Connect-AzAccount @azargs

# Find iot hub
if ([string]::IsNullOrWhiteSpace($IotHubName)) {
   Write-Host "Please choose an Azure Iot Hub from the list (using its index):"
   $script:index = 0
   $hubs = Get-AzIoTHub
   $hubs | Format-Table -AutoSize -Property `
   @{Name = "Index"; Expression = { ($script:index++) } }, `
   @{Name = "Hub"; Expression = { $_.Name } }`
   | Out-Host
   while ($true) {
      $option = Read-Host ">"
      try {
         if ([int]$option -ge 1 -and [int]$option -le $hubs.Count) {
            break
         }
      }
      catch {
         Write-Host "Invalid index '$($option)' provided."
      }
      Write-Host "Choose from the list using an index between 1 and $($hubs.Count)."
   }
   $hub = $hubs[$option - 1]
}
else {
   $hub = Get-AzIoTHub | Where-Object Name -eq $IotHubName
   if (!$hub) {
      throw "IoT Hub $IotHubName not found."
   }
}

if (!$ProvisioningOnly.IsPresent) {
   Write-Host "(Re-) Installing Azure IoT Edge eflow..."
   [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
   Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force | Out-Null
   Install-Module Subnet -Force | Out-Null

   $msiPath = Join-Path $setupPath "AzureIoTEdge.msi"
   if (!(Test-Path $msiPath)) {
      Write-Host "Downloading Azure IoT Edge eflow installer..."
      [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
      Invoke-WebRequest $eflowMsiUri -OutFile $msiPath
   }

   # uninstall existing IoT Edge runtime if needed
   Start-Process -Wait msiexec -ArgumentList "/x", "$msiPath", "/qn" `
      -ErrorAction SilentlyContinue | Out-Null

   Write-Host "Run Azure IoT Edge eflow installer."
   Start-Process -Wait msiexec -ArgumentList "/i", "$msiPath", "/qn"

   Write-Host "Deploy Azure IoT Edge eflow ..."
   Deploy-Eflow -acceptEula Yes -acceptOptionalTelemetry Yes
   if ($LASTEXITCODE -ne 0) {
      throw "Failed to deploy eflow."
   }

   $mountPath = "C:\Shared"
   if (![string]::IsNullOrWhiteSpace($SharedFolderPath)) {
      $mountPath = $SharedFolderPath
   }
   $fullPath = Join-Path $mountPath "EFLOW-Shared"
   if (!(Test-Path $fullPath)) {
      New-Item -ItemType Directory -Path $fullPath | Out-Null
   }
   $sharedFolderConfig = @(
      @{
         sharedFolderRoot = $mountPath
         sharedFolders    = @(
            @{
               hostFolderPath      = "EFLOW-Shared"
               readOnly            = $false
               targetFolderOnGuest = "/tmp/host"
            }
         )
      }
   )
   Write-Host "Adding shared r/w folder $fullPath to eflow vm as /mount..."
   $sharedFoldersJsonPath = Join-Path $setupPath "SharedFolders.json"
   $sharedFolderConfig | ConvertTo-Json `
   | Set-Content -Path $sharedFoldersJsonPath -Force -Encoding UTF8
   Add-EflowVmSharedFolder -sharedFoldersJsonPath $sharedFoldersJsonPath
   if ($LASTEXITCODE -ne 0) {
      throw "Failed to add shared folder."
   }
   Write-Host "Successfully installed and deployed eflow."
   Get-EflowNetwork | ConvertTo-Json
   Get-EflowVmAddr | ConvertTo-Json
   Get-EflowVmEndpoint | ConvertTo-Json

   #Get-EflowNetworkInterface
   #Get-EflowHostConfiguration
   #Get-EflowVmTelemetryOption
   #Get-EflowVmUserName
   #Get-EflowVmFeature
   #Get-EflowVmSharedFolder
   #Get-EflowVmTpmProvisioningInfo
   #Connect-EflowVm Connect to eflow vm
}

Write-Host "Provisioning eflow vm..."
$ok = Verify-EflowVm
if (!$ok) {
   throw "Eflow VM not created."
}

$vmName = Get-EflowVmName
if (!$vmName) {
   throw "Failed to get eflow vm name."
}

Write-Host "Creating new IoT Edge device $vmName in $($hub.Name)..."
$device = $hub | Get-AzIotHubDevice -DeviceId $vmName
if (!$device.EdgeEnabled) {
   $hub | Remove-AzIotHubDevice -DeviceId $vmName
   $device = $null
}
if (!$device) {
   $device = $hub | Add-AzIotHubDevice -DeviceId $vmName `
      -AuthMethod shared_private_key -Status Enabled -EdgeEnabled
}
$devConnString = $hub | Get-AzIotHubDeviceConnectionString -DeviceId $device.Id
if (!$devConnString) {
   throw "Failed to get device connection string for $vmName."
}
if ([string]::IsNullOrWhiteSpace($devConnString.ConnectionString)) {
   throw "Device connection string for $vmName was empty."
}

Write-Host "Provision Azure IoT Edge Eflow VM $vmName..."
# ensure started
Start-EflowVm

if ($DebuggingSupport.IsPresent) {
   Write-Host "Configuring debugging support in eflow..."

   $ds = "/etc/systemd/system/docker.service"
   # Configure the EFLOW virtual machine Docker engine to accept external
   # connections, and add the appropriate firewall rules.
   Invoke-EflowVmCommand `
      "sudo iptables -A INPUT -p tcp --dport 2375 -j ACCEPT"
   # Create a copy of the EFLOW VM _docker.service_ in the system folder.
   Invoke-EflowVmCommand `
      "sudo cp /lib/systemd/system/docker.service $ds"
   # Replace the service execution line to listen for external connections.
   Invoke-EflowVmCommand `
      "sudo sed -i 's/-H fd:\/\// -H fd:\/\/ -H tcp:\/\/0.0.0.0:2375/g'  $ds"
   # Reload the EFLOW VM services configurations.
   Invoke-EflowVmCommand `
      "sudo systemctl daemon-reload"
   # Reload the Docker engine service.
   Invoke-EflowVmCommand `
      "sudo systemctl restart docker.service"
   # Check that the Docker engine is listening to external connections.
   Invoke-EflowVmCommand `
      "sudo netstat -lntp | grep dockerd"

   $vmIp = Get-EflowVmAddr

   if (!$NoModules.IsPresent) {
      $containerRegistry = "$($hub.Name)acr" -replace '[^a-zA-Z0-9]'
      $registry = Get-AzContainerRegistry -Name $containerRegistry `
         -ResourceGroupName $hub.Resourcegroup -ErrorAction SilentlyContinue
      if (!$registry) {
         Write-Host "Creating ACR $($containerRegistry) in $($hub.Location) ..."
         $registry = New-AzContainerRegistry -Name $containerRegistry `
            -ResourceGroupName $hub.Resourcegroup -Location $hub.Location `
            -EnableAdminUser -Sku Standard
      }
      Write-Host "Getting credentials from ACR $($containerRegistry) ..."
      $registrySecret = Get-AzContainerRegistryCredential -Name $containerRegistry `
         -ResourceGroupName $hub.Resourcegroup

      $containerRegistryUsername = $registrySecret.Username
      $containerRegistryPassword = $registrySecret.Password
      $containerRegistryServer = $registry.LoginServer

      Connect-AzContainerRegistry -Name $containerRegistry

      $proj = $path | Split-Path | Split-Path | `
         Join-Path -ChildPath "src" | `
         Join-Path -ChildPath "Azure.IIoT.OpcUa.Publisher.Module" | `
         Join-Path -ChildPath "src" | `
         Join-Path -ChildPath "Azure.IIoT.OpcUa.Publisher.Module.csproj"

      Write-Host "Building and pushing debug module to $containerRegistry..."
      dotnet publish $proj -c $Configuration --self-contained false `
         /t:PublishContainer /p:ContainerImageTag=debug `
         /p:ContainerRegistry=$containerRegistryServer

      $image = "$containerRegistryServer/iotedge/opc-publisher:debug"
   }
   Write-Host "Use 'docker -H tcp://$($vmIp[1]):2375' to connect to docker."
   Write-Host "Follow instructions in https://aka.ms/iotedge-eflow-debugging."
}

Provision-EflowVm -provisioningType ManualConnectionString `
   -devConnString $devConnString.ConnectionString
if ($LASTEXITCODE -ne 0) {
   throw "Failed to provision eflow vm $vmName."
}
Write-Host "Azure IoT Edge Eflow VM $vmName provisioned."

# work around for ConvertFrom-Json not supporting -AsHashtable in PS 5.1
function ConvertPSObjectToHashtable {
   param ([Parameter(ValueFromPipeline)] $InputObject)
   if ($null -eq $InputObject) {
      return $null
   }
   if ($InputObject -is [System.Collections.IEnumerable] `
         -and $InputObject -isnot [string]) {
      $collection = @( foreach ($object in $InputObject) {
         ConvertPSObjectToHashtable $object
      } )
      Write-Output -NoEnumerate $collection
   }
   elseif ($InputObject -is [psobject]) {
      $hash = @{}
      foreach ($property in $InputObject.PSObject.Properties) {
         $hash[$property.Name] = `
         (ConvertPSObjectToHashtable $property.Value).PSObject.BaseObject
      }
      $hash
   }
   else {
      $InputObject
   }
}
if (!$NoModules.IsPresent) {
   Write-Host "Deploying modules..."
   $modulesContentFile = Join-Path $path "eflow-setup.json"
   if (!(Test-Path $modulesContentFile)) {
      throw "Module content file $modulesContentFile not found."
   }
   $modulesContent = Get-Content -Raw -Path $modulesContentFile `
   | ConvertFrom-Json | ConvertPSObjectToHashtable
   if ($DebuggingSupport.IsPresent) {
      # Update with our debug image
      $desired = $modulesContent["`$edgeAgent"]["properties.desired"]
      $desired["modules"]["publisher"]["settings"]["image"] = $image
      $desired["runtime"]["settings"]["registryCredentials"] = @{
         $containerRegistry = @{
            username = $containerRegistryUsername
            password = $containerRegistryPassword
            address = $containerRegistryServer
         }
      }
      # $modulesContent | ConvertTo-Json -Depth 10
   }
   $hub | Set-AzIotHubEdgeModule -DeviceId $device.Id `
      -ModulesContent $modulesContent | Out-Null
}

Start-Sleep -Seconds 10
$vm = Get-EflowVm
if (!$vm) {
   throw "Failed to get eflow vm."
}
Write-Host ""
Write-Host "Edge status:"
Write-Host "========================================================"
Write-Host ""
$vm.EdgeRuntimeStatus.SystemCtlStatus | ForEach-Object { $_ | Out-Host }
$vm.EdgeRuntimeStatus.ModuleList | ForEach-Object { $_ | Out-Host }
Write-Host ""
Write-Host "========================================================"
Write-Host ""
Write-Host "Azure IoT Edge eflow successfully installed!"

Stop-Transcript
if (!$NoCleanup.IsPresent) {
   Write-Host "Cleaning up..."
   Remove-Item -Path $setupPath -Force -Recurse -ErrorAction SilentlyContinue
}
else {
   Remove-Item -Path $(join-path $setupPath "eflow-setup.log") -Force `
      -ErrorAction SilentlyContinue
   Get-EflowLogs -zipName $(Join-Path $setupPath "eflow-logs.zip")
}

