param($ResourceGroupName)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName is empty."
    return
}

$keyVaultVariableName = "KeyVaultName"

Write-Host "Looking for KeyVault in Resource group '$($ResourceGroupName)'"
$resourceGroup = Get-AzResourceGroup -Name $ResourceGroupName

if (!$resourceGroup) {
    Write-Host "##vso[task.complete result=Failed]Could not get Resource Group with name '$($ResourceGroupName)', exiting...'"
}

$keyVaults = Get-AzKeyVault -ResourceGroupName $resourceGroup.ResourceGroupName

if (!$keyVaults) {
    Write-Host "##vso[task.complete result=Failed]Could not find any KeyVault on Resource Group '$($ResourceGroupName)'."
}

if ($keyVaults.Count -eq 1) {
    $applicationKeyVault = $keyVaults.VaultName
}
else {
    $application = $resourceGroup.Tags["application"]

    if (!$application) {
        Write-Host "##vso[task.complete result=Failed]Application-Tag on Resource Group does not exist or is empty. Please make sure that the Resource Group contains a valid IIoT Deployment."
    }

    foreach ($keyVault in $keyVaults) {
        $kvApplication = $keyVault.Tags["application"]

        if ($kvApplication -ne $application) {
            continue
        } else {
            $applicationKeyVault = $keyVault.VaultName
            break
        }
    }
}

if (!$applicationKeyVault) {
    Write-Host "##vso[task.complete result=Failed]Could not locate KeyVault with Tag 'application = $($application)' in Resource Group '$($ResourceGroupName)'."
}

Write-Host "Setting variable '$($keyVaultVariableName)' to '$($applicationKeyVault)'."
Write-Host "##vso[task.setvariable variable=$($keyVaultVariableName)]$($applicationKeyVault)"