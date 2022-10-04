<#
 .SYNOPSIS
    Returns relevant scan results for a ACR registry.

 .DESCRIPTION
    Returns relevant scan results for a ACR registry
    which can be further processed by another script
    or converted to json by piping it to ConvertTo-Json
    commandlet. The script requires az (AzureCLI) to 
    be installed and you must be logged in (az login)

 .PARAMETER Registry
    The name of the registry

 .PARAMETER Subscription
    The subscription to use - otherwise uses default.

 .PARAMETER All
    Include also vulnerabilities that are not patchable.
#>

Param(
    [string] $Registry = "industrialiotprod",
    [string] $Subscription = "IOT_GERMANY",
    [switch] $All
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

# get registry resource info
$argumentList = @("acr", "show", "--name", $script:Registry, "-ojson")
$result = (& "az" @argumentList)
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
$vulnerabilities = (& "az" @argumentList) | ConvertFrom-Json
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
    $result = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" })
    if ($LastExitCode -ne 0) {
        if (!$result.StartsWith("ERROR: ResourceNotFoundError")) {
            Write-Error "$result"
        }
        $defunct.Add($imageId, "$imageId does not exist in $script:Registry...")
        continue
    }

    $image = $result | ConvertFrom-Json
    if ($script:All.IsPresent -or $vulnerability.additionalData.patchable) {
        # get the tags linking to the image
        $imageParts = $imageId.Split('@')
        $repository = $imageParts[0]
        $digest = $imageParts[1]
        $argumentList = @("acr", "repository", "show-manifests", "-ojson", "--detail",
            "--name", $script:Registry,
            "--repository", $repository,
            "--query", """[?digest=='$($digest)'].tags"""
        )
        $tags = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" }) | ConvertTo-Json
        
        Add-Member -in $image -MemberType NoteProperty -name "tags" -value $tags
        Add-Member -in $vulnerability -MemberType NoteProperty -name "image" -value $image
        $realVulnerabilities += $vulnerability
    }
}

return $realVulnerabilities

