param(
    [string]
    $IAIBlobUrl,

    [string]
    $IAIVersion,

    [string]
    $IAILocalFolder
)

if (!$IAIBlobUrl) {
    Write-Host "##vso[task.complete result=Failed]IAIBlobUrl not set, exiting."
}

if (!$IAILocalFolder) {
    $IAILocalFolder = $PSScriptRoot
    Write-Host "##vso[task.logissue type=warning]IAILocalFolder not set, using $($IAILocalFolder)."
}

if (!$IAIVersion) {
    Write-Host "##vso[task.complete result=Failed]IAIVersion not set, exiting."
}

$iaiBinaryFilename = "Microsoft.Azure.IIoT.Deployment.exe"
$iaiUrlPostfix = "/win-x64/" + $iaiBinaryFilename

$iaiFullUrl = $IAIBlobUrl.TrimEnd('/') + "/" + $IAIVersion + "/" + $iaiUrlPostfix.TrimStart('/')
$iaiLocalFilename = [System.IO.Path]::Combine($IAILocalFolder, $iaiBinaryFilename)

Write-Host "##[group]Downloading IAI binaries..."
Write-Host "IAI binary download"
Write-Host "  Source: $($iaiFullUrl)"
Write-Host "  Target: $($iaiLocalFilename)"

Write-Host "##[command]Downloading binary..."
(New-Object System.Net.WebClient).DownloadFile($iaiFullUrl, $iaiLocalFilename)

Write-Host "Download complete"
Write-Host "##[endgroup]"

Write-Host "Settings Pipelines-Variable 'IAILocalFilename' to $($iaiLocalFilename)..."
Write-Host "##vso[task.setvariable variable=IAILocalFilename;]$($iaiLocalFilename)"