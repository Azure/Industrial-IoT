<#
 .SYNOPSIS
    Consumes tar balls and creates dockerfiles and build context

 .PARAMETER TarFileInput
    The tar files to use
 .PARAMETER OutputFolder
    The output folder to use
 .PARAMETER RegistryName
    The registry to add to the matrix
 .PARAMETER MatrixName
    The name of the matrix to produce
#>

Param(
    [string] $TarFileInput,
    [string] $OutputFolder,
    [string] $MatrixName = "matrix",
    [string] $RegistryName = "industrialiotdev"
)

$ErrorActionPreference = "Stop"
$index = 0
$matrix = @{}
Get-ChildItem -Path $TarFileInput -Filter '*.tar.gz' -Recurse | ForEach-Object {
    $tarfile = $_.FullName
    $name = $_.FullName.Replace($TarFileInput, "").Replace(".tar.gz", "")
    $name = $name.Replace("\", "-").Replace("/", "-").Trim("-")

    # extract the contents of the tar file into the output folder
    $index++
    $contextFolder = Join-Path $OutputFolder $index
    Write-Host "Extracting tar file $tarFile to $contextFolder..."
    New-Item -ItemType Directory -Path $contextFolder -Force | Out-Null
    . tar -xvf $tarFile -C $contextFolder
    if ($LastExitCode -ne 0) {
        . file $tarfile
        # try as zipped tar
        . tar -xvzf $tarFile -C $contextFolder
        if ($LastExitCode -ne 0) {
            throw "tar failed with $($LastExitCode)."
        }
    }

    # find the manifest file
    # Read manifest file content and convert to json
    $manifestFile = Join-Path $contextFolder "manifest.json"
    if (-not (Test-Path -Path $manifestFile)) {
        throw "Manifest file '$manifestFile' not found."
    }
    $manifest = Get-Content -Path $manifestFile | ConvertFrom-Json
    if ($manifest.Count -ne 1) {
        throw "Expected one item  in the manifest file, found $($manifest.Count)."
    }

    # Create a docker file from the manifest
    $dockerFile = "FROM scratch"
    $manifest.Layers | ForEach-Object { $dockerFile +="`nADD $_ /" }

    # Read configuration file content and convert to json
    $configurationFile = Join-Path $contextFolder $manifest[0].Config
    if (-not (Test-Path -Path $configurationFile)) {
        throw "Configuration file '$configurationFile' not found."
    }
    $config = Get-Content -Path $configurationFile | ConvertFrom-Json
    $configuration = $config.config
    $configuration.Labels.PSObject.Properties | ForEach-Object {
        $dockerFile += "`nLABEL `"$($_.Name)`"=`"$($_.Value)`""
    }
    $configuration.ExposedPorts.PSObject.Properties | ForEach-Object {
        $dockerFile += "`nEXPOSE $($_.Name)" 
    }
    $configuration.Env | ForEach-Object { $dockerFile += "`nENV $_" }
    if ($configuration.User) {
        $dockerFile += "`nUSER $($configuration.User)"
    }
    if ($configuration.WorkingDir) {
        $dockerFile += "`nWORKDIR $($configuration.WorkingDir)"
    }
    if ($configuration.EntryPoint.Count -gt 0) {
        $dockerFile += "`nENTRYPOINT $($configuration.EntryPoint | ConvertTo-Json -Compress)"
    }
    if ($configuration.Cmd.Count -gt 0) {
        $dockerFile += "`nCMD $($configuration.Cmd | ConvertTo-Json -Compress)"
    }
    $dockerFilePath = Join-Path $contextFolder "Dockerfile"
    $dockerFile | Out-File -FilePath $dockerFilePath

    $tagName = "latest"
    $repositoryName = "image"
    if ($manifest[0].RepoTags -gt 0) {
        $repoTag = $manifest[0].RepoTags[0].Split(":")
        $repositoryName = $repoTag[0]
        $tagName = $repoTag[1]
    }

    $matrix[$name] = @{
        'DisplayName' = $name
        'RepositoryName' = $repositoryName
        'BuildTag' = $tagName
        'BuildVersion' = $tagName.Split("-")[0]
        'RegistryName' = $script:RegistryName
        'DockerFile' = $dockerFilePath
        'DockerFileRel' = Join-Path "$($index)" "Dockerfile"
        'BuildContext' = $contextFolder
        'BuildContextRel' = "$($index)"
        'Os' = $config.os
        'Arch' = $config.architecture
    }
}

$matrixJson = $matrix | ConvertTo-Json
Write-Host "##vso[task.setVariable variable=$($script:MatrixName);isOutput=true]$matrixJson"