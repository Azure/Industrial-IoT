<#
 .SYNOPSIS
    Prunes all /internal repositories from the registry

 .DESCRIPTION
    The script requires az to be installed and already logged on to a 
    subscription.  This means it should be run in a azcliv2 task in the
    azure pipeline or "az login" must have been performed already.

 .PARAMETER registry
    The name of the registry

 .PARAMETER subscription
    The subscription to use - otherwise uses default
#>

# Check and set registry
if ([string]::IsNullOrEmpty($registry)) {
    $registry = $env.BUILD_REGISTRY
    if ([string]::IsNullOrEmpty($registry)) {
        $registry = "industrialiotdev"
        Write-Warning "No registry specified - using $($registry).azurecr.io."
    }
}

# set default subscription
if (![string]::IsNullOrEmpty($subscription)) {
    Write-Debug "Setting subscription to $($subscription)"
    $argumentList = @("account", "set", "--subscription", $subscription)
    & "az" $argumentList 2`>`&1 | %{ "$_" }
}

# get list of repositories
$argumentList = @("acr", "repository", "list", "--name", $registry)
$repositories = (& "az" $argumentList 2>&1 | %{ "$_" }) | ConvertFrom-Json
$repositories | ForEach-Object {
    $repository = $_
    
    if (!$repository.StartsWith("public/")) {
        Write-Warning "Deleting $($repository)"
        $argumentList = @("acr", "repository", "delete", 
            "--yes",
            "--name", $registry,
            "--repository", $repository
        )
        (& "az" $argumentList 2>&1 | %{ "$_" }) | Out-Host
    }
}

