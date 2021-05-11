<#
 .SYNOPSIS
    Gets all tag listings from mcr

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
    [string] $Registry = "industrialiotprod",
    [string] $Subscription = "IOT_GERMANY"
)

# set default subscription
if (![string]::IsNullOrEmpty($script:Subscription)) {
    Write-Debug "Setting subscription to $($script:Subscription)"
    $argumentList = @("account", "set", "--subscription", $script:Subscription, "-ojson")
    & "az" $argumentList 2`>`&1 | ForEach-Object { "$_" }
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }
}

# get list of repositories
$argumentList = @("acr", "repository", "list", "--name", $script:Registry, "-ojson")
$repositories = (& "az" $argumentList 2>&1 | ForEach-Object { "$_" }) | ConvertFrom-Json
$obsoleted = @()
foreach ($repository in $repositories) {
    $public = $repository.Replace("public/", "");
    $url = "https://mcr.microsoft.com/v2/$public/tags/list"
    $content = (Invoke-WebRequest $url).Content | ConvertFrom-Json

    # get actual tags
    $argumentList = @("acr", "repository", "show-tags", 
        "--name", $script:Registry,
        "--repository", $repository, 
        "-ojson")
    $tags = (& "az" $argumentList 2>&1 | ForEach-Object { "$_" }) | ConvertFrom-Json

    foreach ($tag in $content.tags) {
        if ($tags -contains $tag) {
            continue
        }
        if ($tag -eq "2.0") {
            # must keep tag to keep connected factory in business
            continue
        }
        $obsoleted += "mcr.microsoft.com/$($public):$($tag)"
    }
}
return $obsoleted
