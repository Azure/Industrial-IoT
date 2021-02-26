param($KeyVaultName)

$secrets = Get-AzKeyVaultSecret -VaultName $KeyVaultName
$values = @{}

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

$values | ConvertTo-Json -Depth 10