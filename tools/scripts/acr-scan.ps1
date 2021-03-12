<#
 .SYNOPSIS
    Gets relevant scan results from ACR registry

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

# get registry resource info
$argumentList = @("acr", "show", "--name", $script:Registry, "-ojson")
$result = (& "az" $argumentList)
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$acr = $result | ConvertFrom-Json

# Get all vulnerability assessments
$argumentList = @("security", "sub-assessment", "list", "-ojson",
    "--assessed-resource-id", $acr.id,
    "--assessment-name", "dbd0cb49-b563-45e7-9724-889e799fa648"
)
$realVulnerabilities = @()
$vulnerabilities = (& "az" $argumentList) | ConvertFrom-Json
$defunct = @{ }
foreach ($vulnerability in $vulnerabilities) {
    $imageId = $vulnerability.resourceDetails.id
    if (!$imageId.StartsWith("/repositories/")) {
        continue
    }
    $imageId = $imageId.Replace("/repositories/", "").Replace("/images/", "@")
    if ($defunct.Contains($imageId)) {
        continue
    }
    # check resource still exists
    $argumentList = @("acr", "repository", "show", "-ojson",
        "--name", $script:Registry,
        "--image", $imageId
    )
    $result = (& "az" $argumentList 2>&1 | ForEach-Object { "$_" })
    if ($LastExitCode -ne 0) {
        if (!$result.StartsWith("ERROR: ResourceNotFoundError")) {
            Write-Error "$result"
        }
        $defunct.Add($imageId, "$imageId does not exist in $script:Registry...")
        continue
    }
    $realVulnerabilities += $vulnerability
}

Write-Warning "$($realVulnerabilities | ConvertTo-Json)"
return $realVulnerabilities

