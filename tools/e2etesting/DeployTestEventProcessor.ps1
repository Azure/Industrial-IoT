Param(
    $ResourceGroupName,
    $AppServicePlanName,
    $WebAppName,
    $PackageDirectory,
    $StorageAccountName
)

$suffix = (Get-Random -Minimum 10000 -Maximum 99999)

## Pre-Checks ##

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName is empty."
    return
}

if (!$AppServicePlanName) {
    $AppServicePlanName = "appserviceplan-" + $suffix
}

if (!$WebAppName) {
    $WebAppName = "TestEventProcessor-" + $suffix
}

if (!$PackageDirectory) {
    Write-Error "PackageDirectory not set."
    return
}

if (!(Test-Path $PackageDirectory -ErrorAction SilentlyContinue)) {
    Write-Error "Could not locate specified directory '$($PackageDirectory)'."
    return
}

if (!$StorageAccountName) {
    $StorageAccountName = "checkpointstorage" + $suffix
}

$context = Get-AzContext

if (!$context) {
    Write-Host "Logging in..."
    Login-AzAccount -Tenant "6e660ce4-d51a-4585-80c6-58035e212354"
    $context = Get-AzContext
}

Write-Host "Subscription Id: $($context.Subscription.Id)"
Write-Host "Subscription Name: $($context.Subscription.Name)"
Write-Host "Tenant Id: $($context.Tenant.Id)"
Write-Host
Write-Host "Resource Group: $($ResourceGroupName)"
Write-Host "Storage Account: $($StorageAccountName)"
Write-Host "AppService Plan Name: $($AppServicePlanName)"
Write-Host "WebApp Name: $($WebAppName)"
Write-Host "PackageDirectory: $($PackageDirectory)"
Write-Host

## Ensure Resource Group ##

$resourceGroup = Get-AzResourceGroup -Name $ResourceGroupName

if (!$resourceGroup) {
    Write-Host "ResourceGroup $($ResourceGroupName) does not exist, creating..."
    $resourceGroup = New-AzResourceGroup -Name $ResourceGroupName
}

## Ensure Storage Account (for Checkpoint Storage) ##

$storageAccount = Get-AzStorageAccount -ResourceGroupName $resourceGroup.ResourceGroupName -Name $StorageAccountName -ErrorAction SilentlyContinue

if (!$storageAccount) {
    Write-Host "Storage Account '$($storageAccountName)' does not exist, creating..."
    $storageAccount = New-AzStorageAccount -ResourceGroupName $resourceGroup.ResourceGroupName -Name $StorageAccountName -SkuName Standard_LRS -Location $resourceGroup.Location
}

$key = Get-AzStorageAccountKey -ResourceGroupName $resourceGroup.ResourceGroupName -Name $storageAccount.StorageAccountName

$storageAccountConnectionString = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net" -f $storageAccount.StorageAccountName, $key[0].Value

## Ensure AppServicePlan ##

$appServicePlan = Get-AzAppServicePlan -ResourceGroupName $resourceGroup.ResourceGroupName -Name $AppServicePlanName -ErrorAction SilentlyContinue

if (!$appServicePlan) {
    Write-Host "AppServicePlan '$($AppServicePlanName)' does not exist, creating..."
    $appServicePlan = New-AzAppServicePlan -ResourceGroupName $resourceGroup.ResourceGroupName -Name $AppServicePlanName -Location $resourceGroup.Location
}

## Ensure WebApp ##

$webApp = Get-AzWebApp -Name $WebAppName -ResourceGroupName $resourceGroup.ResourceGroupName -ErrorAction SilentlyContinue

if (!$webApp) {
    Write-Host "WebApp '$($WebAppName)' does not exist, creating..."
    $webApp = New-AzWebApp -Name $WebAppName -ResourceGroupName $resourceGroup.ResourceGroupName -AppServicePlan $AppServicePlanName
}

## Publish Archive ##

$temp = [System.IO.Path]::GetTempPath()
$temp = New-Item -ItemType Directory -Path (Join-Path $temp ([Guid]::NewGuid()))
$packageFilename = Join-Path $temp.FullName "package.zip"
Compress-Archive -Path ($PackageDirectory.TrimEnd("\\") + "\\*") -DestinationPath $packageFilename

Write-Host "Publishing Archive from '$($packageFilename)'to WebApp '$($WebAppName)'..."
$webApp = Publish-AzWebApp -ResourceGroupName $resourceGroup.ResourceGroupName -Name $WebAppName -ArchivePath $packageFilename -Force

Get-Item $temp.FullName | Remove-Item -Force -Recurse

## Set AppSettings ##

$Username = "ApiUser"
$Password = (-join ((65..90) + (97..122) + (33..38) + (40..47) + (48..57)| Get-Random -Count 20 | % {[char]$_}))

Write-Host "Applying authentication settings to WebApp for Basic Auth..."

$creds = @{
    AuthUsername = $Username;
    AuthPassword = $Password;
}

$webApp = Set-AzWebApp -ResourceGroupName $ResourceGroupName -Name $WebAppName -AppSettings $creds

Write-Host "Restarting WebApp..."
$webApp = Restart-AzWebApp -ResourceGroupName $ResourceGroupName -Name $WebAppName

$baseUrl = "https://" + $webApp.DefaultHostName

Write-Host "Setting variable 'WebAppName' to '$($WebAppName)'."
Write-Host "##vso[task.setvariable variable=WebAppName]$($WebAppName)"

Write-Host "Setting output variable 'TestEventProcessorBaseUrl' to '$($baseUrl)'."
Write-Host "##vso[task.setvariable variable=TestEventProcessorBaseUrl;isOutput=true]$($baseUrl)"

Write-Host "Setting output variable 'CheckpointStorageAccountConnectionString' to '***'."
Write-Host "##vso[task.setvariable variable=CheckpointStorageAccountConnectionString;isSecret=true;isOutput=true]$($storageAccountConnectionString)"

Write-Host "Setting output variable 'TestEventProcessorUsername' to '$($Username)'."
Write-Host "##vso[task.setvariable variable=TestEventProcessorUsername;isOutput=true]$($Username)"

Write-Host "Setting output variable 'TestEventProcessorPassword' to '***'."
Write-Host "##vso[task.setvariable variable=TestEventProcessorPassword;isSecret=true;isOutput=true]$($Password)"






