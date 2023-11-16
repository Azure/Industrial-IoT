<#
 .SYNOPSIS
    Registers required applications.

 .DESCRIPTION
    Registers the required applications in AAD and returns an
    object containing the information.

 .PARAMETER Name
    The Name prefix under which to register the applications.

 .PARAMETER TenantId
    The Azure Active Directory tenant to use.

 .PARAMETER Context
    A previously created az context (optional).

 .PARAMETER Credentials
    Credentials to use to log in (optional).

 .PARAMETER ReplyUrl
    A reply url to add to the web application (optional).
#>

param(
    [Parameter(Mandatory = $true)] [string] $Name,
    [object] $Context,
    [string] $TenantId,
    [string] $Output,
    [string] $ReplyUrl,
    [string] $EnvironmentName = "AzureCloud"
)

<#.Description
   Perform login - uses profile file if exists - returns account
#>
Function Select-Context() {
    [OutputType([Microsoft.Azure.Commands.Profile.Models.Core.PSAzureContext])]
    Param(
        [string] $environmentName
    )

    $rootDir = Get-RootFolder $script:ScriptDir
    $contextFile = Join-Path $rootDir ".user"
    if ((Test-Path $contextFile) -and [string]::IsNullOrEmpty($script:TenantId)) {
        # Migrate .user file into root (next to .env)
        if (!(Test-Path $contextFile)) {
            $oldFile = Join-Path $script:ScriptDir ".user"
            if (Test-Path $oldFile) {
                Move-Item -Path $oldFile -Destination $contextFile
            }
        }
        $azProfile = Import-AzContext -Path $contextFile
        $context = $azProfile.Context
        if ($context) {
            Write-Host "Using saved context (Account $($context.Account), Tenant $($context.Tenant.Id))"
            return $context
        }
    }

    $tenantIdArg = @{}

    if (![string]::IsNullOrEmpty($script:tenantId)) {
        $tenantIdArg = @{
            TenantId = $script:tenantId
        }
    }

    # Login and get context
    try {
        $azProfile = Connect-AzAccount `
            -Environment $environmentName @tenantIdArg -AuthScope AadGraph `
            -WarningAction Stop

        $reply = Read-Host -Prompt "Save credentials in .user file? [y/n]"
        if ($reply -match "[yY]") {
            Save-AzContext -Path $contextFile -Profile $connection
        }

        $context = $azProfile.Context
        Write-Host "Using context (Account $($context.Account), Tenant $($context.Tenant.Id))"
        return $context
    }
    catch {
        try {
            # for the case where prompting web browser is not applicable, we will
            # prompt for DeviceAuthentication login.
            $azProfile = Connect-AzAccount -AuthScope AadGraph `
                -UseDeviceAuthentication `
                -Environment $environmentName @tenantIdArg `
                -ErrorAction Stop

            $reply = Read-Host -Prompt "Save credentials in .user file? [y/n]"
            if ($reply -match "[yY]") {
                Save-AzContext -Path $contextFile -Profile $connection
            }

            $context = $azProfile.Context
            Write-Host "Using context (Account $($context.Account), Tenant $($context.Tenant.Id))"
            return $context
        }
        catch{
            throw "The login to the Azure account was not successful."
        }
    }
}

<#.Description
   find the top most folder with solution in it
#>
Function Get-RootFolder() {
    param(
        $startDir
    )
    $cur = $startDir
    while (![string]::IsNullOrEmpty($cur)) {
        if (Test-Path -Path (Join-Path $cur "Industrial-IoT.sln") -PathType Leaf) {
            return $cur
        }
        $cur = Split-Path $cur
    }
    return $startDir
}

<#.Description
   Grant consent to to the app with id = $azureAppId
#>
Function Add-AdminConsentGrant() {
    Param(
        [string] $azureAppId
    )

    $context = $script:Context
    if (!$context) {
        throw "Not authorized to give admin consent for $($azureAppId)."
    }
    $url = "https://main.iam.ad.ext.azure.com/api/RegisteredApplications/$($azureAppId)/Consent?onBehalfOfAll=true"
    for ($i = 0; $i -lt 10; $i++) {
        # Try 10 times * 3 s = 30 s
        $token = [Microsoft.Azure.Commands.Common.Authentication.AzureSession]::Instance.AuthenticationFactory.Authenticate( `
            $context.Account, $context.Environment, $context.Tenant.Id, $null, "Never", `
            $null, "74658136-14ec-4630-ad9b-26e160ff0fc6")

        if (!$token) {
            throw "Failed to get auth token for $($context.Tenant.Id)."
        }
        try {
            $header = @{
                "Authorization"          = "Bearer $($token.AccessToken)"
                "X-Requested-With"       = "XMLHttpRequest"
                "x-ms-client-request-id" = [guid]::NewGuid()
                "x-ms-correlation-id"    = [guid]::NewGuid()
            }
            Invoke-RestMethod -Uri $url -Method POST -Headers $header
            # Success
            return
        }
        catch {
            Write-Verbose "$($_.Exception.Message) at $($url) - Retrying..."
            Start-Sleep -s 3
        }
    }
    throw "Failed to grant consent for $($azureAppId)."
}

<#.Description
   Create an application key
   See https://www.sabin.io/blog/adding-an-azure-active-directory-application-and-key-using-powershell/
#>
Function CreateAppKey([DateTime] $fromDate, [double] $durationInMonths)
{
    $key = New-Object Microsoft.Graph.PowerShell.Models.MicrosoftGraphPasswordCredential

    $key.StartDateTime = $fromDate
    $key.EndDateTime = $fromDate.AddMonths($durationInMonths)
    $key.KeyId = (New-Guid).ToString()
    $key.DisplayName = "app secret"

    return $key
}

<#.Description
   Adds the requiredAccesses (expressed as a pipe separated string) to the requiredAccess structure
   The exposed permissions are in the $exposedPermissions collection, and the type of permission
   (Scope | Role) is described in $permissionType
#>
Function AddResourcePermission(
    $requiredAccess,
    $exposedPermissions,
    [string]$requiredAccesses,
    [string]$permissionType)
{
    foreach($permission in $requiredAccesses.Trim().Split("|"))
    {
        foreach($exposedPermission in $exposedPermissions)
        {
            if ($exposedPermission.Value -eq $permission)
            {
                $resourceAccess = New-Object Microsoft.Graph.PowerShell.Models.MicrosoftGraphResourceAccess
                $resourceAccess.Type = $permissionType # Scope = Delegated permissions | Role = Application permissions
                $resourceAccess.Id = $exposedPermission.Id # Read directory data
                $requiredAccess.ResourceAccess += $resourceAccess
            }
        }
    }
}

<#.Description
   Example: GetRequiredPermissions "Microsoft Graph"  "Graph.Read|User.Read"
  See also: http://stackoverflow.com/questions/42164581/how-to-configure-a-new-azure-ad-application-through-powershell
#>
Function GetRequiredPermissions(
    [string] $applicationDisplayName,
    [string] $requiredDelegatedPermissions,
    [string] $requiredApplicationPermissions,
    $servicePrincipal)
{
    # If we are passed the service principal we use it directly, otherwise we find it
    # from the display name (which might not be unique)
    if ($servicePrincipal)
    {
        $sp = $servicePrincipal
    }
    else
    {
        $sp = Get-MgServicePrincipal -Filter "DisplayName eq '$applicationDisplayName'"
    }
    $appid = $sp.AppId
    $requiredAccess = New-Object Microsoft.Graph.PowerShell.Models.MicrosoftGraphRequiredResourceAccess
    $requiredAccess.ResourceAppId = $appid
    $requiredAccess.ResourceAccess = New-Object `
        System.Collections.Generic.List[Microsoft.Graph.PowerShell.Models.MicrosoftGraphResourceAccess]

    # $sp.Oauth2Permissions | Select Id,AdminConsentDisplayName,Value: To see the list
    # of all the Delegated permissions for the application:
    if ($requiredDelegatedPermissions)
    {
        AddResourcePermission $requiredAccess -exposedPermissions $sp.Oauth2PermissionScopes `
            -requiredAccesses $requiredDelegatedPermissions -permissionType "Scope"
    }

    # $sp.AppRoles | Select Id,AdminConsentDisplayName,Value: To see the list of all the
    # Application permissions for the application
    if ($requiredApplicationPermissions)
    {
        AddResourcePermission $requiredAccess -exposedPermissions $sp.AppRoles `
            -requiredAccesses $requiredApplicationPermissions -permissionType "Role"
    }
    return $requiredAccess
}

<#.Description
   This function creates a new Azure AD scope (OAuth2Permission) with default and
   provided values
#>
Function CreateScope(
    [string] $value,
    [string] $userConsentDisplayName,
    [string] $userConsentDescription,
    [string] $adminConsentDisplayName,
    [string] $adminConsentDescription)
{
    $scope = New-Object Microsoft.Graph.PowerShell.Models.MicrosoftGraphPermissionScope
    $scope.Id = New-Guid
    $scope.Value = $value
    $scope.UserConsentDisplayName = $userConsentDisplayName
    $scope.UserConsentDescription = $userConsentDescription
    $scope.AdminConsentDisplayName = $adminConsentDisplayName
    $scope.AdminConsentDescription = $adminConsentDescription
    $scope.IsEnabled = $true
    $scope.Type = "User"
    return $scope
}

<#.Description
   This function creates a new Azure AD AppRole with default and provided values
#>
Function CreateAppRole(
    [string] $types,
    [string] $name,
    [string] $value,
    [string] $description)
{
    $appRole = New-Object Microsoft.Graph.PowerShell.Models.MicrosoftGraphAppRole
    $appRole.AllowedMemberTypes = New-Object System.Collections.Generic.List[string]
    $typesArr = $types.Split(',')
    foreach($type in $typesArr)
    {
        $appRole.AllowedMemberTypes += $type;
    }
    $appRole.DisplayName = $name
    $appRole.Id = New-Guid
    $appRole.IsEnabled = $true
    $appRole.Description = $description
    $appRole.Value = $value;
    return $appRole
}

<#.Description
   This function takes a string as input and creates an instance of an Optional claim object
#>
Function CreateOptionalClaim([string] $name)
{
    <#.Description
    This function creates a new Azure AD optional claims  with default and provided values
    #>

    $appClaim = New-Object `
        Microsoft.Graph.PowerShell.Models.MicrosoftGraphOptionalClaim
    $appClaim.AdditionalProperties =  New-Object System.Collections.Generic.List[string]
    $appClaim.Source =  $null
    $appClaim.Essential = $false
    $appClaim.Name = $name
    return $appClaim
}

<#.Description
   This function takes a string as input and creates an instance of an
   preauthorized application
#>
Function CreatePreAuthorizedApplication([string] $appId, [string[]] $delegatedPermissionIds)
{
    <#.Description
    This function creates a new preauthorized application
    #>

    $application = New-Object `
        Microsoft.Graph.PowerShell.Models.MicrosoftGraphPreAuthorizedApplication
    $application.AppId =  $appId
    $application.DelegatedPermissionIds = $delegatedPermissionIds
    return $application
}

<#.Description
   Get configuration object for service, web and client applications
#>
Function Connect-MicrosoftGraph() {
    param(
        [Microsoft.Azure.Commands.Profile.Models.Core.PSAzureContext] $context
    )

    $tenantId = $context.Tenant.Id
    if (![string]::IsNullOrEmpty($script:TenantId)) {
        $tenantId = $script:TenantId
    }

    try {
        $token = Get-AzAccessToken -ResourceTypeName MSGraph
        if ($token) {
            try {
                $secureToken = ConvertTo-SecureString -AsPlainText -Force -String $token.Token
                Connect-MgGraph -AccessToken $secureToken | Out-Null
            }
            catch {
                # Fall back to Connect-MgGraph 1.x behavior and pass access token as plain text
                Connect-MgGraph -AccessToken $token.Token | Out-Null
            }
            return
        }
    }
    catch {
        if ($script:interactive) {
            throw
        }
        Write-Warning $_.Exception.Message
    }
    Connect-MgGraph -Scopes "User.Read.All Organization.Read.All Application.Write.All" `
        -Environment $context.Environment.Name `
        -TenantId $tenantId | Out-Null
}

<#.Description
   Get configuration object for service, web and client applications
#>
Function ConfigureApplications() {
    param(
        [string] $applicationName,
        [string] $accessToken
    )
    $context = Get-MgContext

    # Get the user running the script
    $currentUserPrincipalName = $context.Account
    if (!$currentUserPrincipalName) {
        $currentUserPrincipalName = $script:Context.Account.Id
    }
    $tenantId = $context.TenantId
    if (!$tenantId) {
        $tenantId = $script:Context.Tenant.Id
    }
    $authority = $script:Context.Environment.ActiveDirectoryAuthority

    # get the tenant we signed in to
    $tenant = Get-MgOrganization
    # $tenantDisplayName = $tenant.DisplayName
    $defaultTenant = ($tenant.VerifiedDomains | Where-Object { $_.IsDefault -eq $True })
    $tenantName = $defaultTenant.Name

    if ($currentUserPrincipalName) {
        # Try to get current user
        try {
            try {
                $user = Get-MgUser -Filter "UserPrincipalName eq '$($currentUserPrincipalName)'" -ErrorAction Stop
            }
            catch {
                $user = (Get-MgUser -Search "UserPrincipalName:$currentUserPrincipalName" -ErrorAction Stop)[0]
            }
        }
        catch {
            Write-Warning "Getting user principal for $($creds.Account.Id) failed due to $($_.Exception.Message)."
        }

        if (!$user) {
            # Handle guests
            $accountId = $currentUserPrincipalName.Replace("@", "_")
            $user = (Get-MgUser -Filter "startswith(userPrincipalName, '$($accountId)')")
        }
    }

    # Get or create native client application
    $clientDisplayName = $applicationName + "-client"
    $clientAadApplication = Get-MgApplication -Filter "DisplayName eq '$clientDisplayName'"
    if (!$clientAadApplication) {
        $clientAadApplication = New-MgApplication -DisplayName $clientDisplayName
        Write-Host "Created new client application '$($clientDisplayName)' in Tenant '$($tenantName)'."
    }
    else {
        Write-Host "Updating client application '$($clientDisplayName)' in Tenant '$($tenantName)'."
    }
    $currentAppId = $clientAadApplication.AppId
    $currentAppObjectId = $clientAadApplication.Id

    Update-MgApplication -ApplicationId $currentAppObjectId -PublicClient `
        @{
            RedirectUris = @(
                "urn:ietf:wg:oauth:2.0:oob"
                "https://localhost"
                "http://localhost"
            )
        } `
        -SignInAudience AzureADMyOrg -IsFallbackPublicClient
    Write-Host "  Added redirect uris."

    $owner = Get-MgApplicationOwner -ApplicationId $currentAppObjectId
    if ($user -and (-not $owner)) {
        try {
            New-MgApplicationOwnerByRef -ApplicationId $currentAppObjectId -BodyParameter `
                @{"@odata.id" = "https://graph.microsoft.com/v1.0/directoryObjects/$($user.Id)"}
            Write-Host "  Added '$($user.UserPrincipalName)' as owner."
        }
        catch {
            Write-Warning "Adding $($user.UserPrincipalName) as owner failed due to $($_.Exception.Message)."
        }
    }

    # create the service principal of the newly created application
    $currentServicePrincipal = Get-MgServicePrincipal -Filter "AppId eq '$($currentAppId)'"
    if (!$currentServicePrincipal) {
        $currentServicePrincipal = New-MgServicePrincipal -AppId $currentAppId `
            -Tags { WindowsAzureActiveDirectoryIntegratedApp }
        Write-Host "  Added new service principal."
    }

    #
    # Create web application
    #
    $webDisplayName = $applicationName + "-web"
    $webAadApplication = Get-MgApplication -Filter "DisplayName eq '$webDisplayName'"
    if (!$webAadApplication) {
        $webAadApplication = New-MgApplication -DisplayName $webDisplayName
        Write-Host "Created new web client application '$($webDisplayName)' in Tenant '$($tenantName)'."
    }
    else {
        Write-Host "Updating web client application '$($webDisplayName)' in Tenant '$($tenantName)'."
    }
    $currentAppId = $webAadApplication.AppId
    $currentAppObjectId = $webAadApplication.Id

    $replyUrls = @("https://localhost:44321/signin-oidc")
    if (![string]::IsNullOrEmpty($script:ReplyUrl)) {
        $replyUrls = @($script:ReplyUrl, "urn:ietf:wg:oauth:2.0:oob")
    }

    Update-MgApplication -ApplicationId $currentAppObjectId -Web `
        @{
            RedirectUris = $replyUrls
            ImplicitGrantSettings = @{
                EnableAccessTokenIssuance = $True
                EnableIdTokenIssuance = $True
            }
        } `
        -SignInAudience AzureADMyOrg
    Write-Host "  Added redirect uris."

    $owner = Get-MgApplicationOwner -ApplicationId $currentAppObjectId
    if ($user -and (-not $owner)) {
        try {
            New-MgApplicationOwnerByRef -ApplicationId $currentAppObjectId -BodyParameter `
                @{"@odata.id" = "https://graph.microsoft.com/v1.0/directoryObjects/$($user.Id)"}
            Write-Host "  Added '$($user.UserPrincipalName)' as owner."
        }
        catch {
            Write-Warning "Adding $($user.UserPrincipalName) as owner failed due to $($_.Exception.Message)."
        }
    }

    # Get a 24 months application key for the web application
    $fromDate = [DateTime]::Now;
    $key = CreateAppKey -fromDate $fromDate -durationInMonths 24
    $webAppSecret = Add-MgApplicationPassword -ApplicationId $currentAppObjectId -PasswordCredential $key
    Write-Host "  Added a secret."

    # create the service principal of the newly created application
    $currentServicePrincipal = Get-MgServicePrincipal -Filter "AppId eq '$($currentAppId)'"
    if (!$currentServicePrincipal) {
        $currentServicePrincipal = New-MgServicePrincipal -AppId $currentAppId `
            -Tags { WindowsAzureActiveDirectoryIntegratedApp }
        Write-Host "  Added new service principal."
    }

    #
    # Create service application
    #
    $serviceDisplayName = $applicationName + "-service"
    $serviceAadApplication = Get-MgApplication -Filter "DisplayName eq '$serviceDisplayName'"
    if (!$serviceAadApplication) {
        $serviceAadApplication = New-MgApplication -DisplayName $serviceDisplayName
        Write-Host "Created new service application '$($serviceDisplayName)' in Tenant '$($tenantName)'."
    }
    else {
        Write-Host "Updating service application '$($serviceDisplayName)' in Tenant '$($tenantName)'."
    }
    $currentAppId = $serviceAadApplication.AppId
    $currentAppObjectId = $serviceAadApplication.Id

    $tenantName = (Get-MgApplication -ApplicationId $currentAppObjectId).PublisherDomain
    Update-MgApplication -ApplicationId $currentAppObjectId -Web `
        @{
            HomePageUrl = "https://localhost"
        } `
        -Api `
        @{
            RequestedAccessTokenVersion = 2
        } `
        -SignInAudience AzureADMyOrg -IdentifierUris `
        @(
            "api://$currentAppId"
            "https://$tenantName/$serviceDisplayName"
        )
    Write-Host "  Added identifiers."

    $owner = Get-MgApplicationOwner -ApplicationId $currentAppObjectId
    if ($user -and (-not $owner)) {
        try {
            New-MgApplicationOwnerByRef -ApplicationId $currentAppObjectId -BodyParameter `
                @{"@odata.id" = "https://graph.microsoft.com/v1.0/directoryObjects/$($user.Id)"}
            Write-Host "  Added '$($user.UserPrincipalName)' as owner."
        }
        catch {
            Write-Warning "Adding $($user.UserPrincipalName) as owner failed due to $($_.Exception.Message)."
        }
    }

    # Get a 24 months application key for the service application (is this needed?)
    $fromDate = [DateTime]::Now;
    $key = CreateAppKey -fromDate $fromDate -durationInMonths 24
    $serviceSecret  = Add-MgApplicationPassword -ApplicationId $currentAppObjectId -PasswordCredential $key
    Write-Host "  Added a secret."

    # Add optional Claims
    $optionalClaims = New-Object Microsoft.Graph.PowerShell.Models.MicrosoftGraphOptionalClaims
    $optionalClaims.AccessToken = New-Object `
        System.Collections.Generic.List[Microsoft.Graph.PowerShell.Models.MicrosoftGraphOptionalClaim]
    $optionalClaims.IdToken = New-Object `
        System.Collections.Generic.List[Microsoft.Graph.PowerShell.Models.MicrosoftGraphOptionalClaim]
    $optionalClaims.Saml2Token = New-Object `
        System.Collections.Generic.List[Microsoft.Graph.PowerShell.Models.MicrosoftGraphOptionalClaim]
    $newClaim =  CreateOptionalClaim -name "idtyp"
    $optionalClaims.AccessToken += ($newClaim)
    Update-MgApplication -ApplicationId $currentAppObjectId -OptionalClaims $optionalClaims
    Write-Host "  Added optional claims."

    # Add app roles and permissions
    $allAppRoles = $serviceAadApplication.AppRoles
    if ($allAppRoles.Count -eq 0) {
        $appRoles = New-Object `
            System.Collections.Generic.List[Microsoft.Graph.PowerShell.Models.MicrosoftGraphAppRole]
        $newRole = CreateAppRole -current $serviceAadApplication.AppRoles -name "Readers" `
            -value "Reader" -types "User" `
            -description "Readers have the ability to read entitities."
        $appRoles.Add($newRole)
        $newRole = CreateAppRole -current $serviceAadApplication.AppRoles -name "Writers" `
            -value "Writer" -types "User" `
            -description "Writers have the ability to call and write entities."
        $appRoles.Add($newRole)
        $newRole = CreateAppRole -current $serviceAadApplication.AppRoles -name "Administrators" `
            -value "Admin" -types "Application,User" `
            -description "Admins can access read, write and advanced features."
        $appRoles.Add($newRole)
        Update-MgApplication -ApplicationId $currentAppObjectId -AppRoles $appRoles
        Write-Host "  Added api roles."
    }

    # add/update scopes
    $scopes = New-Object `
        System.Collections.Generic.List[Microsoft.Graph.PowerShell.Models.MicrosoftGraphPermissionScope]
    $existingScopes = $serviceAadApplication.Api.Oauth2PermissionScopes
    $scope = $existingScopes | Where-Object { $_.Value -eq "User_impersonation" }
    if ($scope)
    {
        $scopes.Add($scope)
        if ($existingScopes.Count -eq 1) {
            $existingScopes = @()
        }
# disable the scope
# $scope.IsEnabled = $false
# Update-MgApplication -ApplicationId $currentAppObjectId -Api @{Oauth2PermissionScopes = @($scopes)}
# clear the scope
# Update-MgApplication -ApplicationId $currentAppObjectId -Api @{Oauth2PermissionScopes = @()}
# $scopes =  `
#New-Object System.Collections.Generic.List[Microsoft.Graph.PowerShell.Models.MicrosoftGraphPermissionScope]
    }
    if ($existingScopes.Count -eq 0) {
        $scope = CreateScope -value Read  `
            -userConsentDisplayName "Read entities using Industrial IoT"  `
            -userConsentDescription "Allow the app to read entities in the Industrial IoT platform."  `
            -adminConsentDisplayName "Read entities using Industrial IoT"  `
            -adminConsentDescription "Allow the app to read entities using Industrial IoT"
        $scopes.Add($scope)
        $scope = CreateScope -value ReadWrite  `
            -userConsentDisplayName "Read and Write entities using Industrial IoT"  `
            -userConsentDescription "Allow the app to read and write entities in the Industrial IoT platform."  `
            -adminConsentDisplayName "Read and Write entities using Industrial IoT"  `
            -adminConsentDescription "Allow the app to read and write entities using Industrial IoT"
        $scopes.Add($scope)
        Update-MgApplication -ApplicationId $currentAppObjectId `
            -Api @{Oauth2PermissionScopes = @($scopes)}
        Write-Host "  Added api scopes."
    }

    $knownApplications = New-Object System.Collections.Generic.List[System.String]
    $knownApplications.Add($clientAadApplication.AppId)
    $knownApplications.Add($webAadApplication.AppId)
    $knownApplications.Add("04b07795-8ddb-461a-bbee-02f9e1bf7b46")
    $knownApplications.Add("872cd9fa-d31f-45e0-9eab-6e460a02d1f1")
    Update-MgApplication -ApplicationId $currentAppObjectId `
        -Api @{KnownClientApplications = @($knownApplications)}
    Write-Host "  Added known applications."

    # Add 1) Azure CLI and 2) Visual Studio to allow log onto the platform with them as clients
    $preauthApplications = New-Object `
        System.Collections.Generic.List[Microsoft.Graph.PowerShell.Models.MicrosoftGraphPreAuthorizedApplication]
    $serviceAadApplication = Get-MgApplication -ApplicationId $currentAppObjectId
    $existingScopes = $serviceAadApplication.Api.Oauth2PermissionScopes
    $delegatedPermissionIds = $($existingScopes | Select-Object -ExpandProperty Id)
    $preauthApplication = CreatePreAuthorizedApplication `
        -appId "04b07795-8ddb-461a-bbee-02f9e1bf7b46" -delegatedPermissionIds @($delegatedPermissionIds)
    $preauthApplications.Add($preauthApplication)
    $preauthApplication = CreatePreAuthorizedApplication `
        -appId "872cd9fa-d31f-45e0-9eab-6e460a02d1f1" -delegatedPermissionIds @($delegatedPermissionIds)
    $preauthApplications.Add($preauthApplication)
    Update-MgApplication -ApplicationId $currentAppObjectId `
        -Api @{PreAuthorizedApplications = @($preauthApplications)}
    Write-Host "  Added pre-authorized applications."

    # Add required permissions to graph
    $requiredResourcesAccess = New-Object `
        System.Collections.Generic.List[Microsoft.Graph.PowerShell.Models.MicrosoftGraphRequiredResourceAccess]
    $requiredPermissions = GetRequiredPermissions -applicationDisplayName "Microsoft Graph" `
        -requiredDelegatedPermissions "User.Read"
    $requiredResourcesAccess.Add($requiredPermissions)
    Update-MgApplication -ApplicationId $currentAppObjectId `
        -RequiredResourceAccess $requiredResourcesAccess

    # create the service principal of the newly created application
    $currentServicePrincipal = Get-MgServicePrincipal -Filter "AppId eq '$($currentAppId)'"
    if (!$currentServicePrincipal) {
        $currentServicePrincipal = New-MgServicePrincipal -AppId $currentAppId `
            -Tags { WindowsAzureActiveDirectoryIntegratedApp }
        Write-Host "  Added service principal."
    }

    # Add current user as Writer, Approver and Administrator for service application
    if ($user) {
        $allAppRoles = $currentServicePrincipal.AppRoles
        $app_role_name = "Readers"
        $app_role = $allAppRoles | Where-Object { $_.DisplayName -eq $app_role_name }
        if ($app_role) {
            New-MgUserAppRoleAssignment -UserId $user.Id -PrincipalId $user.Id `
                -ResourceId $currentServicePrincipal.Id -AppRoleID $app_role.Id `
                -ErrorAction SilentlyContinue | Out-Null
            Write-Host "  Assigned Reader role to $($user.UserPrincipalName)."
        }
        $app_role_name = "Writers"
        $app_role = $allAppRoles | Where-Object { $_.DisplayName -eq $app_role_name }
        if ($app_role) {
            New-MgUserAppRoleAssignment -UserId $user.Id -PrincipalId $user.Id `
                -ResourceId $currentServicePrincipal.Id -AppRoleID $app_role.Id `
                -ErrorAction SilentlyContinue | Out-Null
            Write-Host "  Assigned Writer role to $($user.UserPrincipalName)."
        }
        $app_role_name = "Administrators"
        $app_role = $allAppRoles | Where-Object { $_.DisplayName -eq $app_role_name }
        if ($app_role) {
            New-MgUserAppRoleAssignment -UserId $user.Id -PrincipalId $user.Id `
                -ResourceId $currentServicePrincipal.Id -AppRoleID $app_role.Id `
                -ErrorAction SilentlyContinue | Out-Null
            Write-Host "  Assigned Admin role to $($user.UserPrincipalName)."
        }
    }

    #
    # Update client application to add and if possible grant required permissions.
    #
    $currentAppId = $clientAadApplication.AppId
    $currentAppObjectId = $clientAadApplication.Id
    $requiredResourcesAccess = New-Object `
        System.Collections.Generic.List[Microsoft.Graph.PowerShell.Models.MicrosoftGraphRequiredResourceAccess]
    #$requiredPermissions = GetRequiredPermissions -applicationDisplayName $serviceDisplayName `
    #    -requiredDelegatedPermissions "user_impersonation"
    #$requiredResourcesAccess.Add($requiredPermissions)
    #$requiredPermissions = GetRequiredPermissions -applicationDisplayName $serviceDisplayName `
    #    -requiredDelegatedPermissions "Read"
    #$requiredResourcesAccess.Add($requiredPermissions)
    $requiredPermissions = GetRequiredPermissions -applicationDisplayName $serviceDisplayName `
        -requiredDelegatedPermissions "ReadWrite"
    $requiredResourcesAccess.Add($requiredPermissions)
    $requiredPermissions = GetRequiredPermissions -applicationDisplayName "Microsoft Graph" `
        -requiredDelegatedPermissions "User.Read"
    $requiredResourcesAccess.Add($requiredPermissions)
    Update-MgApplication -ApplicationId $currentAppObjectId `
        -RequiredResourceAccess $requiredResourcesAccess
    Write-Host "Client application '$($clientDisplayName)' updated with required resource access."
    # Grant permissions to client
    try {
        Add-AdminConsentGrant -azureAppId $currentAppId | Out-Null
        Write-Host "  Admin consent granted to native client application."
    }
    catch {
        Write-Host $_.Exception.Message
        Write-Host "You must grant admin consent for application '$($clientDisplayName)' manually inside Azure Active Directory."
    }

    #
    # Update web application to add and if possible grant required permissions.
    #
    $currentAppId = $webAadApplication.AppId
    $currentAppObjectId = $webAadApplication.Id
    $requiredResourcesAccess = New-Object `
        System.Collections.Generic.List[Microsoft.Graph.PowerShell.Models.MicrosoftGraphRequiredResourceAccess]
    #$requiredPermissions = GetRequiredPermissions -applicationDisplayName $serviceDisplayName `
    #    -requiredDelegatedPermissions "user_impersonation"
    #$requiredResourcesAccess.Add($requiredPermissions)
    #$requiredPermissions = GetRequiredPermissions -applicationDisplayName $serviceDisplayName `
    #    -requiredDelegatedPermissions "Read"
    #$requiredResourcesAccess.Add($requiredPermissions)
    # Also require admin as app role to run end to end tests.  TODO: Split out into seperate client app
    $requiredPermissions = GetRequiredPermissions -applicationDisplayName $serviceDisplayName `
        -requiredDelegatedPermissions "ReadWrite" -requiredApplicationPermissions "Admin"
    $requiredResourcesAccess.Add($requiredPermissions)
    $requiredPermissions = GetRequiredPermissions -applicationDisplayName "Microsoft Graph" `
        -requiredDelegatedPermissions "User.Read"
    $requiredResourcesAccess.Add($requiredPermissions)
    Update-MgApplication -ApplicationId $currentAppObjectId `
        -RequiredResourceAccess $requiredResourcesAccess
    Write-Host "Web application '$($webDisplayName)' updated with required resource access."

    # Grant permissions to web app
    try {
        Add-AdminConsentGrant -azureAppId $webAadApplication.AppId | Out-Null
        Write-Host "  Admin consent granted to web application."
    }
    catch {
        Write-Host $_.Exception.Message
        Write-Host "You must grant admin consent for application '$($webDisplayName)' manually inside Azure Active Directory."
    }

    return [pscustomobject] @{
        TenantId           = $tenantId
        Authority          = $authority
        Audience           = $serviceAadApplication.IdentifierUris[0].ToString()

        ServiceId          = $serviceAadApplication.AppId
        ServicePrincipalId = $serviceAadApplication.Id
        ServiceSecret      = $serviceSecret.SecretText
        ServiceDisplayName = $serviceDisplayName

        ClientId           = $clientAadApplication.AppId
        ClientPrincipalId  = $clientAadApplication.Id
        ClientDisplayName  = $clientDisplayName

        WebAppId           = $webAadApplication.AppId
        WebAppPrincipalId  = $webAadApplication.Id
        WebAppSecret       = $webAppSecret.SecretText
        WebAppDisplayName  = $webDisplayName

        UserPrincipalId    = $user.Id
    }
}

#*******************************************************************************************************
# Script body
#*******************************************************************************************************
$ErrorActionPreference = 'Stop'
$script:ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

# Import modules
Import-Module Az.Accounts

try {
    Import-Module Microsoft.Graph.Authentication -MinimumVersion 2.0.0
    Import-Module Microsoft.Graph.Identity.DirectoryManagement -MinimumVersion 2.0.0
    Import-Module Microsoft.Graph.Applications -MinimumVersion 2.0.0
    Import-Module Microsoft.Graph.Groups -MinimumVersion 2.0.0
    Import-Module Microsoft.Graph.Users -MinimumVersion 2.0.0
}
catch {
    $ex = $_

    Write-Host
    Write-Host "An error occurred: $($ex.Exception.Message)"
    Write-Host
    Write-Host "Ensure you have installed the Microsoft.Graph cmdlets:"
    Write-Host "1) Run PowerShell as an administrator"
    Write-Host "2) In the PowerShell window, type: Install-Module Microsoft.Graph"
    Write-Host

    throw $ex
}

if (!$script:Context) {
    $script:Context = Select-Context -environmentName $script:EnvironmentName
    $script:interactive = $true
}
else {
    Write-Host "Using passed context (Account $($script:Context.Account), Tenant $($script:Context.Tenant.Id))"
    $script:interactive = $false
}

try {
    Connect-MicrosoftGraph -context $context
}
catch {
    Write-Warning "Failed to sign into Microsoft Graph for $($script:Name): $($_.Exception.Message)"
    return $null
}

try {
    $aadConfig = ConfigureApplications -applicationName $script:Name -context $context
    $aadConfigJson = $aadConfig | ConvertTo-Json

    if ($isCloudShell) {
        Write-Host "aadConfig:"
        Write-Host $aadConfigJson
    }
    else {
        Write-Verbose $aadConfigJson
    }

    if ($script:Output) {
        $aadConfigJson | Out-File $script:Output
        return
    }
    return $aadConfig
}
catch {
    Write-Warning "Failed to register applications for $($script:Name): $($_.Exception.Message)"
    return $null
}
finally {
    Disconnect-MgGraph | Out-Null
}
