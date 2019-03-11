<#
 .SYNOPSIS
    Deploys to Azure

 .DESCRIPTION
    Deploys an Azure Resource Manager template of choice

 .PARAMETER applicationName
    The name of the application. 

 .PARAMETER resourceGroupName
    The resource group where the template will be deployed. 

 .PARAMETER aadConfig
    The AAD configuration the template will be configured with.

 .PARAMETER interactive
    Whether to run in interactive mode
#>

param(
    [Parameter(Mandatory=$True)] [string] $applicationName,
    [Parameter(Mandatory=$True)] [string] $resourceGroupName,
    $aadConfig = $null,
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
    $EVENTHUB_CONNSTRING = $deployment.Outputs["eventhub-connstring"].Value
    $EVENTHUB_NAME = $deployment.Outputs["eventhub-name"].Value

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
}

#******************************************************************************
# find the top most folder with docker-compose.yml in it
#******************************************************************************
Function GetRootFolder() {
    param(
        $startDir
    ) 
    $cur = $startDir
    while ($True) {
        if ([string]::IsNullOrEmpty($cur)) {
            break
        }
        $test = Join-Path $cur "docker-compose.yml"
        if (Test-Path $test) {
            $found = $cur
        }
        elseif (![string]::IsNullOrEmpty($found)) {
            break
        }
        $cur = Split-Path $cur
    }
    if (![string]::IsNullOrEmpty($found)) {
        return $found
    }
    return $startDir
}

#******************************************************************************
# Script body
#******************************************************************************
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

$templateParameters = @{}
# Configure auth
if ($aadConfig) {
    if (![string]::IsNullOrEmpty($aadConfig.TenantId)) { 
        $templateParameters.Add("aadTenantId", $aadConfig.TenantId)
    }
}

# Start the deployment
$templateFilePath = Join-Path $ScriptDir "template.json"
Write-Host "Starting deployment..."
$deployment = New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName `
    -TemplateFile $templateFilePath -TemplateParameterObject $templateParameters

if ($aadConfig -and $aadConfig.ClientObjectId) {
    # 
    # Update client application to add reply urls 
    #
    $replyUrls = New-Object System.Collections.Generic.List[System.String]
    $replyUrls.Add("http://localhost/oauth2-redirect.html")
    $replyUrls.Add("https://localhost/oauth2-redirect.html")
    $replyUrls.Add("urn:ietf:wg:oauth:2.0:oob")
    # still connected
    Set-AzureADApplication -ObjectId $aadConfig.ClientObjectId -ReplyUrls $replyUrls
}

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
else {
    GetEnvironmentVariables $deployment | Out-Default
}