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
    [string] $type = "vm",
    [string] $resourceGroupName,
    [string] $resourceGroupLocation,
    [string] $subscriptionName,
    [string] $subscriptionId,
    [string] $accountName,
    [ValidateSet("AzureCloud")] [string] $environmentName = "AzureCloud",
    [parameter(ValueFromRemainingArguments=$true)] [String[]] $deploymentArgs
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
    $profileFile = Join-Path $script:ScriptDir ".user"
    if ([string]::IsNullOrEmpty($script:accountName)) {
        # Try to use saved profile if one is available
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
        $reply = Read-Host -Prompt "Save user profile in $profileFile? [y/n]"
        if ( $reply -match "[yY]" ) { 
            Save-AzureRmContext -Path "$profileFile"
        }
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
    Write-Verbose "Azure subscriptionId '$subscriptionId' selected."
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
# Adds the requiredAccesses (expressed as a pipe separated string) to the requiredAccess structure
#*******************************************************************************************************
Function AddResourcePermission($requiredAccess, $exposedPermissions, [string]$requiredAccesses, `
                               [string]$permissionType) {
    foreach($permission in $requiredAccesses.Trim().Split("|")) {
        foreach($exposedPermission in $exposedPermissions) {
            if ($exposedPermission.Value -eq $permission) {
                $resourceAccess = New-Object Microsoft.Open.AzureAD.Model.ResourceAccess
                $resourceAccess.Type = $permissionType # Scope = Delegated permissions | Role = Application permissions
                $resourceAccess.Id = $exposedPermission.Id # Read directory data
                $requiredAccess.ResourceAccess.Add($resourceAccess)
             }
        }
    }
}

#*******************************************************************************************************
# Called when no configuration for the AAD tenant to use was found to let the user choose one.
#*******************************************************************************************************
Function SelectAzureADTenantId2() {
    $tenants = Get-AzureRmTenant
    if ($tenants.Count -eq 0) {
        throw "No Active Directory domains found for '$($script:AzureAccountName)'";
    }

    if ($tenants.Count -eq 1) {
        # Select single one
        $tenantId = $tenants[0].Id
    }
    else {
        Write-Host "Select AD tenant to use..."
        Write-Host
        Write-Host "Available AAD tenants in Azure environment '$($script:environmentName)':"
        Write-Host
        Write-Host (($tenants |  `
        Format-Table @{ `
            Name = 'Option'; `
            Expression = { `
                $script:optionIndex; $script:optionIndex+=1 `
            }; `
            Alignment = 'right' `
        }, Id, Directory -AutoSize) | Out-String).Trim()
        while ($tenantId -eq $null) {
            try {
                [int]$script:optionIndex = Read-Host "Select an option"
            }
            catch {
                Write-Host "Must be a number"
                continue
            }
            if ($script:optionIndex -lt 1 -or $script:optionIndex -gt $tenants.length) {
                continue
            }
            $tenantId = $tenants[$script:optionIndex - 1].TenantId
        }
    }
    return $tenantId
}

#*******************************************************************************************************
# Acquire bearer token for user
#*******************************************************************************************************
Function AcquireToken() {
    Param (
        [string] $tenant,
        [string] $authUri,
        [string] $resourceUri,
        [Parameter(Mandatory=$false)] [string] $user = $null,
        [Parameter(Mandatory=$false)] [string] $prompt = "Auto"
    )
    $psAadClientId = "1950a258-227b-4e31-a9cf-717495945fc2"
    [Uri]$aadRedirectUri = "urn:ietf:wg:oauth:2.0:oob"
    $authority = "{0}{1}" -f $authUri, $tenant
    $authContext = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext" `
         -ArgumentList $authority,$true
    $userId = [Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifier]::AnyUser
    if (![string]::IsNullOrEmpty($user)) {
        $userId = new-object Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifier  `
            -ArgumentList $user, "OptionalDisplayableId"
    }
    $authResult = $authContext.AcquireToken($resourceUri, $psAadClientId, `
        $aadRedirectUri, $prompt, $userId)
    return $authResult
}


#*******************************************************************************************************
# Select azure ad tenant available to user
#*******************************************************************************************************
Function SelectAzureADTenantId() {
    $tenants = Get-AzureRmTenant
    if ($tenants.Count -eq 0) {
        Write-Error ("No Active Directory domains found for '{0}'" -f $script:accountName)
        throw ("No Active Directory domains found for '{0}'" -f $script:accountName)
    }
    if ($tenants.Count -eq 1) {
        $tenantId = $tenants[0].Id
    }
    else {
        # List Active directories associated with account
        $directories = @()
        $index = 1
        [int]$selectedIndex = -1
        foreach ($tenantObj in $tenants) {
            $tenant = $tenantObj.Id
            $uri = "{0}{1}/me?api-version=1.6" -f $script:environment.GraphUrl, $tenant
            $token = AcquireToken $tenant $script:environment.ActiveDirectoryAuthority `
                $script:environment.GraphUrl $script:accountName "Auto"
            $result = Invoke-RestMethod -Method "GET" -Uri $uri -Headers @{ `
                "Authorization"=$($token.CreateAuthorizationHeader()); `
                "Content-Type"="application/json" `
            }
            $directory = New-Object System.Object
            $directory | Add-Member -MemberType NoteProperty `
                -Name "Option" -Value $index
            $directory | Add-Member -MemberType NoteProperty `
                -Name "Directory Name" -Value ($result.userPrincipalName.Split('@')[1])
            $directory | Add-Member -MemberType NoteProperty `
                -Name "Tenant Id" -Value $tenant
            $directories += $directory
            $index += 1
        }
        if ($selectedIndex -eq -1) {
            Write-Host "Select an Active Directories to use"
            Write-Host
            Write-Host "Available Active Directories:"
            Write-Host
            Write-Host ($directories | Out-String) -NoNewline
            while ($selectedIndex -lt 1 -or $selectedIndex -ge $index) {
                try {
                    [int]$selectedIndex = Read-Host "Select an option"
                }
                catch {
                    Write-Host "Must be a number"
                }
            }
        }
        $tenantId = $tenants[$selectedIndex - 1].Id
    }
    return $tenantId
}

#*******************************************************************************************************
# Login to Azure AD (interactive if credentials are not already provided.
#*******************************************************************************************************
Function GetAzureADTenantId() {
    if (!$Credential) {
        if (!$tenantId) {
            $tenantId = SelectAzureADTenantId
        }
        # Interactive logon
        $creds = Connect-AzureAD -TenantId $tenantId
    }
    else {
        if (!$tenantId) {
            # use home tenant
            $creds = Connect-AzureAD -Credential $Credential
        }
        else {
            $creds = Connect-AzureAD -TenantId $tenantId -Credential $Credential
        }
    }
    if (!$tenantId) {
        $tenantId = $creds.Tenant.Id
    }
    return $tenantId
}

#*******************************************************************************************************
# Example: GetRequiredPermissions "Microsoft Graph" "Graph.Read|User.Read"
#*******************************************************************************************************
Function GetRequiredPermissions() {
    Param(
        [string] $applicationDisplayName,
        [string] $requiredDelegatedPermissions, 
        [string] $requiredApplicationPermissions, 
        $servicePrincipal
    )

    # If we are passed the service principal we use it directly, otherwise we find it from 
    # the display name (which might not be unique)
    if ($servicePrincipal) {
        $sp = $servicePrincipal
    }
    else {
        $sp = Get-AzureADServicePrincipal -Filter "DisplayName eq '$applicationDisplayName'"
    }

    $appid = $sp.AppId
    $requiredAccess = New-Object Microsoft.Open.AzureAD.Model.RequiredResourceAccess
    $requiredAccess.ResourceAppId = $appid 
    $requiredAccess.ResourceAccess =  
        New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.ResourceAccess]

    if ($requiredDelegatedPermissions) {
        AddResourcePermission $requiredAccess -exposedPermissions $sp.Oauth2Permissions `
            -requiredAccesses $requiredDelegatedPermissions -permissionType "Scope"
    }
    if ($requiredApplicationPermissions) {
        AddResourcePermission $requiredAccess -exposedPermissions $sp.AppRoles `
            -requiredAccesses $requiredApplicationPermissions -permissionType "Role"
    }
    return $requiredAccess
}

#*******************************************************************************************************
# Delete Azure AD applications for service and clients
#*******************************************************************************************************
Function DeleteAzureADApplications() {
    $appFile = Join-Path $script:ScriptDir ".app"
    if (Test-Path $appFile) {
        $config = Get-Content -Raw -Path $appFile | ConvertFrom-Json
        # Removes all applications
        Write-Host "Cleaning-up applications from tenant '$($config.TenantName)'..."
        $app=Get-AzureADApplication -Filter "AppId eq '$($config.AppId)'"  
        if ($app) {
            Remove-AzureADApplication -ObjectId $app.ObjectId
            Write-Host "Removed."
        }
        $app=Get-AzureADApplication -Filter "AppId eq '$($config.ClientId)'"  
        if ($app) {
            Remove-AzureADApplication -ObjectId $app.ObjectId
            Write-Host "Removed."
        }
        Remove-Item -Path $appFile -Force
    }
}

#*******************************************************************************************************
# Create Azure AD applications for service and clients
#*******************************************************************************************************
Function CreateAzureADApplications() {
    try {
        $tenantId = GetAzureADTenantId
    }
    catch {
        Write-Error $_.Exception.Message

        Write-Host
        Write-Host "Ensure you have installed the AzureAD cmdlets:" 
        Write-Host "1) Run Powershell as an administrator" 
        Write-Host "2) in the PowerShell window, type: Install-Module AzureAD" 
        Write-Host
        $reply = Read-Host -Prompt "Continue without authentication? [y/n]"
        if ( $reply -match "[yY]" ) { 
            return;
        }
        throw $_.Exception;
    }

    # Delete any existing application for force update
    DeleteAzureADApplication

    $tenant = Get-AzureADTenantDetail
    $tenantName =  ($tenant.VerifiedDomains | Where { $_._Default -eq $True }).Name
    Write-Host ("Using Tenant {0}..." -f $tenantName)

    Write-Host "Creating AAD service application..."
    $serviceAadApplication = New-AzureADApplication -DisplayName "opc-twin-services" `
        -HomePage "https://localhost:44324" -IdentifierUris "https://$tenantName/opc-twin-services" `
        -PublicClient $False
    $currentAppId = $serviceAadApplication.AppId
    $serviceServicePrincipal = New-AzureADServicePrincipal -AppId $currentAppId `
        -Tags {WindowsAzureActiveDirectoryIntegratedApp}

    # add current user as app owner
    $user = Get-AzureADUser -ObjectId $creds.Account.Id
    Add-AzureADApplicationOwner -ObjectId $serviceAadApplication.ObjectId -RefObjectId $user.ObjectId
    Write-Host "'$($user.UserPrincipalName)' added as a owner for '$($serviceAadApplication.DisplayName)'"
    Write-Host "... Done."

    Write-Host "Creating AAD client application..."
    $clientAadApplication = New-AzureADApplication -DisplayName "opc-twin-client" `
        -ReplyUrls "https://opctwin" -PublicClient $True

    $currentAppId = $clientAadApplication.AppId
    $clientServicePrincipal = New-AzureADServicePrincipal -AppId $currentAppId `
        -Tags {WindowsAzureActiveDirectoryIntegratedApp}

    # add current user as app owner
    Add-AzureADApplicationOwner -ObjectId $clientAadApplication.ObjectId -RefObjectId $user.ObjectId
    Write-Host "'$($user.UserPrincipalName)' added as owner for '$($clientServicePrincipal.DisplayName)'"
    Write-Host "... Done."

    #
    # Register client with service
    #
    $requiredResourcesAccess = `
        New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.RequiredResourceAccess]
    Write-Host "Add Required Resources Access from 'client' to 'service'..."
    $requiredPermissions = GetRequiredPermissions -applicationDisplayName "opc-twin-services" `
        -requiredDelegatedPermissions "user_impersonation";
    $requiredResourcesAccess.Add($requiredPermissions)
    Set-AzureADApplication -ObjectId $clientAadApplication.ObjectId `
        -RequiredResourceAccess $requiredResourcesAccess
    Write-Host "... Done."
    Write-Host "Configure known client applications for the 'service'..."
    $knowApplications = New-Object System.Collections.Generic.List[System.String]
    $knowApplications.Add($clientAadApplication.AppId)
    Set-AzureADApplication -ObjectId $serviceAadApplication.ObjectId `
        -KnownClientApplications $knowApplications
    Write-Host "... Done."

    Write-Host "Write configuration to .app file..."
    $config = [pscustomobject]@{ 
        TenantId = $tenantId
        TenantName = $tenantName
        
        AppId = $serviceAadApplication.AppId
        AppName = $serviceAadApplication.DisplayName
        ResourceId = $serviceAadApplication.IdentifierUris 

        ClientName = $clientAadApplication.DisplayName 
        ClientId = $clientAadApplication.AppId 
        ClientUri = $clientAadApplication.ReplyUrls
    } 
    $appFile = Join-Path $script:ScriptDir ".app"
    ($config | ConvertTo-Json -Compress) | Out-File $appFile 
    Write-Host "... Done."

    return $config
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
        Write-Host
        $script:resourceGroupName = Read-Host "Please provide a name for the resource group"
    }

    # Create or check for existing resource group
    Select-AzureRmSubscription -SubscriptionId $script:subscriptionId -Force | Out-Host
    $resourceGroup = Get-AzureRmResourceGroup -Name $script:resourceGroupName `
        -ErrorAction SilentlyContinue
    if(!$resourceGroup) {
        Write-Host "Resource group '$script:resourceGroupName' does not exist."
        if(!(ValidateLocation $script:resourceGroupLocation)) {
            SelectLocation
        }
        New-AzureRmResourceGroup -Name $script:resourceGroupName `
            -Location $script:resourceGroupLocation | Out-Host
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

# Get aad configuration
$deleteApp = $false
if (Test-Path ".app") {
    $appFile = Join-Path $script:ScriptDir ".app"
    $aadConfig = Get-Content -Raw -Path $appFile  | ConvertFrom-Json
}
else {
    $aadConfig = CreateAzureADApplications
    $deleteApp = $true
}

$deleteOnErrorPrompt = GetOrCreateResourceGroup
try {
    Write-Host "Starting deployment..."
    & ($deploymentScript) -resourceGroupName $script:resourceGroupName -aadConfig $aadConfig
    Write-Host "Deployment succeeded."
}
catch {
    Write-Host "Deployment failed."
    $ex = $_.Exception
    if ($deleteOnErrorPrompt) {
        $reply = Read-Host -Prompt "Delete resource group? [y/n]"
        if ($reply -match "[yY]") { 
            try {
                Remove-AzureRmResourceGroup -Name $script:resourceGroupName -Force
                if ($deleteApp) {
                    DeleteAzureADApplications
                }
            }
            catch {
                Write-Host $_.Exception.Message
            }
        }
    }
    throw $ex
}