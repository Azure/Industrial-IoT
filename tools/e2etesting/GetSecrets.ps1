param(
    [Parameter(Mandatory=$true)]
    [string] $KeyVaultName
)

$ErrorActionPreference = "Stop"

# Find the full path of the launchSettings.json file
$folderName = "e2e-tests"
$currentPath = (Get-Location).Path
$path = Get-ChildItem -Path $currentPath $folderName -Recurse
while (!$path) {
    $currentPath = $currentPath + '\..'
    $path = Get-ChildItem -Path $currentPath $folderName
}
$settingsFile = $path.FullName + "\.env"

$confirmation = Read-Host "Do you want to overwrite the $($settingsFile) file? yes/no"

$values = @{}

# Get the resource group name
$resourceGroup = (Get-AzResource -Name $KeyVaultName).ResourceGroupName
$values["ApplicationName"] = $resourceGroup

# Get the names of the secrets from the Key Vault
$secrets = Get-AzKeyVaultSecret -VaultName $KeyVaultName

# Get the values for the secrets
foreach ($secret in $secrets) {
    $secretValueSec = Get-AzKeyVaultSecret -VaultName $KeyVaultName -Name $secret.Name

    $ssPtr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secretValueSec.SecretValue)

    try {
        $secretValueText = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($ssPtr)
    } finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ssPtr)
    }
    $values[$secret.Name.ToUpperInvariant().Replace("-", "_")] = $secretValueText.Replace("`n", "\n").Replace("`r", "\r")
}

# Write the secrets to .env
$content = ""
foreach ($variable in $values.Keys) {
    $content += "$($variable)=$($values[$variable])`n"
}

if ($confirmation -eq "yes"){
    Set-Content -Path $settingsFile $content
    Write-Host "The file $($settingsFile) was successfully updated with the secrets from your key vault."
}
else {
    Write-Host $content
}