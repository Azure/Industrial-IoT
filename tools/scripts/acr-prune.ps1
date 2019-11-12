<#
 .SYNOPSIS
    Prunes all /internal repositories from the registry

 .DESCRIPTION
    The script requires az to be installed and already logged on to a 
    subscription.  This means it should be run in a azcliv2 task in the
    azure pipeline or "az login" must have been performed already.

 .PARAMETER Registry
    The name of the registry

 .PARAMETER Subscription
    The subscription to use - otherwise uses default
#>

Param(
    [string] $Registry = $null,
    [string] $Subscription = $null
)

# Check and set registry
if ([string]::IsNullOrEmpty($Registry)) {
    $Registry = $env.BUILD_REGISTRY
    if ([string]::IsNullOrEmpty($Registry)) {
        $Registry = "industrialiotdev"
        Write-Warning "No registry specified - using $($Registry).azurecr.io."
    }
}

# set default subscription
if (![string]::IsNullOrEmpty($Subscription)) {
    Write-Debug "Setting subscription to $($Subscription)"
    $argumentList = @("account", "set", "--subscription", $Subscription)
    & "az" $argumentList 2`>`&1 | %{ "$_" }
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }
}

# get list of repositories
$argumentList = @("acr", "repository", "list", "--name", $Registry)
$repositories = (& "az" $argumentList 2>&1 | %{ "$_" }) | ConvertFrom-Json
$repositories | ForEach-Object {
    $repository = $_
    
    if (!$repository.StartsWith("public/")) {
        Write-Warning "Deleting $($repository)"
        $argumentList = @("acr", "repository", "delete", 
            "--yes",
            "--name", $Registry,
            "--repository", $repository
        )
        (& "az" $argumentList 2>&1 | %{ "$_" }) | Out-Host
    }
}

