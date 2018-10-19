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

# Register RPs
Register-AzureRmResourceProvider -ProviderNamespace "microsoft.web" | Out-Null

# Convert docker compose file to base64 string
$dockerComposeFilePath = Join-Path $ScriptDir "docker-compose.yml"
$dockerComposeFileContent = [System.Convert]::ToBase64String([System.IO.File]::ReadAllBytes($dockerComposeFilePath))
Write-Host $dockerComposeFileContent

# Start the deployment
$templateFilePath = Join-Path $ScriptDir "template.json"
$templateParameters = @{ dockerComposeFileContent = $dockerComposeFileContent }
$deployment = New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile $templateFilePath -TemplateParameterObject $templateParameters
