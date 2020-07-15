Param(
    [Guid]
    $TenantId,
    [Guid]
    $SubscriptionId,
    [Guid]
    $ClientId,
    [string]
    $ClientSecret,
    [string]
    $ApplicationName,
    [string]
    $AppSettingsFilename,
    [string]
    $ResourceGroupName,
    [string]
    $Region
)

if (!$TenantId -or !$SubscriptionId) {
    Write-Host "Getting Azure Context..."
    $context = Get-AzContext

    if (!$SubscriptionId) {
        $SubscriptionId = $context.Subscription.Id
        Write-Host "Using Subscription Id $($subscriptionId)."
    }

    if (!$TenantId) {
        $TenantId = $context.Tenant.Id
        Write-Host "Using TenantId $($TenantId)."
    }
}

if (!$ClientId) {
    Write-Host "##vso[task.complete result=Failed]ClientId not set, exiting."
}

if (!$ClientSecret) {
    Write-Host "##vso[task.complete result=Failed]ClientSecret not set, exiting."
}

if (!$ApplicationName) {
    Write-Host "##vso[task.complete result=Failed]ApplicationName not set, exiting."
}

if (!$AppSettingsFilename) {
    Write-Host "##vso[task.complete result=Failed]AppSettingsFilename not set, exiting."
}

Write-Host "##[group]Parameter values"
Write-Host "TenantId: $($TenantId)"
Write-Host "SubscriptionId: $($SubscriptionId)"
Write-Host "ClientId: $($ClientId)"
Write-Host "ClientSecret: $($ClientSecret)"
Write-Host "ApplicationName: $($ApplicationName)"
Write-Host "ResourceGroupName: $($ResourceGroupName)"
Write-Host "Region: $($Region)"
Write-Host "AppSettingsFilename: $($AppSettingsFilename)"
Write-Host "##[endgroup]"

$fileContent = Get-Content $AppSettingsFilename -Raw

$fileContent = $fileContent -replace "{{TenantId}}", $TenantId
$fileContent = $fileContent -replace "{{SubscriptionId}}", $SubscriptionId
$fileContent = $fileContent -replace "{{ClientId}}", $ClientId
$fileContent = $fileContent -replace "{{ClientSecret}}", $ClientSecret
$fileContent = $fileContent -replace "{{ApplicationName}}", $ApplicationName
$fileContent = $fileContent -replace "{{ResourceGroupName}}", $ResourceGroupName
$fileContent = $fileContent -replace "{{Region}}", $Region

$fileContent | Out-File $AppSettingsFilename -Force -Encoding utf8