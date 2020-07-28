Param(
    $KeyVaultUrl,
    $ResourceGroupName
)

$keyVaultName = $KeyVaultUrl.Split('.')[0].Split('/')[2]
$upn = (Get-AzContext).Account.Id

Write-Host "Adding List,Get-Permissions for secrets of vault '$($keyVaultName)' for user '$($upn)'"
Set-AzKeyVaultAccessPolicy -VaultName '$keyVaultName' -ResourceGroupName '$ResourceGroupName' -UserPrincipalName '$upn' -PermissionsToSecrets get,list

Write-Host "Setting output-variable 'KeyVaultName' to '$($keyVaultName)'"
Write-Host "##vso[task.setvariable variable=KeyVaultName;isOutput=true]$keyVaultName"