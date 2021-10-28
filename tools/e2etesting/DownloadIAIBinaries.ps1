param(
    [string]
    $IAIStorageAccountName,

    [string]
    $IAIStorageAccountContainerName,

    [string]
    $IAIVersion,

    [string]
    $IAILocalFolder
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$IAIStorageAccountName) {
    Write-Error "IAIStorageAccountName not set, exiting."
}

if (!$IAIStorageAccountContainerName) {
    Write-Error "IAIStorageAccountContainerName not set, exiting."
}

if (!$IAILocalFolder) {
    $IAILocalFolder = $PSScriptRoot
    Write-Host "IAILocalFolder not set, using $($IAILocalFolder)."
}

if (!$IAIVersion) {
    Write-Host "IAIVersion not set, using latest..."
    $IAIVersion = "latest"
}

$context = New-AzStorageContext -StorageAccountName $IAIStorageAccountName -Anonymous

if (!$context) {
    Write-Error "Could not retrieve storage context with name '$($IAIStorageAccountName), exiting.'"
}

$blobObjects = Get-AzStorageBlob -Container $IAIStorageAccountContainerName -Context $context

if (!$blobObjects) {
    Write-Error "Could not get blob contents in storage account '$($IAIStorageAccountName), container '$($IAIStorageAccountContainerName)', exiting.'"
}

$blobObjects = $blobObjects | ?{ $_.Name.StartsWith("main") -and $_.Name.EndsWith(".exe") }

if ($IAIVersion -eq "latest") {
    $blobObject = $blobObjects | sort -Descending Name | select -First 1
} else {
    $blobObject = $blobObjects | ?{ $_.Name.Contains($IAIVersion) } | select -First 1
}

$version = $blobObject.Name.Split("/") | select -Skip 1 -First 1

if (!$blobObject) {
    Write-Error "Could not find blob object with selected version '$($IAIVersion)', exiting..."
}

$baseUrl = $context.BlobEndpoint
$iaiFullUrl = $baseUrl.TrimEnd("/") + "/" + $IAIStorageAccountContainerName + "/" + $blobObject.Name.TrimStart("/")
$fileName = $blobObject.Name.Split("/") | select -Last 1

$iaiLocalFilename = [System.IO.Path]::Combine($IAILocalFolder, $fileName)

Write-Host "##[group]Downloading IAI binaries..."
Write-Host "IAI binary download"
Write-Host "  Version: $($version)"
Write-Host "  Source: $($iaiFullUrl)"
Write-Host "  Target: $($iaiLocalFilename)"

Write-Host "##[command]Downloading binary..."
(New-Object System.Net.WebClient).DownloadFile($iaiFullUrl, $iaiLocalFilename)

Write-Host "Download complete"
Write-Host "##[endgroup]"

Write-Host "Settings Pipelines-Variable 'IAILocalFilename' to $($iaiLocalFilename)..."
Write-Host "##vso[task.setvariable variable=IAILocalFilename]$($iaiLocalFilename)"