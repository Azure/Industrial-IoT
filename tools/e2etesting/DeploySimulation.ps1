Param(
    [string]
    $ResourceGroupName,
    [int]
    $NumberOfSimulations = 18,
    [Guid]
    $TenantId
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName not set."
}

$templateDir = [System.IO.Path]::Combine($PSScriptRoot, "../../deploy/templates") 

## Login if required

$context = Get-AzContext

if (!$context) {
    Write-Host "Logging in..."
    Login-AzAccount -Tenant $TenantId
    $context = Get-AzContext
}

## Check if resource group exists

$resourceGroup = Get-AzResourceGroup -Name $resourceGroupName

if (!$resourceGroup) {
    Write-Error "Could not find Resource Group '$($ResourceGroupName)'."
}

## Determine suffix for testing resources

$testSuffix = $resourceGroup.Tags["TestingResourcesSuffix"]

if (!$testSuffix) {
    $testSuffix = Get-Random -Minimum 10000 -Maximum 99999

    $tags = $resourceGroup.Tags
    $tags+= @{"TestingResourcesSuffix" = $testSuffix}
    Set-AzResourceGroup -Name $resourceGroup.ResourceGroupName -Tag $tags | Out-Null
    $resourceGroup = Get-AzResourceGroup -Name $resourceGroup.ResourceGroupName
}

Write-Host "Using suffix for testing resources: $($testSuffix)"

## Check if KeyVault exists
$keyVault = Get-AzKeyVault -ResourceGroupName $ResourceGroupName

if ($keyVault.Count -ne 1) {
    Write-Error "keyVault could not be automatically selected in Resource Group '$($ResourceGroupName)'."    
} 

Write-Host "Key Vault Name: $($keyVault.VaultName)"

## Deploy simulated PLCs
$prefix = "e2etesting-simulation"

$plcTemplateParameters = @{
    "numberOfSimulations" = $numberOfSimulations
    "numberOfSlowNodes" = 1000
    "slowNodeRate" = 10
    "slowNodeType" = "uint"
    "numberOfFastNodes" = 1000
    "fastNodeRate" = 1
    "fastNodeType" = "uint"
    "resourcesPrefix" = $prefix
    "resourcesSuffix" = $testSuffix
}

Write-Host "Running ARM-Deployment with e2e.plc.simulation.json"

$plcTemplate = [System.IO.Path]::Combine($TemplateDir, "e2e.plc.simulation.json")
$plcDeployment = New-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile $plcTemplate -TemplateParameterObject $plcTemplateParameters

if ($plcDeployment.ProvisioningState -ne "Succeeded") {
    Write-Error "Deployment $($plcDeployment.ProvisioningState)." -ErrorAction Stop
}

Write-Host "Getting IPs of ACIs for simulated PLCs..."

$containerInstances = Get-AzContainerGroup -ResourceGroupName $ResourceGroupName | ?{ $_.Name.StartsWith($prefix) }

$plcSimNames = ""
foreach ($ci in $containerInstances) {
    $plcSimNames += $ci.Fqdn + ";"
}

Write-Host "Adding/Updating KeVault-Secret 'plc-simulation-urls' with value '$($plcSimNames)'..."
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'plc-simulation-urls' -SecretValue (ConvertTo-SecureString $plcSimNames -AsPlainText -Force) | Out-Null

Write-Host "Deployment finished."

