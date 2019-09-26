<#
 .SYNOPSIS
    Sets environment variables containing version numbers

 .DESCRIPTION
    The script is a wrapper around any versioning tool in the build.
#>

try {
    $buildRoot = & ./getroot.ps1 -startDir $path `
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