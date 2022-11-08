param(
    [Parameter(Mandatory=$true)]
    [string] $KeyVaultName
)

$confirmation = Read-Host "Do you want to overwrite the IIoTPlatform-E2E-Tests\Properties\launchSettings.json file? yes/no"

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
    $values[$secret.Name.ToUpperInvariant().Replace("-", "_")] = $secretValueText
}

# Find the full path of the launchSettings.json file
$folderName = "e2e-tests"
$currentPath = (Get-Location).Path
$path = Get-ChildItem -Path $currentPath $folderName -Recurse
while (!$path) {
    $currentPath = $currentPath + '\..'
    $path = Get-ChildItem -Path $currentPath $folderName
}
$settingsFile = $path.FullName + "\IIoTPlatform-E2E-Tests\Properties\launchSettings.json"

# Write the secrets to launchSettings.json
$launchSettings = Get-Content $settingsFile  | ConvertFrom-Json
$launchSettings.profiles.'IIoTPlatform-E2E-Tests'.environmentVariables = [PSCustomObject]($values)
$json = ConvertTo-Json $launchSettings -Depth 4

if ($confirmation -eq "yes"){
    Set-Content -Path $settingsFile $json
    Write-Host "The file launchSettings.json is successfully updated with the secrets from your key vault."
}
else {
    Write-Host $json
}