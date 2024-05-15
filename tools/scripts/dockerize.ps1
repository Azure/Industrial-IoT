<#
 .SYNOPSIS
    Consumes tar balls creates dockerfiles and build contexts

 .DESCRIPTION
    Consumes tar balls created by publish.ps1 and creates dockerfiles
    and build context to be consumed in matrix based traditional docker
    builds.

 .PARAMETER TarFileInput
    The tar files to use
 .PARAMETER OutputFolder
    The output folder to use
 .PARAMETER MatrixName
    The name of the matrix to produce
#>

Param(
    [string] $TarFileInput,
    [string] $OutputFolder,
    [string] $MatrixName = "matrix"
)

$ErrorActionPreference = "Stop"
$matrix = @{}
Get-ChildItem -Path $TarFileInput -Filter '*.tar.gz' -Recurse `
    | Group-Object { $_.Name }
    | ForEach-Object {

    $name = $_.Name.Replace(".tar.gz", "")
    $name = $name.Replace("\", "_").Replace("/", "_").Trim("_")
    $contextFolder = Join-Path $OutputFolder $name

    # Go through all difference setups of the tar file
    $index = 0
    $dockerFile = ""
    $platforms = @()
    $_.Group | ForEach-Object {
        $tarfile = $_.FullName

        # extract the contents of the tar file into the index folder
        $index++
        $tarFolder = Join-Path $contextFolder $index
        Write-Host "Extracting tar file $tarFile to $tarFolder..."
        New-Item -ItemType Directory -Path $tarFolder -Force | Out-Null
        . tar -xvf $tarFile -C $tarFolder
        if ($LastExitCode -ne 0) {
            . file $tarfile
            # try as zipped tar
            . tar -xvzf $tarFile -C $tarFolder
            if ($LastExitCode -ne 0) {
                throw "tar failed with $($LastExitCode)."
            }
        }

        # find the manifest file. read manifest file content and convert to json
        $manifestFile = Join-Path $tarFolder "manifest.json"
        if (-not (Test-Path -Path $manifestFile)) {
            throw "Manifest file '$manifestFile' not found."
        }
        $manifest = Get-Content -Path $manifestFile | ConvertFrom-Json
        if ($manifest.Count -ne 1) {
            throw "Expected one item  in the manifest file, found $($manifest.Count)."
        }

        # Read configuration file content and convert to json
        $configurationFile = Join-Path $tarFolder $manifest[0].Config
        if (-not (Test-Path -Path $configurationFile)) {
            throw "Configuration file '$configurationFile' not found."
        }
        $config = Get-Content -Path $configurationFile | ConvertFrom-Json

        # Each scratch is a target that gets built
        $dockerFile += "`nFROM scratch as $($config.os)_$($config.architecture)"
        $platform = "$($config.os)/$($config.architecture)"
        if ($config.variant) {
            $platform += "/$($config.variant)"
        }
        elseif ($platform -eq "linux/arm") {
            $platform += "/v7"
        }
        $platforms += $platform

        # Create a docker file from the manifest
        $manifest.Layers | ForEach-Object { $dockerFile +="`nADD $($index)/$_ /" }
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
    }

    $dockerFile += "`nFROM `${TARGETOS}_`${TARGETARCH}"
    $dockerFile += "`n"
    $dockerFilePath = Join-Path $contextFolder "Dockerfile"
    $dockerFile | Out-File -FilePath $dockerFilePath

    $tagName = "latest"
    $repositoryName = "image"
    if ($manifest[0].RepoTags -gt 0) {
        $repoTag = $manifest[0].RepoTags[0].Split(":")
        $repositoryName = $repoTag[0]
        $tagName = $repoTag[1].Split("-")[0]
    }

    $matrix[$name] = @{
        'RepositoryName' = $repositoryName
        'BuildTag' = $tagName
        'BuildContext' = $contextFolder
        'BuildContextRel' = $name
        'Platforms'= $($platforms -join ",")
    }
}
$matrix | ConvertTo-Json | Out-Host
$matrixJson = $matrix | ConvertTo-Json -Compress
Write-Host "##vso[task.setVariable variable=$($script:MatrixName);isOutput=true]$matrixJson"