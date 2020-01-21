<#
 .SYNOPSIS
    Lists all containers in the tree

 .DESCRIPTION
    The script traverses the build root to find all container.json files
#>

Param(
)

$BuildRoot = & (Join-Path $PSScriptRoot "get-root.ps1") -fileName "*.sln"
# Traverse from build root and find all container.json metadata files and build
Get-ChildItem $BuildRoot -Recurse -Include "container.json" `
    | ForEach-Object {

    # Get root
    $folder = $_.DirectoryName.Replace($BuildRoot, "").Substring(1)
    $metadata = Get-Content -Raw -Path (join-path $_.DirectoryName "container.json") | ConvertFrom-Json
    return $metadata.name
}
