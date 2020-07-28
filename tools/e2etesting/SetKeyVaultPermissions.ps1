Param(
    $KeyVaultUrl,
    $ResourceGroupName
)

$keyVaultName = $KeyVaultUrl.Split('.')[0].Split('/')[2]

$vm = Get-AzVm -Name $env:COMPUTERNAME
$objectId = $vm.Identity.PrincipalId

Write-Host "Using ObjectId '$($objectId)' ($($env:COMPUTERNAME)) for access permissions."

Write-Host "Adding List,Get-Permissions for secrets of vault '$($keyVaultName)' for user '$($upn)'"
Set-AzKeyVaultAccessPolicy -VaultName $keyVaultName -ResourceGroupName $ResourceGroupName -ObjectId $objectId -PermissionsToSecrets get,list

Write-Host "Setting output-variable 'KeyVaultName' to '$($keyVaultName)'"
Write-Host "##vso[task.setvariable variable=KeyVaultName;isOutput=true]$keyVaultName"