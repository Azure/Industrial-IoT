<#
 .SYNOPSIS
    Sets environment variables containing version numbers

 .DESCRIPTION
    The script is a wrapper around any versioning tool in the build.
#>

#
# find the top most folder with file in it and return the path
#
Function GetTopMostFolder() {
    param(
        [string] $startDir,
        [string] $fileName
    ) 
    $cur = $startDir
    while (![string]::IsNullOrEmpty($cur)) {
        if (Test-Path -Path (Join-Path $cur $fileName) -PathType Leaf) {
            return $cur
        }
        $cur = Split-Path $cur
    }
    return $startDir
}

try {
    $buildRoot = GetTopMostFolder -startDir $path `
        -fileName "version.props"
    # set version number from first encountered version.props
    [xml] $props=Get-Content -Path (Join-Path $buildRoot "version.props")
    $sourceTag="$($props.Project.PropertyGroup.VersionPrefix)".Trim()
}
catch {
    Write-Warning $_.Exception
    $sourceTag = $null
}

return $sourceTag