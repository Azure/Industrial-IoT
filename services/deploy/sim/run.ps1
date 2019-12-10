<#
 .SYNOPSIS
    Deploys to Azure

 .DESCRIPTION
    Deploys an Azure Resource Manager template of choice

 .PARAMETER applicationName
    The name of the application. 

 .PARAMETER resourceGroupName
    The resource group where the template will be deployed. 

 .PARAMETER containerRegistryPrefix
    Optional, a container registry prefix from which to pull the micro services containers to deploy.

 .PARAMETER aadConfig
    The AAD configuration the template will be configured with.

 .PARAMETER interactive
    Whether to run in interactive mode
#>

param(
    [Parameter(Mandatory=$True)] [string] $applicationName,
    [Parameter(Mandatory=$True)] [string] $resourceGroupName,
    $aadConfig = $null,
    $containerRegistryPrefix = $null,
    $interactive = $true
)

#******************************************************************************
# Get env file content from deployment
#******************************************************************************
Function GetEnvironmentVariables() {
    Param(
        $deployment
    )

    $IOTHUB_NAME = $deployment.Outputs["iothub-name"].Value
    $IOTHUB_ENDPOINT = $deployment.Outputs["iothub-endpoint"].Value
    $IOTHUB_CONSUMERGROUP = $deployment.Outputs["iothub-consumer-group"].Value
    $IOTHUB_CONNSTRING = $deployment.Outputs["iothub-connstring"].Value
    $BLOB_ACCOUNT = $deployment.Outputs["azureblob-account"].Value
    $BLOB_KEY = $deployment.Outputs["azureblob-key"].Value
    $BLOB_ENDPOINT_SUFFIX = $deployment.Outputs["azureblob-endpoint-suffix"].Value
    $DOCUMENTDB_CONNSTRING = $deployment.Outputs["docdb-connstring"].Value
    $SIGNALR_CONNSTRING = $deployment.Outputs["signalr-connstring"].Value
    $EVENTHUB_CONNSTRING = $deployment.Outputs["eventhub-connstring"].Value
    $EVENTHUB_NAME = $deployment.Outputs["eventhub-name"].Value
    $SERVICEBUS_CONNSTRING = $deployment.Outputs["sb-connstring"].Value
    $KEYVAULT_URL = $deployment.Outputs["keyvault-url"].Value
    $AZURE_WEBSITE = $deployment.Outputs["azureWebsite"].Value
    $WORKSPACE_NAME = $deployment.Outputs["workspace-name"].Value
    $APPINSIGHTS_NAME = $deployment.Outputs["appinsights-name"].Value
    $APPINSIGHTS_INSTRUMENTATIONKEY = $deployment.Outputs["appinsights-instrumentationkey"].Value
    $ADLSG2_ACCOUNT = $deployment.Outputs["adlsg2-account"].Value

    Write-Output `
        "_HUB_CS=$IOTHUB_CONNSTRING"
    Write-Output `
        "PCS_IOTHUB_CONNSTRING=$IOTHUB_CONNSTRING"
    Write-Output `
        "PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING=$DOCUMENTDB_CONNSTRING"
    Write-Output `
        "PCS_TELEMETRY_DOCUMENTDB_CONNSTRING=$DOCUMENTDB_CONNSTRING"
    Write-Output `
        "PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING=$DOCUMENTDB_CONNSTRING"
    Write-Output `
        "PCS_IOTHUBREACT_ACCESS_CONNSTRING=$IOTHUB_CONNSTRING"
    Write-Output `
        "PCS_IOTHUBREACT_HUB_NAME=$IOTHUB_NAME"
    Write-Output `
        "PCS_IOTHUBREACT_HUB_ENDPOINT=$IOTHUB_ENDPOINT"
    Write-Output `
        "PCS_IOTHUBREACT_HUB_CONSUMERGROUP=$IOTHUB_CONSUMERGROUP"
    Write-Output `
        "PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT=$BLOB_ACCOUNT"
    Write-Output `
        "PCS_IOTHUBREACT_AZUREBLOB_KEY=$BLOB_KEY"
    Write-Output `
        "PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX=$BLOB_ENDPOINT_SUFFIX"
    Write-Output `
        "PCS_ASA_DATA_AZUREBLOB_ACCOUNT=$BLOB_ACCOUNT"
    Write-Output `
        "PCS_ASA_DATA_AZUREBLOB_KEY=$BLOB_KEY"
    Write-Output `
        "PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX=$BLOB_ENDPOINT_SUFFIX"
    Write-Output `
        "PCS_EVENTHUB_CONNSTRING=$EVENTHUB_CONNSTRING"
    Write-Output `
        "PCS_EVENTHUB_NAME=$EVENTHUB_NAME"
    Write-Output `
        "PCS_SERVICEBUS_CONNSTRING=$SERVICEBUS_CONNSTRING"
    Write-Output `
        "PCS_SIGNALR_CONNSTRING=$SIGNALR_CONNSTRING"
    Write-Output `
        "PCS_KEYVAULT_URL=$KEYVAULT_URL"
    Write-Output `
        "PCS_WORKSPACE_NAME=$WORKSPACE_NAME"
    Write-Output `
        "PCS_APPINSIGHTS_NAME=$APPINSIGHTS_NAME"
    Write-Output `
        "PCS_APPINSIGHTS_INSTRUMENTATIONKEY=$APPINSIGHTS_INSTRUMENTATIONKEY"
    Write-Output `
        "PCS_SERVICE_URL=$AZURE_WEBSITE"

    if (!$aadConfig) {
    Write-Output `
        "PCS_AUTH_HTTPSREDIRECTPORT=0"
    Write-Output `
        "PCS_AUTH_REQUIRED=false"
    Write-Output `
        "REACT_APP_PCS_AUTH_REQUIRED=false"
        return;
    }

    $AUTH_AUDIENCE = $aadConfig.Audience
    $AUTH_AAD_APPID = $aadConfig.ClientId
    $AUTH_AAD_TENANT = $aadConfig.TenantId
    $AUTH_AAD_AUTHORITY = $aadConfig.Instance
    $AUTH_AAD_SERVICEID = $aadConfig.ServiceId
    $AUTH_AAD_SERVICESECRET = $aadConfig.ServiceSecret


    Write-Output `
        "PCS_AUTH_HTTPSREDIRECTPORT=0"
    Write-Output `
        "PCS_AUTH_REQUIRED=true"
    Write-Output `
        "PCS_AUTH_AUDIENCE=$AUTH_AUDIENCE"
    Write-Output `
        "PCS_AUTH_ISSUER=https://sts.windows.net/$AUTH_AAD_TENANT/"
    Write-Output `
        "PCS_WEBUI_AUTH_AAD_APPID=$AUTH_AAD_APPID"
    Write-Output `
        "PCS_WEBUI_AUTH_AAD_AUTHORITY=$AUTH_AAD_AUTHORITY"
    Write-Output `
        "PCS_WEBUI_AUTH_AAD_TENANT=$AUTH_AAD_TENANT"

    Write-Output `
        "REACT_APP_PCS_AUTH_REQUIRED=true"
    Write-Output `
        "REACT_APP_PCS_AUTH_AUDIENCE=$AUTH_AUDIENCE"
    Write-Output `
        "REACT_APP_PCS_AUTH_ISSUER=https://sts.windows.net/$AUTH_AAD_TENANT/"
    Write-Output `
        "REACT_APP_PCS_WEBUI_AUTH_AAD_APPID=$AUTH_AAD_APPID"
    Write-Output `
        "REACT_APP_PCS_WEBUI_AUTH_AAD_AUTHORITY=$AUTH_AAD_AUTHORITY"
    Write-Output `
        "REACT_APP_PCS_WEBUI_AUTH_AAD_TENANT=$AUTH_AAD_TENANT"
   Write-Output `
        "PCS_AUTH_AAD_SERVICEID=$AUTH_AAD_SERVICEID"
    Write-Output `
        "PCS_AUTH_AAD_SERVICESECRET=$AUTH_AAD_SERVICESECRET"
    Write-Output `
        "PCS_ADLSG2_ACCOUNT=$ADLSG2_ACCOUNT"
}

#******************************************************************************
# Generate a random password
#******************************************************************************
Function CreateRandomPassword() {
    param(
        $length = 15
    ) 
    $punc = 46..46
    $digits = 48..57
    $lcLetters = 65..90
    $ucLetters = 97..122

    $password = `
        [char](Get-Random -Count 1 -InputObject ($lcLetters)) + `
        [char](Get-Random -Count 1 -InputObject ($ucLetters)) + `
        [char](Get-Random -Count 1 -InputObject ($digits)) + `
        [char](Get-Random -Count 1 -InputObject ($punc))
    $password += get-random -Count ($length -4) `
        -InputObject ($punc + $digits + $lcLetters + $ucLetters) |`
         % -begin { $aa = $null } -process {$aa += [char]$_} -end {$aa}

    return $password
}

#******************************************************************************
# find the top most folder with solution in it
#******************************************************************************
Function GetRootFolder() {
    param(
        $startDir
    ) 
    $cur = $startDir
    while (![string]::IsNullOrEmpty($cur)) {
        if (Test-Path -Path (Join-Path $cur "Industrial-IoT.sln") -PathType Leaf) {
            return $cur
        }
        $cur = Split-Path $cur
    }
    return $startDir
}

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
  } catch [System.Net.WebException] {
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
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

# Register RPs
Register-AzureRmResourceProvider -ProviderNamespace "microsoft.web" | Out-Null
Register-AzureRmResourceProvider -ProviderNamespace "microsoft.compute" | Out-Null

# Set admin user and password
$adminPassword = CreateRandomPassword
$adminUser = "azureuser"
$templateParameters = @{ 
    adminPassword = $adminPassword
    adminUsername = $adminUser
    azureWebsiteName = $script:applicationName
}

# Try get branch name
$branchName = $env:BUILD_SOURCEBRANCH
if (![string]::IsNullOrEmpty($branchName)) {
    if ($branchName.StartsWith("refs/heads/")) {
        $branchName = $branchName.Replace("refs/heads/", "")
    }
    else {
        $branchName = $null
    }
}
if ([string]::IsNullOrEmpty($branchName)) {
    try {
        $argumentList = @("rev-parse", "--abbrev-ref", "HEAD")
        $branchName = (& "git" $argumentList 2>&1 | %{ "$_" });
    }
    catch {
        $branchName = $null
    }
}
if ([string]::IsNullOrEmpty($branchName) -or ($branchName -eq "HEAD")) {
    $branchName = "master"
}
Write-Host "VM deployment will use configuration from '$branchName' branch."
$templateParameters.Add("branchName", $branchName)

# Configure auth
if ($aadConfig) {
    if (![string]::IsNullOrEmpty($aadConfig.TenantId)) { 
        $templateParameters.Add("aadTenantId", $aadConfig.TenantId)
    }
    if (![string]::IsNullOrEmpty($aadConfig.Instance)) { 
        $templateParameters.Add("aadInstance", $aadConfig.Instance)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ServiceId)) { 
        $templateParameters.Add("aadServiceId", $aadConfig.ServiceId)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ServicePrincipalId)) { 
        $templateParameters.Add("aadServicePrincipalId", $aadConfig.ServicePrincipalId)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ServiceSecret)) { 
        $templateParameters.Add("aadServiceSecret", $aadConfig.ServiceSecret)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ClientId)) { 
        $templateParameters.Add("aadClientId", $aadConfig.ClientId)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ClientSecret)) { 
        $templateParameters.Add("aadClientSecret", $aadConfig.ClientSecret)
    }
    if (![string]::IsNullOrEmpty($aadConfig.Audience)) { 
        $templateParameters.Add("authAudience", $aadConfig.Audience)
    }
    if (![string]::IsNullOrEmpty($aadConfig.UserPrincipalId)) { 
        $templateParameters.Add("aadUserPrincipalId", $aadConfig.UserPrincipalId)
    }
}

if (![string]::IsNullOrEmpty($script:containerRegistryPrefix)) {
    $templateParameters.Add("containerRegistryPrefix", $script:containerRegistryPrefix)
}

# Create ssl cert 
$cert = New-SelfSignedCertificate -DnsName "opctwin.services.net" `
    -KeyExportPolicy Exportable -CertStoreLocation "Cert:\CurrentUser\My"
if ($cert) {
    $thumbprint = $cert.Thumbprint;
    if (![string]::IsNullOrEmpty($thumbprint)) { 
        $templateParameters.Add("remoteEndpointSSLThumbprint", $thumbprint)
    }
    $certificate = [Convert]::ToBase64String(`
        $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, `
        $adminPassword))
    if (![string]::IsNullOrEmpty($certificate)) { 
        $templateParameters.Add("remoteEndpointCertificate", $certificate)
    }
}
Write-Host "Creating simulated VM parameters.."
# Set simulated virtual machine user and password
$simVMPassword = CreateRandomPassword
$simVMUser = "edgeuser"
$templateParameters.Add("simVirtualMachineUsername", $simVMUser)
$templateParameters.Add("simVirtualMachinePassword", $simVMPassword)

Write-Host "Starting deployment..."
Write-Host 
Write-Host "To trouble shoot cloud-based resources vm, use the following User and Password to log onto your VM:"
Write-Host 
Write-Host $adminUser
Write-Host $adminPassword
Write-Host 
Write-Host "To trouble shoot simulated edge vm, use the following User and Password to log onto your VM:"
Write-Host 
Write-Host $simVMUser
Write-Host $simVMPassword

# Start the deployment
$templateFilePath = Join-Path $ScriptDir "template.json"
$deployment = New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName `
    -TemplateFile $templateFilePath -TemplateParameterObject $templateParameters

$website = $deployment.Outputs["azureWebsite"].Value
$iothub = $deployment.Outputs["iothub-connstring"].Value
$iothubName = $deployment.Outputs["iothub-name"].Value
$simVmName = $deployment.Outputs["simVirtualMachineName"].Value

# Create edge enabled iot device
$deviceId = "simEdgeDevice"
$device = CreateEdgeDevice -IoTHubConnectionString $iothub -DeviceId $deviceId
# Get shared key from the device
$device = CreateEdgeDevice -IoTHubConnectionString $iothub -DeviceId $deviceId
$device = $device | ConvertFrom-Json
$key = $device.authentication.symmetricKey.primaryKey
$edgeDeviceConnectionString = "HostName=${iothubName}.azure-devices.net;DeviceId=${deviceId};SharedAccessKey=${key}"
# Connect newly created simulation VM with newly created edge device in IotHub
$fileContent = "sudo /etc/iotedge/configedge.sh ""${edgeDeviceConnectionString}"""
$fileContent | Out-File -FilePath .\connect.sh
Write-Host 
Write-Host "Setting connection string in the simulation VM.."
Write-Host 
Invoke-AzureRmVMRunCommand -ResourceGroupName $resourceGroupName -VMName $simVmName -CommandId 'RunShellScript' -ScriptPath 'connect.sh'

# find the top most folder with docker-compose.yml in it
$rootDir = GetRootFolder $ScriptDir

$writeFile = $false
if ($script:interactive) {
    $ENVVARS = Join-Path $rootDir ".env"
    $prompt = "Save environment as $ENVVARS for local development? [y/n]"
    $reply = Read-Host -Prompt $prompt
    if ($reply -match "[yY]") {
        $writeFile = $true
    }
    if ($writeFile) {
        if (Test-Path $ENVVARS) {
            $prompt = "Overwrite existing .env file in $rootDir? [y/n]"
            if ( $reply -match "[yY]" ) {
                Remove-Item $ENVVARS -Force
            }
            else {
                $writeFile = $false
            }
        }
    }
}

if ($aadConfig -and $aadConfig.ClientObjectId) {
    # 
    # Update client application to add reply urls 
    #
    $replyUrls = New-Object System.Collections.Generic.List[System.String]
    $replyUrls.Add($website)
    $replyUrls.Add($website + "/twin/oauth2-redirect.html")
    $replyUrls.Add($website + "/registry/oauth2-redirect.html")
    $replyUrls.Add($website + "/history/oauth2-redirect.html")
    $replyUrls.Add($website + "/vault/oauth2-redirect.html")
    $replyUrls.Add($website + "/ua/oauth2-redirect.html")
    if ($writeFile) {
        $replyUrls.Add("http://localhost:3000")
        $replyUrls.Add("urn:ietf:wg:oauth:2.0:oob")
    }
    # still connected
    Set-AzureADApplication -ObjectId $aadConfig.ClientObjectId -ReplyUrls $replyUrls
}

if ($writeFile) {
    GetEnvironmentVariables $deployment | Out-File -Encoding ascii `
        -FilePath $ENVVARS

    Write-Host
    Write-Host ".env file created in $rootDir."
    Write-Host
    Write-Warning "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    Write-Warning "!The file contains security keys to your Azure resources!"
    Write-Warning "! Safeguard the contents of this file, or delete it now !"
    Write-Warning "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    Write-Host
}

Write-Host
Write-Host "Your services can be accessed at:"
Write-Host $website
Write-Host 
