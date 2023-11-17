<#
 .SYNOPSIS
    Set the namespace in the pipeline based on the branch name.
    Sets the "ImageNamespace" variable in the pipeline

 .DESCRIPTION
    The logic to disect the branch name into a namespace to
    structure the container registry.
#>

$branchName = $env:BUILD_SOURCEBRANCH
if (![string]::IsNullOrEmpty($branchName)) {
    if ($branchName.StartsWith("refs/heads/")) {
        $branchName = $branchName.Replace("refs/heads/", "")
    }
    else {
        Write-Warning "'$($branchName)' is not a branch."
        $branchName = $null
    }
}
if ([string]::IsNullOrEmpty($branchName)) {
    try {
        $argumentList = @("rev-parse", "--abbrev-ref", "HEAD")
        $branchName = (& "git" @argumentList 2>&1 | ForEach-Object { "$_" });
        if ($LastExitCode -ne 0) {
            throw "git $($argumentList) failed with $($LastExitCode)."
        }
    }
    catch {
        Write-Warning $_.Exception
        $branchName = $null
    }
}

if ([string]::IsNullOrEmpty($branchName) -or ($branchName -eq "HEAD")) {
    Write-Warning "Not building from a branch - skip image build."
    return
}

# Set namespace name based on branch name
$namespace = $branchName
if ($namespace.StartsWith("feature/")) {
    # dev feature builds
    $namespace = $namespace.Replace("feature/", "")
}
elseif ($namespace.StartsWith("release/") -or ($namespace -eq "releases")) {
    $namespace = "public"
    if ([string]::IsNullOrEmpty($registry)) {
        # Release and Preview builds go into staging
        $registry = "industrialiot"
    }
}
$namespace = $namespace.Replace("_", "/").Substring(0, [Math]::Min($namespace.Length, 24))
if ([string]::IsNullOrEmpty($registry)) {
    # Everything else into dev registry
    $registry = "industrialiotdev"
}

Write-Host "Setting ImageNamespace to '$($namespace)'..."
Write-Host "##vso[task.setvariable variable=ImageNamespace]$($namespace)"
Write-Host "Setting Registry to '$($registry)'..."
Write-Host "##vso[task.setvariable variable=ContainerRegistry]$($registry)"
return $namespace
