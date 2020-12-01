Param(
    $ResourceGroupName,
    $AppServicePlanName,
    $WebAppName,
    $PackageDirectory,
    $StorageAccountName,
    $IoTHubName,
    $TenantId,
    $StorageContainerName
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

## Pre-Checks ##

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName is empty."
    return
}

if (!$PackageDirectory) {
    Write-Error "PackageDirectory not set."
    return
}

if (!(Test-Path $PackageDirectory -ErrorAction SilentlyContinue)) {
    Write-Error "Could not locate specified directory '$($PackageDirectory)'."
    return
}

$context = Get-AzContext

if (!$context) {
    Write-Host "Logging in..."
    Login-AzAccount -Tenant $TenantId
    $context = Get-AzContext
}

## Ensure Resource Group ##

$resourceGroup = Get-AzResourceGroup -Name $ResourceGroupName

if (!$resourceGroup) {
    Write-Host "ResourceGroup $($ResourceGroupName) does not exist, creating..."
    $resourceGroup = New-AzResourceGroup -Name $ResourceGroupName
}

$suffix = $resourceGroup.Tags["TestingResourcesSuffix"]

if ([String]::IsNullOrWhiteSpace($suffix)) {
    $suffix = (Get-Random -Minimum 10000 -Maximum 99999)

    $tags = $resourceGroup.Tags
    $tags+= @{"TestingResourcesSuffix"=$suffix}
    Set-AzResourceGroup -Name $resourceGroup.ResourceGroupName -Tag $tags | Out-Null
    $resourceGroup = Get-AzResourceGroup -Name $resourceGroup.ResourceGroupName
}

Write-Host "Using suffix $($suffix) for test-related resources."

if (!$AppServicePlanName) {
    $AppServicePlanName = "e2etesting-appserviceplan-" + $suffix
}

if (!$WebAppName) {
    $WebAppName = "e2etesting-TestEventProcessor-" + $suffix
}

if (!$StorageAccountName) {
    $StorageAccountName = "e2etestingstorage" + $suffix
}

if (!$StorageContainerName) {
    $StorageContainerName = "checkpoint";
}

## Check existence of Resources ##

if (!$IoTHubName) {
    $iothub = Get-AzIotHub -ResourceGroupName $ResourceGroupName
    if ($iotHub.Count -eq 1) {
        $IoTHubName = $iotHub.Name
    } else {
        Write-Error "More then 1 IoT Hub instances found in resource group $($ResourceGroupName). Please specify IoTHubName argument of this script."
        return
    }
} else {
    $iotHub = Get-AzIotHub -ResourceGroupName $ResourceGroupName -Name $IoTHubName -ErrorAction SilentlyContinue
}

if (!$iotHub) {
    Write-Error "Could not retrieve IoTHub '$($IoTHubName)' in Resource Group '$($ResourceGroupName)'. Please make sure that it exists."
    return
}

$keyVault = Get-AzKeyVault -ResourceGroupName $ResourceGroupName

if ($keyVault.Count -ne 1) {
    Write-Error "keyVault could not be automatically selected in Resource Group '$($ResourceGroupName)'."    
} 

Write-Host "Key Vault Name: $($keyVault.VaultName)"

## Log parameters ##

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
Write-Host "KeyVault: $($keyVault.VaultName)"
Write-Host "IoTHub: $($IoTHubName)"

## Ensure Storage Account (for Checkpoint Storage) ##

$storageAccount = Get-AzStorageAccount -ResourceGroupName $resourceGroup.ResourceGroupName -Name $StorageAccountName -ErrorAction SilentlyContinue

if (!$storageAccount) {
    Write-Host "Storage Account '$($storageAccountName)' does not exist, creating..."
    $storageAccount = New-AzStorageAccount -ResourceGroupName $resourceGroup.ResourceGroupName -Name $StorageAccountName -SkuName Standard_LRS -Location $resourceGroup.Location
}

$key = Get-AzStorageAccountKey -ResourceGroupName $resourceGroup.ResourceGroupName -Name $storageAccount.StorageAccountName

$storageAccountConnectionString = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net" -f $storageAccount.StorageAccountName, $key[0].Value

## Creating container for Checkpoint storage ##

$storageContext = $storageAccount.Context
New-AzStorageContainer -Name $StorageContainerName -Context $storageContext -Permission Container

## Ensure AppServicePlan ##

$appServicePlan = Get-AzAppServicePlan -ResourceGroupName $resourceGroup.ResourceGroupName -Name $AppServicePlanName -ErrorAction SilentlyContinue

if (!$appServicePlan) {
    Write-Host "AppServicePlan '$($AppServicePlanName)' does not exist, creating..."
    $appServicePlan = New-AzAppServicePlan -ResourceGroupName $resourceGroup.ResourceGroupName -Name $AppServicePlanName -Location $resourceGroup.Location -Tier Basic
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

$webApp = Set-AzWebApp -ResourceGroupName $ResourceGroupName -Name $WebAppName -AppSettings $creds -AlwaysOn $true

Write-Host "Restarting WebApp..."
$webApp = Restart-AzWebApp -ResourceGroupName $ResourceGroupName -Name $WebAppName

$baseUrl = "https://" + $webApp.DefaultHostName

## Get IoT Hub EventHub-compatible Endpoint ##

$iotHub = Get-AzIotHub -ResourceGroupName $ResourceGroupName -Name $IoTHubName
$iotHubkey = Get-AzIotHubKey -ResourceGroupName $ResourceGroupName -Name $IoTHubName -KeyName "iothubowner"
$ehEndpoint = $iotHub.Properties.EventHubEndpoints["events"].Endpoint

$ehConnectionString =  "Endpoint={0};SharedAccessKeyName={1};SharedAccessKey={2};EntityPath={3}" -f $ehEndpoint,$iotHubkey.KeyName,$iotHubkey.PrimaryKey,$iotHub.Name

## Set Secrets to KeyVault ##
Write-Host "Setting KeyVault Secret 'TestEventProcessorBaseUrl' to '$($baseUrl)'."
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name "TestEventProcessorBaseUrl" -SecretValue (ConvertTo-SecureString -String $baseUrl -AsPlainText -Force)

Write-Host "Setting KeyVault Secret 'CheckpointStorageAccountConnectionString' to '***'."
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name "CheckpointStorageAccountConnectionString" -SecretValue (ConvertTo-SecureString -String $storageAccountConnectionString -AsPlainText -Force)

Write-Host "SettingKeyVault Secret 'TestEventProcessorUsername' to '$($Username)'."
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name "TestEventProcessorUsername" -SecretValue (ConvertTo-SecureString -String $Username -AsPlainText -Force)

Write-Host "Setting KeyVault Secret 'TestEventProcessorPassword' to '***'."
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name "TestEventProcessorPassword" -SecretValue (ConvertTo-SecureString -String $Password -AsPlainText -Force)

Write-Host "Setting KeyVault Secret 'IoTHubEventHubConnectionString' to '***'."
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name "IoTHubEventHubConnectionString" -SecretValue (ConvertTo-SecureString -String $ehConnectionString -AsPlainText -Force)






