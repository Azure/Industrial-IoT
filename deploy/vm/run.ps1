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
Register-AzureRmResourceProvider -ProviderNamespace "microsoft.compute" | Out-Null

# Start the deployment
$templateFilePath = Join-Path $ScriptDir "template.json"
$deployment = New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile $templateFilePath
