<#
 .SYNOPSIS
    Creates edge device in Iot Hub

 .DESCRIPTION
    1. Creates edge enabled IoT device in IoT hub
    2. Gets primary connection string of the newly created IoT Edge device

 .PARAMETER ResourceGroupName
    The resource group where the vm and iothub exists

 .PARAMETER IoTHubName
    The name of Iot Hub. 

 .PARAMETER IoTHubConnectionString
    Connection string of Iot Hub.

 .PARAMETER Env
    The simulation environment i.e. windows or linux

 .PARAMETER VmCount
    Number of edge devices to create
#>

param(
    [Parameter(Mandatory=$True)] [string] $ResourceGroupName,
    [Parameter(Mandatory=$True)] [string] $IoTHubName,
    [Parameter(Mandatory=$True)] [string] $IoTHubConnectionString,
    [Parameter(Mandatory=$True)] [string] $Env,
    [Parameter(Mandatory=$True)] [int] $VmCount
)

#******************************************************************************
# Create SAS token
#******************************************************************************
Function CreateSASToken {
  param(
    [Parameter(Mandatory = $True)]
    [string]$ResourceUri,
    [Parameter(Mandatory = $True)]
    [string]$Key,
    [string]$KeyName = "",
    [int]$TokenTimeOut = 1800 # in seconds
  )
  [Reflection.Assembly]::LoadWithPartialName("System.Web") | Out-Null
  $Expires = ([DateTimeOffset]::Now.ToUnixTimeSeconds()) + $TokenTimeOut
  #Building Token
  $SignatureString = [System.Web.HttpUtility]::UrlEncode($ResourceUri) + "`n" + [string]$Expires
  $HMAC = New-Object System.Security.Cryptography.HMACSHA256
  $HMAC.key = [Convert]::FromBase64String($Key)
  $Signature = $HMAC.ComputeHash([Text.Encoding]::ASCII.GetBytes($SignatureString))
  $Signature = [Convert]::ToBase64String($Signature)
  $SASToken = "SharedAccessSignature sr=" + [System.Web.HttpUtility]::UrlEncode($ResourceUri) + "&sig=" + [System.Web.HttpUtility]::UrlEncode($Signature) + "&se=" + $Expires
  if ($KeyName -ne "") {
    $SASToken = $SASToken + "&skn=$KeyName"
  }
  return $SASToken
}

#******************************************************************************
# Create Edge device
#******************************************************************************
Function CreateEdgeDevice {
  param(
    [Parameter(Mandatory = $True)]
    [string]$IoTHubConnectionString,
    [Parameter(Mandatory = $True)]
    [string]$DeviceId
  )
  [Reflection.Assembly]::LoadWithPartialName("System.Web") | Out-Null
  $strings = $IoTHubConnectionString.split(";")
  $keys = @{}
  for ($i = 0; $i -lt $strings.count; $i++) {
    $keys[$strings[$i].split("=")[0]] = $strings[$i].split("=")[1]
  }
  $keys["SharedAccessKey"] = $keys["SharedAccessKey"] + "="
  $body = '{"deviceId":"' + $DeviceId + '",
            "capabilities": {"iotEdge": true}
           }'

  try {
    $webRequest = Invoke-WebRequest -Method PUT -Uri "https://$($keys["HostName"])/devices/$([System.Web.HttpUtility]::UrlEncode($DeviceId))?api-version=2018-06-30" -ContentType "application/json" -Header @{ Authorization = (CreateSASToken -ResourceUri $keys["HostName"] -Key $keys["SharedAccessKey"] -KeyName $keys["SharedAccessKeyName"]) } -Body $body
  } catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 409) {
      Write-Host "Getting data from IoT hub"
      $webRequest = Invoke-WebRequest -Method GET -Uri "https://$($keys["HostName"])/devices/$([System.Web.HttpUtility]::UrlEncode($DeviceId))?api-version=2018-06-30" -ContentType "application/json" -Header @{ Authorization = (CreateSASToken -ResourceUri $keys["HostName"] -Key $keys["SharedAccessKey"] -KeyName $keys["SharedAccessKeyName"]) }
    }
    else {
      Write-Error "An exception was caught: $($_.Exception.Message)"
    }
  }
  return $webRequest
}


#******************************************************************************
# Script body
#******************************************************************************

$resourceGroupName = $script:ResourceGroupName
$iothub = $script:IoTHubConnectionString
$iothubName = $script:IoTHubName

Write-Host "Creating edge device.."
$deviceNum = 0
$edgeDeviceConnectionString = @()
while ($deviceNum -lt $script:VmCount) {
    # Create edge enabled iot device
    $deviceId = $script:Env + "EdgeDevice" + $deviceNum
    $device = CreateEdgeDevice -IoTHubConnectionString $iothub -DeviceId $deviceId

    # Get shared key from the device
    $device = $device | ConvertFrom-Json
    Write-Host "Fetching device key.."
    $key = $device.authentication.symmetricKey.primaryKey
    $edgeDeviceConnectionString += "'HostName=${iothubName}.azure-devices.net;DeviceId=${deviceId};SharedAccessKey=${key}'"

    $deviceNum += 1 # Increase the value stored in the variable by 1.
}
# Connect newly created simulation VM with newly created edge device in IotHub
Write-Host 
Write-Host "Setting connection string in the pipeline variables.."
Write-Host 
$keyname = "Output_edgeKey"
$edgeDeviceCombinedString = $edgeDeviceConnectionString -join "|"
Write-Output "##vso[task.setvariable variable=$keyname]$edgeDeviceCombinedString"
