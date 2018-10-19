<#
 .SYNOPSIS
    Deploys to Azure

 .DESCRIPTION
    Deploys an Azure Resource Manager template of choice

 .PARAMETER resourceGroupName
    The resource group where the template will be deployed. Can be the name of an existing or a new resource group.
#>

param(
 [Parameter(Mandatory=$True)]
 [string]
 $resourceGroupName
)

#******************************************************************************
# Script body
#******************************************************************************
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

# find the parent folder with docker-compose.yml in it
$rootDir = $ScriptDir
while ($True) {
    if ([string]::IsNullOrEmpty($rootDir)) {
        $rootDir = $ScriptDir
        break
    }
    $test = Join-Path $rootDir "docker-compose.yml"
    if (Test-Path $test) {
        break
    }
    $rootDir = Split-Path $rootDir
}

# Start the deployment
$templateFilePath = Join-Path $ScriptDir "template.json"
$deployment = New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile $templateFilePath

$PCS_IOTHUBREACT_HUB_NAME = $deployment.Outputs["iothub-name"].Value
$PCS_IOTHUBREACT_HUB_ENDPOINT = $deployment.Outputs["iothub-endpoint"].Value
$PCS_IOTHUBREACT_HUB_CONSUMERGROUP = $deployment.Outputs["iothub-consumer-group"].Value
$PCS_IOTHUB_CONNSTRING = $deployment.Outputs["iothub-connstring"].Value
$PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT = $deployment.Outputs["azureblob-account"].Value
$PCS_IOTHUBREACT_AZUREBLOB_KEY = $deployment.Outputs["azureblob-key"].Value
$PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX = $deployment.Outputs["azureblob-endpoint-suffix"].Value
$PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING = $deployment.Outputs["docdb-connstring"].Value
# $PCS_AUTH_AUDIENCE = $deployment.Outputs["auth-audience"].Value
# $PCS_WEBUI_AUTH_TYPE = $deployment.Outputs["auth-type"].Value
# $PCS_WEBUI_AUTH_AAD_APPID = $deployment.Outputs["aad-appid"].Value
# $PCS_WEBUI_AUTH_AAD_TENANT = $deployment.Outputs["aad-tenant"].Value
# $PCS_WEBUI_AUTH_AAD_INSTANCE = $deployment.Outputs["aad-instance"].Value
# $PCS_APPLICATION_SECRET = $deployment.Outputs["aad-appsecret"].Value
$PCS_EVENTHUB_CONNSTRING = $deployment.Outputs["eventhub-connstring"].Value
$PCS_EVENTHUB_NAME = $deployment.Outputs["eventhub-name"].Value

$ENVVARS = Join-Path $rootDir ".env"
"PCS_IOTHUB_CONNSTRING=${PCS_IOTHUB_CONNSTRING}" >> ${ENVVARS}
"PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING=${PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING}" >> ${ENVVARS}
"PCS_TELEMETRY_DOCUMENTDB_CONNSTRING=${PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING}" >> ${ENVVARS}
"PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING=${PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING}" >> ${ENVVARS}
"PCS_IOTHUBREACT_ACCESS_CONNSTRING=${PCS_IOTHUB_CONNSTRING}" >> ${ENVVARS}
"PCS_IOTHUBREACT_HUB_NAME=${PCS_IOTHUBREACT_HUB_NAME}" >> ${ENVVARS}
"PCS_IOTHUBREACT_HUB_ENDPOINT=${PCS_IOTHUBREACT_HUB_ENDPOINT}" >> ${ENVVARS}
"PCS_IOTHUBREACT_HUB_CONSUMERGROUP=${PCS_IOTHUBREACT_HUB_CONSUMERGROUP}" >> ${ENVVARS}
"PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT=${PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT}" >> ${ENVVARS}
"PCS_IOTHUBREACT_AZUREBLOB_KEY=${PCS_IOTHUBREACT_AZUREBLOB_KEY}" >> ${ENVVARS}
"PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX=${PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX}" >> ${ENVVARS}
"PCS_ASA_DATA_AZUREBLOB_ACCOUNT=${PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT}" >> ${ENVVARS}
"PCS_ASA_DATA_AZUREBLOB_KEY=${PCS_IOTHUBREACT_AZUREBLOB_KEY}" >> ${ENVVARS}
"PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX=${PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX}" >> ${ENVVARS}
"PCS_EVENTHUB_CONNSTRING=${PCS_EVENTHUB_CONNSTRING}" >> ${ENVVARS}
"PCS_EVENTHUB_NAME=${PCS_EVENTHUB_NAME}" >> ${ENVVARS}
"PCS_AUTH_ISSUER=${PCS_AUTH_ISSUER}" > ${ENVVARS}
"PCS_AUTH_AUDIENCE=${PCS_AUTH_AUDIENCE}" >> ${ENVVARS}
"PCS_AUTH_AAD_GLOBAL_TENANTID=${PCS_AUTH_AAD_GLOBAL_TENANTID}" >> ${ENVVARS}
"PCS_AUTH_AAD_GLOBAL_CLIENTID=${PCS_AUTH_AAD_GLOBAL_CLIENTID}" >> ${ENVVARS}
"PCS_AUTH_AAD_GLOBAL_LOGINURI=${PCS_AUTH_AAD_GLOBAL_LOGINURI}" >> ${ENVVARS}
"PCS_APPLICATION_SECRET=${PCS_APPLICATION_SECRET}" >> ${ENVVARS}

Write-Host
Write-Host ".env file created in $rootDir."
Write-Host
Write-Warning "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
Write-Warning "!The file contains security keys to your Azure resources!"
Write-Warning "! Safeguard the contents of this file, or delete it now !"
Write-Warning "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
Write-Host
Write-Host "Ensure you have docker-compose installed. "
Write-Host "Then start the solution by running:"
Write-Host
Write-Host "cd $rootDir"
Write-Host "docker-compose up"
Write-Host
