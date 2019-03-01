<#
 .SYNOPSIS
    Deploys Industrial IoT services to Azure

 .DESCRIPTION
    Deploys the Industrial IoT services dependencies and optionally micro services and UI to Azure.

 .PARAMETER type
    The type of deployment (vm, local)

 .PARAMETER resourceGroupName
    Can be the name of an existing or a new resource group.

 .PARAMETER subscriptionId
    Optional, the subscription id where resources will be deployed.

 .PARAMETER subscriptionName
    Or alternatively the subscription name.

 .PARAMETER resourceGroupLocation
    Optional, a resource group location. If specified, will try to create a new resource group in this location.

 .PARAMETER withAuthentication
    Whether to enable authentication - defaults to $true.

 .PARAMETER tenantId
    AD tenant to use. 

 .PARAMETER credentials
    To support non interactive usage of script. (TODO)
#>

param(
    [string] $type = "vm",
    [string] $resourceGroupName,
    [string] $resourceGroupLocation,
    [string] $subscriptionName,
    [string] $subscriptionId,
    [string] $accountName,
    $credentials,
    [string] $tenantId,
    [bool] $withAuthentication = $true,
    [ValidateSet("AzureCloud")] [string] $environmentName = "AzureCloud"
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
            $script:accountName = $rmProfile.Context.Account.Id
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
        if ($reply -match "[yY]") { 
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
    Write-Host "Azure subscriptionId '$subscriptionId' selected."
}

#*******************************************************************************************************
# Called if no Azure location is configured for the deployment to let the user choose a location.
#*******************************************************************************************************
Function SelectLocation() {
    $locations = @()
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
        [string] $locationToValidate
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
            Write-Host
            Write-Host "Select an Active Directory Tenant to use..."
            Write-Host "Available:"
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
Function ConnectToAzureADTenant() {
    if ($script:interactive) {
        # Interactive
        if (!$script:tenantId) {
            if (!$script:withAuthentication) {
                $reply = Read-Host -Prompt "Enable authentication? [y/n]"
                if ( $reply -notmatch "[yY]" ) { 
                    return $null
                }
            }
            $script:tenantId = SelectAzureADTenantId
        }
    }
    if (!$script:credentials) {
        if (!$script:tenantId) {
            throw "No tenant selected for AAD connect."
        }
        else {
            # Make sure we get token from token cache instead of interactive logon
            $graphAuth = AcquireToken $script:tenantId $script:environment.ActiveDirectoryAuthority `
                $script:environment.GraphUrl $script:accountName "Auto"
            $user = Invoke-RestMethod -Method "GET" `
                -Uri ("{0}{1}/me?api-version=1.6" -f $script:environment.GraphUrl, $script:tenantId) `
                -Headers @{ `
                    "Authorization"=$($graphAuth.CreateAuthorizationHeader()); `
                    "Content-Type"="application/json" `
                }
            return Connect-AzureAD -MsAccessToken $graphAuth.AccessToken -TenantId $script:tenantId `
                -ErrorAction Stop -AadAccessToken $graphAuth.AccessToken -AccountId $user.userPrincipalName
        }
    }
    else {
        if (!$script:tenantId) {
            # use home tenant
            return Connect-AzureAD -Credential $script:credential `
                -ErrorAction Stop
        }
        else {
            return Connect-AzureAD -Credential $script:credential -TenantId $script:tenantId `
                -ErrorAction Stop
        }
    }
    return $null
}

#*******************************************************************************************************
# Adds the requiredAccesses (expressed as a pipe separated string) to the requiredAccess structure
#*******************************************************************************************************
Function AddResourcePermission() {
    Param (
        $requiredAccess, 
        $exposedPermissions, 
        [string]$requiredAccesses, `
        [string]$permissionType
    ) 
    foreach($permission in $requiredAccesses.Trim().Split("|")) {
        foreach($exposedPermission in $exposedPermissions) {
            if ($exposedPermission.Value -eq $permission) {
                $resourceAccess = New-Object Microsoft.Open.AzureAD.Model.ResourceAccess
                # Scope = Delegated permissions | Role = Application permissions
                $resourceAccess.Type = $permissionType
                # Read directory data
                $resourceAccess.Id = $exposedPermission.Id 
                $requiredAccess.ResourceAccess.Add($resourceAccess)
            }
        }
    }
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
# Create an application role of given name and description
#*******************************************************************************************************
Function CreateAppRole() {
    param(
        $current,
        [string] $name, 
        [string] $description, 
        [string] $value
    )
    $appRole = $current | Where-Object { $_.Value -eq $value }
    if (!$appRole) {
        $appRole = New-Object Microsoft.Open.AzureAD.Model.AppRole
        $appRole.AllowedMemberTypes = New-Object System.Collections.Generic.List[string]
        $appRole.AllowedMemberTypes.Add("User");
        $appRole.Id = New-Guid
        $appRole.IsEnabled = $true
        $appRole.Value = $value;
    }
    $appRole.DisplayName = $name
    $appRole.Description = $description
    return $appRole
}

#*******************************************************************************************************
# Get configuration object for service and client applications
#*******************************************************************************************************
Function GetAzureADApplicationConfig() {
    $serviceDisplayName = "azure-iiot-services"
    $clientDisplayName = "azure-iiot-clients"
    try {
        $creds = ConnectToAzureADTenant
        if (!$creds) {
            return $null
        }
        $script:tenantId = $creds.Tenant.Id
        if (!$script:tenantId) {
            return $null
        }

        $tenant = Get-AzureADTenantDetail
        $tenantName =  ($tenant.VerifiedDomains | Where { $_._Default -eq $True }).Name
        Write-Host "Selected Tenant '$tenantName' as authority."

        $serviceAadApplication=Get-AzureADApplication `
            -Filter "identifierUris/any(uri:uri eq 'https://$tenantName/$serviceDisplayName')"  
        if (!$serviceAadApplication) {
            $serviceAadApplication = New-AzureADApplication -DisplayName $serviceDisplayName `
                -PublicClient $False -HomePage "https://localhost" `
                -IdentifierUris "https://$tenantName/$serviceDisplayName"
            Write-Host "Created new AAD service application."
        }
        $serviceServicePrincipal=Get-AzureADServicePrincipal `
             -Filter "AppId eq '$($serviceAadApplication.AppId)'"
        if (!$serviceServicePrincipal) {
            $serviceServicePrincipal = New-AzureADServicePrincipal `
                -AppId $serviceAadApplication.AppId `
                -Tags {WindowsAzureActiveDirectoryIntegratedApp}
        }

        $clientAadApplication=Get-AzureADApplication `
            -Filter "DisplayName eq '$clientDisplayName'"
        if (!$clientAadApplication) {
            $clientAadApplication = New-AzureADApplication -DisplayName $clientDisplayName `
                -PublicClient $True
            Write-Host "Created new AAD client application."
        }

        # Find client principal
        $clientServicePrincipal=Get-AzureADServicePrincipal `
             -Filter "AppId eq '$($clientAadApplication.AppId)'"
        if (!$clientServicePrincipal) {
            $clientServicePrincipal = New-AzureADServicePrincipal `
                -AppId $clientAadApplication.AppId `
                -Tags {WindowsAzureActiveDirectoryIntegratedApp}
        }

        #
        # Try to add current user as app owner 
        #
        try {
            $user = Get-AzureADUser -ObjectId $creds.Account.Id -ErrorAction Stop
            # TODO: Check whether already owner...

            Add-AzureADApplicationOwner -ObjectId $serviceAadApplication.ObjectId `
                -RefObjectId $user.ObjectId
            Add-AzureADApplicationOwner -ObjectId $clientAadApplication.ObjectId `
                -RefObjectId $user.ObjectId
            Write-Host "'$($user.UserPrincipalName)' added as owner for applications."
        }
        catch {
            Write-Verbose "Adding $($creds.Account.Id) as owner failed."
        }

        #
        # Update service application to add roles, known applications and required permissions
        #
        $approverRole = CreateAppRole -current $serviceAadApplication.AppRoles -name "Approver" `
            -value "Sign" -description "Approvers have the ability to issue certificates."
        $writerRole = CreateAppRole -current $serviceAadApplication.AppRoles -name "Writer" `
            -value "Write" -description "Writers Have the ability to change entities."
        $adminRole = CreateAppRole -current $serviceAadApplication.AppRoles -name "Administrator" `
            -value "Admin" -description "Admins can access advanced features."
        $appRoles = New-Object `
            System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.AppRole]
        $appRoles.Add($writerRole)
        $appRoles.Add($approverRole)
        $appRoles.Add($adminRole)
        $knownApplications = New-Object System.Collections.Generic.List[System.String]
        $knownApplications.Add($clientAadApplication.AppId)
        $requiredResourcesAccess = `
            New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.RequiredResourceAccess]
        $requiredPermissions = GetRequiredPermissions -applicationDisplayName "Azure Key Vault" `
            -requiredDelegatedPermissions "user_impersonation" 
        $requiredResourcesAccess.Add($requiredPermissions)
        $requiredPermissions = GetRequiredPermissions -applicationDisplayName "Microsoft Graph" `
            -requiredDelegatedPermissions "User.Read" 
        $requiredResourcesAccess.Add($requiredPermissions)
        Set-AzureADApplication -ObjectId $serviceAadApplication.ObjectId `
            -RequiredResourceAccess $requiredResourcesAccess `
            -KnownClientApplications $knownApplications -AppRoles $appRoles

        # 
        # Update client application to add reply urls required permissions.
        #
        $replyUrls = New-Object System.Collections.Generic.List[System.String]
        $replyUrls.Add("urn:ietf:wg:oauth:2.0:oob")
        $requiredResourcesAccess = `
            New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.RequiredResourceAccess]
        $requiredPermissions = GetRequiredPermissions -applicationDisplayName $serviceDisplayName `
            -requiredDelegatedPermissions "user_impersonation" # "Directory.Read.All|User.Read"
        $requiredResourcesAccess.Add($requiredPermissions)
        $requiredPermissions = GetRequiredPermissions -applicationDisplayName "Microsoft Graph" `
            -requiredDelegatedPermissions "User.Read" 
        $requiredResourcesAccess.Add($requiredPermissions)
        Set-AzureADApplication -ObjectId $clientAadApplication.ObjectId `
            -RequiredResourceAccess $requiredResourcesAccess -ReplyUrls $replyUrls `
            -Oauth2AllowImplicitFlow $True -Oauth2AllowUrlPathMatching $True

        return [pscustomobject] @{ 
            TenantId = $tenantId
            Instance = $script:environment.ActiveDirectoryAuthority
            Audience = $serviceAadApplication.IdentifierUris[0].ToString()
            AppId = $serviceAadApplication.AppId
            AppObjectId = $serviceAadApplication.ObjectId
            ClientId = $clientAadApplication.AppId
            ClientObjectId = $clientAadApplication.ObjectId
        }
    }
    catch {
        $ex = $_.Exception

        Write-Host
        Write-Host "An error occurred: $($ex.Message)" 
        Write-Host
        Write-Host "Ensure you have installed the AzureAD cmdlets:" 
        Write-Host "1) Run Powershell as an administrator" 
        Write-Host "2) in the PowerShell window, type: Install-Module AzureAD" 
        Write-Host

        $reply = Read-Host -Prompt "Continue without authentication? [y/n]"
        if ($reply -match "[yY]") { 
            return $null
        }
        throw $ex
    }
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

#******************************************************************************
# Get an access token
#******************************************************************************
Function GetAzureToken() {

    try {
        $context = Get-AzureRmContext
        $tenantId = $context.Tenant.Id
        $refreshToken = @($context.TokenCache.ReadItems() | where {$_.tenantId -eq $tenantId -and $_.ExpiresOn -gt (Get-Date)})[0].RefreshToken
        $body = "grant_type=refresh_token&refresh_token=$($refreshToken)&resource=74658136-14ec-4630-ad9b-26e160ff0fc6"
        $apiToken = Invoke-RestMethod "https://login.windows.net/$tenantId/oauth2/token" -Method POST -Body $body -ContentType "application/x-www-form-urlencoded"
        return $apiToken.access_token
    }
    catch {
        Write-Host "An error occurred: $($_.Exception.Message)"
    }
}

#******************************************************************************
# Grant permission to to the app with id = $azureAppId
#******************************************************************************
Function GrantPermission() {
    
    Param(
        [string] $azureAppId
    )

    try { 
        $token = GetAzureToken
        $header = @{
            "Authorization"          = "Bearer " + $token
            "X-Requested-With"       = "XMLHttpRequest"
            "x-ms-client-request-id" = [guid]::NewGuid()
            "x-ms-correlation-id"    = [guid]::NewGuid()
        } 
        $url = "https://main.iam.ad.ext.azure.com/api/RegisteredApplications/" + $azureAppId + "/Consent?onBehalfOfAll=true"
        Invoke-RestMethod -Uri $url -Method Post -Headers $header
    }
    catch {
        Write-Host "An error occurred: $($_.Exception.Message)"
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

$script:interactive = $($script:credential -eq $null)
$script:subscriptionId = $null

SelectEnvironment
Login
SelectSubscription

$aadConfig = GetAzureADApplicationConfig
$deleteOnErrorPrompt = GetOrCreateResourceGroup

GrantPermission -azureAppId $aadConfig.ClientObjectId

try {
    Write-Host "Almost done..."
    & ($deploymentScript) -resourceGroupName $script:resourceGroupName `
        -interactive $script:interactive -aadConfig $aadConfig

    Write-Host "Grant permissions"
    GrantPermission -azureAppId $aadConfig.ClientId   
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
            }
            catch {
                Write-Host $_.Exception.Message
            }
        }
    }
    throw $ex
}
