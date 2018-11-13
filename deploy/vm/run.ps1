<#
 .SYNOPSIS
    Deploys to Azure

 .DESCRIPTION
    Deploys an Azure Resource Manager template of choice

 .PARAMETER resourceGroupName
    The resource group where the template will be deployed. 
#>

param(
    [Parameter(Mandatory=$True)] [string] $resourceGroupName,
    $aadConfig
)

#******************************************************************************
# Script body
#******************************************************************************
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

# Register RPs
Register-AzureRmResourceProvider -ProviderNamespace "microsoft.web" | Out-Null
Register-AzureRmResourceProvider -ProviderNamespace "microsoft.compute" | Out-Null

# TODO
$remoteEndpointCertificate = $null
$remoteEndpointCertificateKey = $null

# Get current branch name
$branchName = "master"
try {
    $output = cmd /c "git rev-parse --abbrev-ref HEAD" 2`>`&1
    $branchName = ("{0}" -f $output);
}
catch {
    Write-Warning("Could not get current branch - using master branch.");
}
Write-Host ("VM deployment will use configuration from '{0}' branch" -f $branchName);

# Start the deployment
$templateFilePath = Join-Path $ScriptDir "template.json"
$templateParameters = @{ `
  #  remoteEndpointCertificate = $remoteEndpointCertificate; `
  #  remoteEndpointCertificateKey = $remoteEndpointCertificateKey; `
    branchName = $branchName; `
}
$deployment = New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName `
    -TemplateFile $templateFilePath -TemplateParameterObject $templateParameters

$website = $deployment.Outputs["azureWebsite"].Value
$iothub = $deployment.Outputs["iothub-connstring"].Value

Write-Host
Write-Host "In your webbrowser go to:"
Write-Host $website
Write-Host
Write-Host "To connect your own servers you might also need the iothubowner connection string:"
Write-Host ("PCS_IOTHUB_CONNSTRING=" + $iothub)
Write-Host
