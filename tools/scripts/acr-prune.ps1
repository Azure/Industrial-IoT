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

 .PARAMETER All
    Delete all repositories

 .PARAMETER Yes
    Confirm action - otherwise it will be a dry run.
#>

Param(
    [Parameter(Mandatory = $true)] [string] $Registry = $null,
    [string] $Subscription = $null,
    [switch] $All,
    [switch] $Yes
)

# set default subscription
if (![string]::IsNullOrEmpty($script:Subscription)) {
    Write-Debug "Setting subscription to $($script:Subscription)"
    $argumentList = @("account", "set", "--subscription", $script:Subscription, "-ojson")
    & "az" @argumentList 2`>`&1 | ForEach-Object { "$_" }
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }
}

if (-not $script:All.IsPresent) {
    # get build registry credentials
    $argumentList = @("acr", "credential", "show", "--name", $script:Registry, "-ojson")
    $result = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" })
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }
    $dockerCredentials = $result | ConvertFrom-Json
    $dockerUser = $dockerCredentials.username
    $dockerPassword = $dockerCredentials.passwords[0].value
}

# get list of repositories
$argumentList = @("acr", "repository", "list", "--name", $script:Registry, "-ojson")
$repositories = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" }) | ConvertFrom-Json
foreach ($repository in $repositories) {

    if ($script:All.IsPresent) {
        Write-Warning "Deleting $($repository)..."
        $argumentList = @("acr", "repository", "delete", "--yes", "-ojson",
            "--name", $Registry,
            "--repository", $repository
        )
        if (-not $script:Yes.IsPresent) {
            "Would have deleted $($repository). Uses -Yes option."
        }
        else {
            (& "az" @argumentList 2>&1 | ForEach-Object { "$_" }) | Out-Host
        }
    }
    else {
        # use acr cli to purge dangling manifests per repo
        $argumentList = @("run", "-it", "mcr.microsoft.com/acr/acr-cli:latest", "purge"
            "--password", $dockerPassword,
            "--username", $dockerUser,
            "--registry", $script:Registry,
            "--filter", """$($repository):.*""",
            "--ago", "2y",
            "--untagged"
        )
        if (-not $script:Yes.IsPresent) {
            $argumentList += "--dry-run"
        }
        (& "docker" @argumentList 2>&1 | ForEach-Object { "$_" }) | Out-Host
    }
}

