<#
 .SYNOPSIS
    Get Environment file from Keyvault

 .DESCRIPTION
    Retrieves an environment file from keyvault

 .PARAMETER ResourceGroup
    The resource group from where to get the environment file

 .PARAMETER Subscription
    The subscription to use - otherwise uses default
#>

param(
    [Parameter(Mandatory=$True)] [string] $ResourceGroup,
    [string] $Subscription
)

# run az
Function DoAz() {
    param(
        [string[]] $argumentList
    )
    $output = & "az" ($argumentList) 2>&1 | ForEach-Object { "$_" }
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode): $($output)."
    }
    return $output
}

# Get subscriptions and if failed log in
$argumentList = @("account", "list", "--all")
$output = & "az" $argumentList 2>&1 | ForEach-Object { "$_" }
if ($LastExitCode -ne 0) {
    DoAz(@("login"))
    $subscriptions = DoAz($argumentList) | ConvertFrom-Json 
}
else {
    $subscriptions = $output | ConvertFrom-Json 
}

# set default subscription
if ([string]::IsNullOrEmpty($Subscription)) {
    # select subscription
    if ($subscriptions.Count -gt 1) {
        # todo allow select or skip
    }
    else {
        $Subscription = $subscriptions[0].id
    }
}

if (![string]::IsNullOrEmpty($Subscription)) {
    Write-Debug "Setting subscription to $($Subscription)"
    DoAz(@("account", "set", "--subscription", $Subscription))
}
# Get keyvault
$vaults = DoAz(@("keyvault", "list", "-g", $ResourceGroup)) | ConvertFrom-Json
if ($vaults.Count -ne 1) {
    return
}
$keyVault = $vaults[0].name
# get upn
$user = DoAz(@("ad", "signed-in-user", "show")) | ConvertFrom-Json 
# set access to keyvault
DoAz(@("keyvault", "set-policy", "--name", $keyVault, "--upn",  `
    $user.userPrincipalName, "--secret-permissions", "get", "-g", $ResourceGroup)) `
        | Out-Null



# read configuration object
$secret = DoAz(@("keyvault", "secret", "show", "--name", "configuration", `
    "--vault-name", $keyVault)) | ConvertFrom-Json 
$configuration = $secret.value | ConvertFrom-Json

# remove access from keyvault
DoAz(@("keyvault", "delete-policy", "--name", $keyVault, "--upn", `
    $user.userPrincipalName, "-g", $ResourceGroup)) | Out-Null

$configuration.PSObject.Properties | ForEach-Object {
    Write-Host "$($_.Name)=$($_.Value)"
}