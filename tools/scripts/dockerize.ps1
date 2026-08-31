<#
 .SYNOPSIS
    Consumes tar files creates dockerfiles and build contexts

 .DESCRIPTION
    Consumes tar files created by publish.ps1 and creates dockerfiles
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
    [string] $MatrixName = "matrix",
    [string[]] $PcapImages = @("opc-publisher")
)

$ErrorActionPreference = "Stop"

# Images named here get libpcap staged into them. The OPC Publisher MCP
# diagnostics tools capture traffic through SharpPcap, which P/Invokes
# libpcap and probes for libpcap.so, libpcap.so.0, libpcap.so.0.8 and
# libpcap.so.1. The .NET base images do not carry it, so without this the
# tools are present but fail at the first call.
#
# The library is staged from a throwaway builder rather than installed into
# the image: the runtime base is distroless, so it has no package manager
# and no shell to run one with. Only the shared object crosses over, which
# is the point - the shipped image gains the capability without gaining a
# package manager.
#
# Capture also needs CAP_NET_RAW at run time, which a container does not get
# by default. See docs/opc-publisher/mcp.md.
$pcapArchitectures = @{
    "amd64" = "linux/amd64"
    "arm64" = "linux/arm64"
}
$pcapBuilderImage = "mcr.microsoft.com/azurelinux/base/core:3.0"
$matrix = @{}
Get-ChildItem -Path $TarFileInput -Filter '*.tar.gz' -Recurse `
    | Group-Object { $_.Name }
    | ForEach-Object {

    $name = $_.Name.Replace(".tar.gz", "")
    $name = $name.Replace("\", "-").Replace("-", "_").Trim("_")
    $contextFolder = Join-Path $OutputFolder $name

    # Go through all difference setups of the tar file
    $index = 0
    $dockerFile = ""
    $platforms = @()
    $pcapStages = @{}
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

        # Stage libpcap into this architecture's image when the image asks for
        # it. Only the Azure Linux based architectures qualify: the 32 bit arm
        # image is Alpine, so it is musl based and a glibc shared object copied
        # into it would not load.
        $wantsPcap = $false
        if ($manifest[0].RepoTags.Count -gt 0) {
            $repoTagName = $manifest[0].RepoTags[0].Split(":")[0]
            $wantsPcap = $null -ne ($PcapImages | Where-Object { $repoTagName -like "*$_*" })
        }
        if ($wantsPcap -and $pcapArchitectures.ContainsKey($config.architecture)) {
            $pcapStages[$config.architecture] = $pcapArchitectures[$config.architecture]
            $dockerFile += "`nCOPY --from=pcap_$($config.architecture) /out/ /usr/lib/"
        }
    }

    $dockerFile += "`nFROM `${TARGETOS}_`${TARGETARCH}"
    $dockerFile += "`n"

    # Builder stages are emitted first so the COPY --from above resolves. Each
    # is pinned to its target platform, so the shared object matches the image
    # it is staged into rather than the machine doing the build. cp -P keeps the
    # soname symlink, and the unversioned name is added because SharpPcap probes
    # for plain libpcap.so first.
    $pcapPreamble = ""
    $pcapStages.GetEnumerator() | Sort-Object Key | ForEach-Object {
        $pcapPreamble += "`nFROM --platform=$($_.Value) $pcapBuilderImage AS pcap_$($_.Key)"
        $pcapPreamble += "`nRUN tdnf install -y libpcap && tdnf clean all"
        $pcapPreamble += " && mkdir -p /out && cp -P /usr/lib/libpcap.so* /out/"
        $pcapPreamble += " && ln -sf `$(ls -1 /out | grep -E '^libpcap\.so\.[0-9]+`$' | head -1) /out/libpcap.so"
        $pcapPreamble += "`n"
    }
    $dockerFile = $pcapPreamble + $dockerFile

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
# Also emit a GitHub Actions step output when running under GitHub Actions
# (additive; ADO behavior is unchanged).
if (![string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
    "$($script:MatrixName)=$matrixJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
}