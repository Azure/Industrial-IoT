<#
 .SYNOPSIS
    Sets CI version build variables and/or returns version information.

 .DESCRIPTION
    The script is a wrapper around any versioning tool we use and abstracts it from
    the rest of the build system.
#>

try {
    # Try install tool
    & dotnet @("tool", "install", "--tool-path", "./tools", "--framework", "netcoreapp3.1", "nbgv") 2>&1 

    $props = (& ./tools/nbgv  @("get-version", "-f", "json")) | ConvertFrom-Json
    if ($LastExitCode -ne 0) {
        throw "Error: 'nbgv get-version -f json' failed with $($LastExitCode)."
    }

    return [pscustomobject] @{ 
        Full = $props.CloudBuildAllVars.NBGV_NuGetPackageVersion
        Prefix = $props.CloudBuildAllVars.NBGV_SimpleVersion
        Prerelease = $props.CloudBuildAllVars.NBGV_PrereleaseVersion
    }
}
catch {
    Write-Warning $_.Exception
    return $null
}
