<#
 .SYNOPSIS
    Deploys to Azure

 .DESCRIPTION
    Deploys the opc twin dependencies and optionally micro services and UI to Azure.

 .PARAMETER type
    The type of deployment (basic = vm, standard = aks, local)

 .PARAMETER resourceGroupName
    Can be the name of an existing or a new resource group.

 .PARAMETER subscriptionId
    Optional, the subscription id where resources will be deployed.

 .PARAMETER subscriptionName
    Or alternatively the subscription name.

 .PARAMETER resourceGroupLocation
    Optional, a resource group location. If specified, will try to create a new resource group in this location.

 .PARAMETER type
    Optional, the type of deployment - defaults to deploy basic
#>

param(
    [string]
    $type = "local",

    [string]
    $resourceGroupName,

    [string]
    $resourceGroupLocation,
 
    [string]
    $subscriptionName,
 
    [string]
    $subscriptionId,

    [string]
    $accountName,

    [ValidateSet("AzureCloud")] 
    [string]
    $environmentName = "AzureCloud"
)

$script:optionIndex = 0

#*******************************************************************************************************
# Validate environment names
#*******************************************************************************************************
Function SelectEnvironment() {
    switch ($script:environmentName) {
        "AzureCloud" {
            if ((Get-AzureRMEnvironment AzureCloud) -eq $null) {
                Add-AzureRMEnvironment –Name AzureCloud -EnableAdfsAuthentication $False `
                    -ActiveDirectoryServiceEndpointResourceId https://management.core.windows.net/  `
                    -GalleryUrl https://gallery.azure.com/ `
                    -ServiceManagementUrl https://management.core.windows.net/ `
                    -SqlDatabaseDnsSuffix .database.windows.net `
                    -StorageEndpointSuffix core.windows.net `
                    -ActiveDirectoryAuthority https://login.microsoftonline.com/ `
                    -GraphUrl https://graph.windows.net/ `
                    -trafficManagerDnsSuffix trafficmanager.net `
                    -AzureKeyVaultDnsSuffix vault.azure.net `
                    -AzureKeyVaultServiceEndpointResourceId https://vault.azure.net `
                    -ResourceManagerUrl https://management.azure.com/ `
                    -ManagementPortalUrl http://go.microsoft.com/fwlink/?LinkId=254433
            }
            $script:locations = @("West US", "North Europe", "West Europe")
        }
        default {
            throw ("'{0}' is not a supported Azure Cloud environment" -f $script:environmentName)
        }
    }
    $script:environment = Get-AzureRmEnvironment $script:environmentName
    $script:environmentName = $script:environment.Name
}

#*******************************************************************************************************
# Called in case no account is configured to let user choose the account.
#*******************************************************************************************************
Function SelectAccount() {
    if (![string]::IsNullOrEmpty($script:accountName)) {
        # Validate Azure account
        $account = Get-AzureAccount -Name $script:accountName
        if ($account -eq $null) {
            Write-Error ("Specified account {0} does not exist" -f $script:accountName)
        }
        else {
            if ([string]::IsNullOrEmpty($account.Subscriptions) -or  `
                    (Get-AzureSubscription -SubscriptionId `
                        ($account.Subscriptions -replace '(?:\r\n)',',').split(",")[0]) -eq $null) {
                Write-Warning ("No subscriptions in account {0}." -f $script:accountName)
                $account = $null
            }
        }
    }
    if ($account -eq $null) {
        $accounts = Get-AzureAccount
        if ($accounts -eq $null) {
            $account = Add-AzureAccount -Environment $script:environmentName
        }
        else {
            Write-Host "Select Azure account to use"
            $script:optionIndex = 1
            Write-Host
            Write-Host ("Available accounts in Azure environment '{0}':" -f $script:environmentName)
            Write-Host
            Write-Host (($accounts |  `
                Format-Table @{ `
                    Name = 'Option'; `
                    Expression = { `
                        $script:optionIndex; $script:optionIndex+=1 `
                    }; `
                    Alignment = 'right' `
                }, Id, Subscriptions -AutoSize) | Out-String).Trim()
            Write-Host (("{0}" -f $script:optionIndex).PadLeft(6) + " Use another account")
            Write-Host
            $account = $null
            while ($account -eq $null) {
                try {
                    [int]$script:optionIndex = Read-Host "Select an option"
                }
                catch {
                    Write-Host "Must be a number"
                    continue
                }
                if ($script:optionIndex -eq $accounts.length + 1) {
                    $account = Add-AzureAccount -Environment $script:environmentName
                    break
                }
                if ($script:optionIndex -lt 1 -or $script:optionIndex -gt $accounts.length) {
                    continue
                }
                $account = $accounts[$script:optionIndex - 1]
            }
        }
    }
    if ([string]::IsNullOrEmpty($account.Id)) {
        throw ("There was no account selected. Please check and try again.")
    }
    $script:accountName = $account.Id
}

#*******************************************************************************************************
# Perform login - uses profile file if exists
#*******************************************************************************************************
Function Login() {
    $rmProfileLoaded = $False
    if ([string]::IsNullOrEmpty($script:accountName)) {
        # Try to use saved profile if one is available
        $profileFile = Join-Path $script:ScriptDir ".user"
        if (Test-Path "$profileFile") {
            Write-Output ("Use saved profile from '{0}'" -f $profileFile)
            $rmProfile = Import-AzureRmContext -Path "$profileFile"
            $rmProfileLoaded = ($rmProfile -ne $null) `
                     -and ($rmProfile.Context -ne $null) `
                     -and ((Get-AzureRmSubscription) -ne $null)
        }
        if ($rmProfileLoaded) {
            $script:accountName = $rmProfile.Context.Account.Id;
        }
    }
    if (!$rmProfileLoaded) {
        # select account
        Write-Host "Logging in..."
        SelectAccount
        try {
            Login-AzureRmAccount -EnvironmentName $script:environmentName -ErrorAction Stop | Out-Null
        }
        catch {
            throw "The login to the Azure account was not successful."
        }
        Save-AzureRmContext -Path "$profileFile"
    }
}

#*******************************************************************************************************
# Select the subscription identifier
#*******************************************************************************************************
Function SelectSubscription() {
    $subscriptions = Get-AzureRMSubscription
    if ($script:subscriptionName -ne $null -and $script:subscriptionName -ne "") {
        $subscriptionId = Get-AzureRmSubscription -SubscriptionName $script:subscriptionName
    }
    else {
        $subscriptionId = $script:subscriptionId
    }

    if (![string]::IsNullOrEmpty($subscriptionId)) {
        if (!$subscriptions.Id.Contains($subscriptionId)) {
            Write-Error ("Invalid subscription id {0}" -f $subscriptionId)
            $subscriptionId = ""
        }
    }

    if ([string]::IsNullOrEmpty($subscriptionId)) {
        if ($subscriptions.Count -eq 1) {
            $subscriptionId = $subscriptions[0].Id
        }
        else {
            Write-Output "Select an Azure subscription to use... "
            $script:optionIndex = 1
            Write-Host
            Write-Host ("Available subscriptions for account '{0}'" -f $script:accountName)
            Write-Host
            Write-Host ($subscriptions | Format-Table @{ `
                Name='Option'; `
                Expression={ `
                    $script:optionIndex;$script:optionIndex+=1 `
                }; `
                Alignment='right' `
            },Name, Id -AutoSize | Out-String).Trim() 
            Write-Host
            while (!$subscriptions.Id.Contains($subscriptionId)) {
                try {
                    [int]$script:optionIndex = Read-Host "Select an option"
                }
                catch {
                    Write-Host "Must be a number!"
                    continue
                }
                if ($script:optionIndex -lt 1 -or $script:optionIndex -gt $subscriptions.length) {
                    continue
                }
                $subscriptionId = $subscriptions[$script:optionIndex - 1].Id
            }
        }
    }
    $script:subscriptionId = $subscriptionId
    Select-AzureRmSubscription -SubscriptionId $subscriptionId | Out-Null
    Write-Host "Azure subscription id '$subscriptionId' selected."
}

#*******************************************************************************************************
# Called if no Azure location is configured for the deployment to let the user choose a location.
#*******************************************************************************************************
Function SelectLocation() {
    $locations = @();
    $index = 1
    foreach ($location in $script:locations) {
        $newLocation = New-Object System.Object
        $newLocation | Add-Member -MemberType NoteProperty -Name "Option" -Value $index
        $newLocation | Add-Member -MemberType NoteProperty -Name "Location" -Value $location
        $locations += $newLocation
        $index += 1
    }
    Write-Host
    Write-Host ("Please choose a location in Azure environment '{0}':" -f $script:environmentName)
    $script:optionIndex = 1
    Write-Host ($locations | Format-Table @{ `
        Name='Option'; `
        Expression={ `
            $script:optionIndex;$script:optionIndex+=1 `
        }; `
        Alignment='right' `
    }, @{ `
        Name="Location"; `
        Expression={ `
            $_.Location `
        } `
    } -AutoSize | Out-String).Trim()
    Write-Host
    $location = ""
    while ($location -eq "" -or !(ValidateLocation $location)) {
        try {
            [int]$script:optionIndex = Read-Host "Select an option"
        }
        catch {
            Write-Host "Must be a number"
            continue
        }
        if ($script:optionIndex -lt 1 -or $script:optionIndex -ge $index) {
            continue
        }
        $location = $script:locations[$script:optionIndex - 1]
    }
    Write-Verbose "Azure location '$location' selected."
    $script:resourceGroupLocation = $location
}

#*******************************************************************************************************
# Validate a location
#*******************************************************************************************************
Function ValidateLocation() {
    Param (
        [string] 
        $locationToValidate
    )
    if (![string]::IsNullOrEmpty($locationToValidate)) {
        $locationToValidate = $locationToValidate.Replace(' ', '').ToLowerInvariant()
        foreach ($location in $script:locations) {
            if ($location.Replace(' ', '').ToLowerInvariant() -eq $locationToValidate) {
                return $True
            }
        }
        Write-Warning "Location '$locationToValidate' is not available."
    }
    return $false
}

#*******************************************************************************************************
# Get or create new resource group
#*******************************************************************************************************
Function GetOrCreateResourceGroup() {

    # Registering default resource providers
    Register-AzureRmResourceProvider -ProviderNamespace "microsoft.devices" | Out-Null
    Register-AzureRmResourceProvider -ProviderNamespace "microsoft.documentdb" | Out-Null
    Register-AzureRmResourceProvider -ProviderNamespace "microsoft.eventhub" | Out-Null
    Register-AzureRmResourceProvider -ProviderNamespace "microsoft.storage" | Out-Null

    while ([string]::IsNullOrEmpty($script:resourceGroupName)) {
        $script:resourceGroupName = Read-Host "Please provide a name for the resource group"
    }

    # Create or check for existing resource group
    $resourceGroup = Get-AzureRmResourceGroup -Name $script:resourceGroupName -ErrorAction SilentlyContinue
    if(!$resourceGroup) {
        Write-Host "Resource group '$script:resourceGroupName' does not exist."
        if(!(ValidateLocation $script:resourceGroupLocation)) {
            SelectLocation
        }
        Write-Host "Creating resource group '$script:resourceGroupName' in '$script:resourceGroupLocation'..."
        New-AzureRmResourceGroup -Name $script:resourceGroupName -Location $script:resourceGroupLocation | Out-Null
        Write-Host "Resource group '$script:resourceGroupName' created."
        return $True
    }
    else{
        Write-Host "Using existing resource group '$script:resourceGroupName'..."
        return $False
    }
}

#*******************************************************************************************************
# Script body
#*******************************************************************************************************
$ErrorActionPreference = "Stop"
$script:ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

$deploymentScript = Join-Path $script:ScriptDir $script:type
$deploymentScript = Join-Path $deploymentScript "run.ps1"
if(![System.IO.File]::Exists($deploymentScript)) {
    throw "Invalid deployment type '$type' specified."
}

SelectEnvironment
Login
SelectSubscription
$deleteOnError = GetOrCreateResourceGroup
try {
    Write-Host "Starting deployment..."
    & ($deploymentScript) -resourceGroupName $script:resourceGroupName
    Write-Host "Deployment succeeded."
}
catch {
    Write-Host "Deployment failed.  Removing resource group."
    $ex = $_.Exception
    if ($deleteOnError) {
        Try {
            Remove-AzureRmResourceGroup -Name $script:resourceGroupName -Force
        }
        Catch {
            Write-Host $_.Exception.Message
        }
    }
    throw $ex
}