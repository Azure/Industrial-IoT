<#
    .SYNOPSIS
        Deploys Azure services required for the Industrial IoT solution.
    .DESCRIPTION
        Deploys the Industrial IoT services and dependencies on
        Azure.
    .PARAMETER Name
        The name of the deployment.
    .PARAMETER ResourceGroup
        The name of the resource group to deploy to.
    .PARAMETER TenantId
        The Azure tenant ID to use for the deployment.
    .PARAMETER SubscriptionId
        The Azure subscription ID to use for the deployment.
    .PARAMETER Location
        The Azure region to deploy the resources to.
    .PARAMETER NoPrompt
        If specified, the script will not prompt for confirmation
        before saving the environment variables to a file.
#>

param(
    [string] [Parameter(Mandatory = $true)] $Name,
    [string] $ResourceGroup,
    [string] $TenantId,
    [string] $SubscriptionId,
    [string] $Location,
    [switch] $NoPrompt
)

$ErrorActionPreference = 'Continue'
$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Path

#
# Dump command line arguments
#
if ([string]::IsNullOrWhiteSpace($script:ResourceGroup)) {
    $script:ResourceGroup = $script:Name
}
Write-Host "Using resource group $($script:ResourceGroup)..." -ForegroundColor Cyan
if ([string]::IsNullOrWhiteSpace($script:TenantId)) {
    $script:TenantId = $env:AZURE_TENANT_ID
}
if (![string]::IsNullOrWhiteSpace($script:TenantId)) {
    Write-Host "Using tenant $($script:TenantId)..." -ForegroundColor Cyan
}
if ([string]::IsNullOrWhiteSpace($script:Location)) {
    $script:Location = "westus"
}
Write-Host "Using location $($script:Location)..." -ForegroundColor Cyan
if ([string]::IsNullOrWhiteSpace($script:OpsInstanceName)) {
    $script:OpsInstanceName = $script:Name
}

$errOut = $($azVersion = (az version)[1].Split(":")[1].Split('"')[1]) 2>&1
if ($azVersion -lt "2.74.0" -or !$azVersion) {
    Write-Host "Azure CLI version 2.74.0 or higher is required. $errOut" `
        -ForegroundColor Red
    exit -1
}
#
# Log into azure
#
Write-Host "Log into Azure..." -ForegroundColor Cyan
$loginParams = @( "--only-show-errors" )
if (![string]::IsNullOrWhiteSpace($script:TenantId)) {
    $loginParams += @("--tenant", $script:TenantId)
}
$session = (az login @loginParams) | ConvertFrom-Json
if (-not $session) {
    Write-Host "Error: Login failed." -ForegroundColor Red
    exit -1
}
if ([string]::IsNullOrWhiteSpace($SubscriptionId)) {
    $script:SubscriptionId = $session[0].id
}
if ([string]::IsNullOrWhiteSpace($TenantId)) {
    $script:TenantId = $session[0].tenantId
}

$errOut = $($rg = & { az group show `
    --name $script:ResourceGroup `
    --subscription $script:SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$rg) {
    Write-Host "Creating resource group $script:ResourceGroup..." -ForegroundColor Cyan
    $errOut = $($rg = & { az group create `
        --name $script:ResourceGroup `
        --location $script:Location `
        --subscription $script:SubscriptionId `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if (-not $? -or !$rg) {
        Write-Host "Error creating resource group - $($errOut)." -ForegroundColor Red
        exit -1
    }
    Write-Host "Resource group $($rg.id) created." -ForegroundColor Green
}
else {
    Write-Host "Resource group $($rg.id) exists." -ForegroundColor Green
}

Write-Host "Deploying resources to $($script:ResourceGroup)..." -ForegroundColor Cyan
$azureDeployFile = Join-Path $script:ScriptDir "azuredeploy.json"
& { az deployment group create `
    --name $script:Name `
    --resource-group $rg.Name `
    --template-file $azureDeployFile `
    --parameters "userPrincipalId=$(az ad signed-in-user show --query id -o tsv)" `
    --subscription $script:SubscriptionId `
    --only-show-errors --no-prompt --no-wait } | Out-Null
if ($?) {
    az deployment group wait --created --interval 3 `
        --name $script:Name `
        --resource-group $rg.Name `
        --subscription $script:SubscriptionId `
        --only-show-errors
}
if (-not $?) {
    Write-Host "Error deploying resources." -ForegroundColor Red
    exit -1
}
Write-Host "Deployment $($script:Name) completed." -ForegroundColor Green
$errOut = $($deployment = & { az deployment group show `
    --name $script:Name `
    --resource-group $rg.Name `
    --subscription $script:SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$deployment.properties.outputs) {
    $deployment | ConvertTo-Json -Depth 10 | Out-Host
    Write-Host "Deployment $($script:Name) not found - $($errOut)." -ForegroundColor Red
    exit -1
}
$rootDir = $scriptDir
while (![string]::IsNullOrEmpty($rootDir)) {
    if (Test-Path -Path (Join-Path $rootDir "Industrial-IoT.sln") -PathType Leaf) {
        break
    }
    $rootDir = Split-Path $rootDir
}
$writeFile = $true
$ENVVARS = Join-Path $rootDir ".env"
if (-not $script:NoPrompt.IsPresent) {
    $writeFile = $false
    $prompt = "Save environment as $ENVVARS for local development? [y/n]"
    $reply = Read-Host -Prompt $prompt
    if ($reply -match "[yY]") {
        $writeFile = $true
    }
}
if ($writeFile) {
    if (Test-Path $ENVVARS) {
        if (-not $script:NoPrompt.IsPresent) {
            $prompt = "Overwrite existing .env file in $rootDir? [y/n]"
            if ($reply -match "[yY]") {
                Remove-Item $ENVVARS -Force
            }
            else {
                $writeFile = $false
            }
        }
        else {
            Remove-Item $ENVVARS -Force
        }
    }
}

Function Get-EnvironmentVariables() { Param( $deployment )
    if (![string]::IsNullOrEmpty($script:ResourceGroup)) {
        Write-Output "PCS_RESOURCE_GROUP=$($script:ResourceGroup)"
    }
    if (![string]::IsNullOrEmpty($script:SubscriptionId)) {
        Write-Output "PCS_SUBSCRIPTION_ID=$($script:SubscriptionId)"
    }
    $var = $deployment.properties.outputs.tenantId.value
    if (![string]::IsNullOrEmpty($var)) {
        Write-Output "PCS_AUTH_TENANT=$($var)"
    }
    $var = $deployment.properties.outputs.iotHubConnString.value
    if (![string]::IsNullOrEmpty($var)) {
        Write-Output "PCS_IOTHUB_CONNSTRING=$($var)"
    }
    $var = $deployment.properties.outputs.dpsConnString.value
    if (![string]::IsNullOrEmpty($var)) {
        Write-Output "PCS_DPS_CONNSTRING=$($var)"
    }
    $var = $deployment.properties.outputs.dpsIdScope.value
    if (![string]::IsNullOrEmpty($var)) {
        Write-Output "PCS_DPS_IDSCOPE=$($var)"
    }
}

if ($writeFile) {
    Get-EnvironmentVariables $deployment | Out-File -Encoding ascii `
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
    Get-EnvironmentVariables $deployment | Out-Default
}
