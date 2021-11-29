<#
 .SYNOPSIS
    Registers required applications.

 .DESCRIPTION
    Registers the required applications in AAD and returns an object containing the information.

 .PARAMETER Name
    The Name prefix under which to register the applications.

 .PARAMETER TenantId
    The Azure Active Directory tenant to use.

 .PARAMETER ReplyUrl
    A reply_url to register, e.g. https://<NAME>.azurewebsites.net/

 .PARAMETER Context
    A previously created az context (optional).
#>

param(
    [Parameter(Mandatory = $true)] [string] $Name,
    $Context,
    [string] $TenantId = $null,
    [string] $ReplyUrl = $null,
    [string] $Output = $null,
    [string] $EnvironmentName = "AzureCloud"
)

#*******************************************************************************************************
# Perform login - uses profile file if exists - returns account
#*******************************************************************************************************
Function Select-Context() {
    [OutputType([Microsoft.Azure.Commands.Profile.Models.Core.PSAzureContext])]
    Param(
        [string] $environmentName,
        [Microsoft.Azure.Commands.Profile.Models.Core.PSAzureContext] $context
    )

    if ($context) {
        Write-Host "Using passed context (Account $($context.Account), Tenant $($context.Tenant.Id))"
        return $context
    }

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
            -Environment $environmentName @tenantIdArg `
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
            # for the case where prompting web browser is not applicable, we will prompt for DeviceAuthentication login.
            $azProfile = Connect-AzAccount `
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

#******************************************************************************
# find the top most folder with solution in it
#******************************************************************************
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

#*******************************************************************************************************
# Adds the requiredAccesses (expressed as a pipe separated string) to the requiredAccess structure
#*******************************************************************************************************
Function Add-ResourcePermission() {
    Param (
        $requiredAccess,
        $exposedPermissions,
        [string] $requiredAccesses,
        [string] $permissionType
    )
    foreach ($permission in $requiredAccesses.Trim().Split("|")) {
        foreach ($exposedPermission in $exposedPermissions) {
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
# Example: Get-RequiredPermissions "Microsoft Graph" "Graph.Read|User.Read"
#*******************************************************************************************************
Function Get-RequiredPermissions() {
    Param(
        [string] $applicationDisplayName,
        [string] $requiredDelegatedPermissions
    )

    for ($i = 0; $i -lt 20; $i++) {
        try {
            $requiredAccess = New-Object Microsoft.Open.AzureAD.Model.RequiredResourceAccess
            $sp = Get-AzureADServicePrincipal -Filter "DisplayName eq '$($applicationDisplayName)'"
            if (!$sp) {
                Write-Warning "Service principal $($applicationDisplayName) not found."
            }
            else {
                $requiredAccess.ResourceAppId = $sp.AppId.Trim().Split(" ")[0]
                $requiredAccess.ResourceAccess =
                New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.ResourceAccess]

                if ($requiredDelegatedPermissions) {
                    Add-ResourcePermission $requiredAccess -exposedPermissions $sp.Oauth2Permissions `
                        -requiredAccesses $requiredDelegatedPermissions -permissionType "Scope"
                }
                if ($requiredApplicationPermissions) {
                    Add-ResourcePermission $requiredAccess -exposedPermissions $sp.AppRoles `
                        -requiredAccesses $requiredApplicationPermissions -permissionType "Role"
                }
            }
            return $requiredAccess
        }
        catch {
            Write-Host "$($_.Exception.Message) for $($applicationDisplayName) - Retrying..."
            Start-Sleep -s 1
        }
    }
    throw "Failed to get resource permissions for $($applicationDisplayName)."
}

#*******************************************************************************************************
# Create an application role of given name and description
#*******************************************************************************************************
Function New-AppRole() {
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
        $appRole.Id = [guid]::NewGuid()
        $appRole.IsEnabled = $true
        $appRole.Value = $value;
    }
    $appRole.DisplayName = $name
    $appRole.Description = $description
    return $appRole
}

#*******************************************************************************************************
# Get configuration object for service, web and client applications
#*******************************************************************************************************
Function New-ADApplications() {
    param(
        [Microsoft.Azure.Commands.Profile.Models.Core.PSAzureContext] $context,
        [string] $applicationName
    )
    try {
        $tenantId = $context.Tenant.Id

        if (![string]::IsNullOrEmpty($script:TenantId)) {
            $tenantId = $script:TenantId
        }

        try {
            $creds = Connect-AzureADAlias `
                -AzureEnvironmentName $context.Environment.Name `
                -TenantId $tenantId `
                -AccountId $context.Account.Id `
                -Credential $context.Account.Credential
        }
        catch {
            # For some accounts $context.Account.Id may be first.last@something.com which might
            # not be correct UserPrincipalName. In those cases we will prompt for another login.
            try {
                $creds = Connect-AzureADAlias `
                    -AzureEnvironmentName $context.Environment.Name `
                    -TenantId $tenantId `
                }
            catch {
                Write-Host "Failed collecting user credentials while registering $($applicationDisplayName): $($_.Exception.Message)"
            }
        }

        if (!$creds) {
            return $null
        }

        $tenant = Get-AzureADTenantDetail
        $defaultTenant = ($tenant.VerifiedDomains | Where-Object { $_._Default -eq $True })
        $tenantName = $defaultTenant.Name
        Write-Host "Selected Tenant '$tenantName' as authority."

        # Try to get current user
        try {
            try {
                $user = Get-AzureADUser -ObjectId $creds.Account.Id -ErrorAction Stop
            }
            catch {
                try {
                    $user = (Get-AzureADUser -SearchString $creds.Account.Id)[0]
                }
                catch {
                    Write-Host "Failed getting user principal for $($creds.Account.Id) while searching by account ID."
                }
            }
        }
        catch {
            Write-Verbose "Getting user principal for $($creds.Account.Id) failed."
        }

        # May be using authTenantId, get ObjectId from external account.
        if(!$user) {
            $accountId = $context.Account.Id.Replace("@", "_")
            $adUser = (Get-AzureADUser -Filter "startswith(userPrincipalName, '$($accountId)')")

            $properties = @{
                UserPrincipalName = $adUser.UserPrincipalName
                ObjectId = $adUser.ObjectId
            }
            $user = New-Object psobject -Property $properties
            $fallBackUser = $user.ObjectId
        }

        # Get or create native client application
        $clientDisplayName = $applicationName + "-client"
        $clientAadApplication = Get-AzureADApplication -Filter "DisplayName eq '$clientDisplayName'"
        if (!$clientAadApplication) {
            $clientAadApplication = New-AzureADApplication -DisplayName $clientDisplayName -PublicClient $True
            Write-Host "Created new AAD client application '$($clientDisplayName)' in Tenant '$($tenantName)'."
            if ($user) {
                Write-Host "Adding '$($user.UserPrincipalName)' as owner ..."
                try {
                    Add-AzureADApplicationOwner -ObjectId $clientAadApplication.ObjectId `
                        -RefObjectId $user.ObjectId
                }
                catch {
                    Write-Verbose "Adding $($user.UserPrincipalName) as owner failed."
                }
            }
        }

        # Find client service principal
        $clientServicePrincipal = Get-AzureADServicePrincipal -Filter "AppId eq '$($clientAadApplication.AppId)'"
        if (!$clientServicePrincipal) {
            $clientServicePrincipal = New-AzureADServicePrincipal -AppId $clientAadApplication.AppId `
                -Tags { WindowsAzureActiveDirectoryIntegratedApp }
        }

        # Get or create web application
        $webDisplayName = $applicationName + "-web"
        $webAadApplication = Get-AzureADApplication -Filter "DisplayName eq '$webDisplayName'"
        if (!$webAadApplication) {
            $webAadApplication = New-AzureADApplication -DisplayName $webDisplayName 
            Write-Host "Created new AAD web app '$($webDisplayName)' in Tenant '$($tenantName)'."
            if ($user) {
                Write-Host "Adding '$($user.UserPrincipalName)' as owner ..."
                try {
                    Add-AzureADApplicationOwner -ObjectId $webAadApplication.ObjectId `
                        -RefObjectId $user.ObjectId
                }
                catch {
                    Write-Verbose "Adding $($user.UserPrincipalName) as owner failed."
                }
            }
        }
        $webSecret = New-AzureADApplicationPasswordCredential -ObjectId $webAadApplication.ObjectId `
            -CustomKeyIdentifier "Client Key" -EndDate (get-date).AddYears(2)

        # Find web service principal
        $webServicePrincipal = Get-AzureADServicePrincipal -Filter "AppId eq '$($webAadApplication.AppId)'"
        if (!$webServicePrincipal) {
            $webServicePrincipal = New-AzureADServicePrincipal -AppId $webAadApplication.AppId `
                -Tags { WindowsAzureActiveDirectoryIntegratedApp }
        }

        # Get or create service application
        $serviceDisplayName = $applicationName + "-service"
        $serviceAadApplication = Get-AzureADApplication `
            -Filter "identifierUris/any(uri:uri eq 'https://$tenantName/$serviceDisplayName')"
        if (!$serviceAadApplication) {
            $serviceAadApplication = New-AzureADApplication -DisplayName $serviceDisplayName `
                -PublicClient $False -HomePage "https://localhost" `
                -IdentifierUris "https://$tenantName/$serviceDisplayName"
            Write-Host "Created new AAD service application '$($serviceDisplayName)' in Tenant '$($tenantName)'."
            if ($user) {
                Write-Host "Adding '$($user.UserPrincipalName)' as owner ..."
                try {
                    Add-AzureADApplicationOwner -ObjectId $serviceAadApplication.ObjectId `
                        -RefObjectId $user.ObjectId
                }
                catch {
                    Write-Verbose "Adding $($user.UserPrincipalName) as owner failed."
                }
            }
        }
        $serviceSecret = New-AzureADApplicationPasswordCredential -ObjectId $serviceAadApplication.ObjectId `
            -CustomKeyIdentifier "Service Key" -EndDate (get-date).AddYears(2)

        # Find service principal
        $serviceServicePrincipal = Get-AzureADServicePrincipal -Filter "AppId eq '$($serviceAadApplication.AppId)'"
        if (!$serviceServicePrincipal) {
            $serviceServicePrincipal = New-AzureADServicePrincipal -AppId $serviceAadApplication.AppId `
                -Tags { WindowsAzureActiveDirectoryIntegratedApp }
        }

        # Update service application to add roles, known applications and required permissions
        $approverRole = New-AppRole -current $serviceAadApplication.AppRoles -name "Approver" `
            -value "Sign" -description "Approvers have the ability to issue certificates."
        $writerRole = New-AppRole -current $serviceAadApplication.AppRoles -name "Writer" `
            -value "Write" -description "Writers Have the ability to change entities."
        $adminRole = New-AppRole -current $serviceAadApplication.AppRoles -name "Administrator" `
            -value "Admin" -description "Admins can access advanced features."
        $appRoles = New-Object `
            System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.AppRole]
        $appRoles.Add($writerRole)
        $appRoles.Add($approverRole)
        $appRoles.Add($adminRole)

        $requiredResourcesAccess = `
            New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.RequiredResourceAccess]
        $requiredPermissions = Get-RequiredPermissions -applicationDisplayName "Microsoft Graph" `
            -requiredDelegatedPermissions "User.Read"
        $requiredResourcesAccess.Add($requiredPermissions)

        $knownApplications = New-Object System.Collections.Generic.List[System.String]
        $knownApplications.Add($clientAadApplication.AppId)
        $knownApplications.Add($webAadApplication.AppId)
        $knownApplications.Add("04b07795-8ddb-461a-bbee-02f9e1bf7b46")
        $knownApplications.Add("872cd9fa-d31f-45e0-9eab-6e460a02d1f1")

        Set-AzureADApplication -ObjectId $serviceAadApplication.ObjectId `
            -KnownClientApplications $knownApplications `
            -AppRoles $appRoles `
            -RequiredResourceAccess $requiredResourcesAccess | Out-Null

        # Read updated service principal
        $serviceServicePrincipal = Get-AzureADServicePrincipal -Filter "AppId eq '$($serviceAadApplication.AppId)'"

        # Add 1) Azure CLI and 2) Visual Studio to allow log onto the platform with them as clients
        Add-PreauthorizedApplication -servicePrincipal $serviceServicePrincipal `
            -azureAppObjectId $serviceAadApplication.ObjectId `
            -azurePreAuthAppId "04b07795-8ddb-461a-bbee-02f9e1bf7b46" -context $context | Out-Null
        Add-PreauthorizedApplication -servicePrincipal $serviceServicePrincipal `
            -azureAppObjectId $serviceAadApplication.ObjectId `
            -azurePreAuthAppId "872cd9fa-d31f-45e0-9eab-6e460a02d1f1" -context $context | Out-Null

        Write-Host "'$($serviceDisplayName)' updated with required resource access, app roles and known applications."

        # Add current user as Writer, Approver and Administrator for service application
        try {
            $app_role_name = "Writer"
            $app_role = $serviceServicePrincipal.AppRoles | Where-Object { $_.DisplayName -eq $app_role_name }
            New-AzureADUserAppRoleAssignment -ObjectId $user.ObjectId -PrincipalId $user.ObjectId `
                -ResourceId $serviceServicePrincipal.ObjectId -Id $app_role.Id | Out-Null

            $app_role_name = "Approver"
            $app_role = $serviceServicePrincipal.AppRoles | Where-Object { $_.DisplayName -eq $app_role_name }
            New-AzureADUserAppRoleAssignment -ObjectId $user.ObjectId -PrincipalId $user.ObjectId `
                -ResourceId $serviceServicePrincipal.ObjectId -Id $app_role.Id | Out-Null

            $app_role_name = "Administrator"
            $app_role = $serviceServicePrincipal.AppRoles | Where-Object { $_.DisplayName -eq $app_role_name }
            New-AzureADUserAppRoleAssignment -ObjectId $user.ObjectId -PrincipalId $user.ObjectId `
                -ResourceId $serviceServicePrincipal.ObjectId -Id $app_role.Id | Out-Null

            Write-Host "Assigned all app roles to user $($user.UserPrincipalName)."
        }
        catch {
            Write-Host "User $($user.UserPrincipalName) already has all app roles assigned."
        }

        # Update client application to add reply urls required permissions.
        $replyUrls = New-Object System.Collections.Generic.List[System.String]
        $replyUrls.Add("urn:ietf:wg:oauth:2.0:oob")
        $replyUrls.Add("https://localhost")
        $replyUrls.Add("http://localhost")
        $requiredResourcesAccess = `
            New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.RequiredResourceAccess]
        $requiredPermissions = Get-RequiredPermissions -applicationDisplayName $serviceDisplayName `
            -requiredDelegatedPermissions "user_impersonation" # "Directory.Read.All|User.Read"
        $requiredResourcesAccess.Add($requiredPermissions)
        $requiredPermissions = Get-RequiredPermissions -applicationDisplayName "Microsoft Graph" `
            -requiredDelegatedPermissions "User.Read"
        $requiredResourcesAccess.Add($requiredPermissions)

        Set-AzureADApplication -ObjectId $clientAadApplication.ObjectId `
            -RequiredResourceAccess $requiredResourcesAccess -ReplyUrls $replyUrls `
            -Oauth2AllowImplicitFlow $False -Oauth2AllowUrlPathMatching $True | Out-Null
        # Grant permissions to native client
        try {
            Add-AdminConsentGrant -azureAppId $clientAadApplication.AppId -context $context | Out-Null
            Write-Host "Admin consent granted to native client application."
        }
        catch {
            Write-Host "$($_.Exception) - this must be done manually with appropriate permissions."
        }
        Write-Host "'$($clientDisplayName)' updated with required resource access."

        $replyUrls = New-Object System.Collections.Generic.List[System.String]
        $replyUrl = $script:ReplyUrl
        if (![string]::IsNullOrEmpty($replyUrl)) {
            # Append "/" if necessary.
            $replyUrl = If ($replyUrl.Substring($replyUrl.Length - 1, 1) -eq "/") { $replyUrl } Else {$replyUrl + "/"}
            $replyUrls.Add("$($replyUrl)signin-oidc")
            Write-Host "Registering $($replyUrl) as reply URL ..."
        }

        Set-AzureADApplication -ObjectId $webAadApplication.ObjectId `
            -RequiredResourceAccess $requiredResourcesAccess -ReplyUrls $replyUrls `
            -Oauth2AllowImplicitFlow $True -Oauth2AllowUrlPathMatching $True | Out-Null
        # Grant permissions to web app
        try {
            Add-AdminConsentGrant -azureAppId $webAadApplication.AppId -context $context | Out-Null
            Write-Host "Admin consent granted to web application."
        }
        catch {
            Write-Host "$($_.Exception) - this must be done manually with appropriate permissions."
        }
        Write-Host "'$($webDisplayName)' updated with required resource access."

        # Reset ObjectId to use the one from the default tenant.
        if($null -ne $fallBackUser) {
            $user.ObjectId = $null
        }

        return [pscustomobject] @{
            TenantId           = $creds.Tenant.Id
            Authority          = $context.Environment.ActiveDirectoryAuthority
            Audience           = $serviceAadApplication.IdentifierUris[0].ToString()

            ServiceId          = $serviceAadApplication.AppId
            ServicePrincipalId = $serviceServicePrincipal.ObjectId
            ServiceSecret      = $serviceSecret.Value
            ServiceDisplayName = $serviceDisplayName

            ClientId           = $clientAadApplication.AppId
            ClientPrincipalId  = $clientAadApplication.ObjectId
            ClientDisplayName  = $clientDisplayName

            WebAppId           = $webAadApplication.AppId
            WebAppPrincipalId  = $webAadApplication.ObjectId
            WebAppSecret       = $webSecret.Value
            WebAppDisplayName  = $webDisplayName

            UserPrincipalId    = $user.ObjectId
            FallBackPrincipalId= $fallBackUser
        }
    }
    catch {
        $ex = $_

        Write-Host
        Write-Host "An error occurred: $($ex.Exception.Message)"
        Write-Host
        Write-Host "Ensure you have installed the AzureAD cmdlets:"
        Write-Host "1) Run PowerShell as an administrator"
        Write-Host "2) In the PowerShell window, type: Install-Module AzureAD"
        Write-Host

        throw $ex
    }
}

#*******************************************************************************************************
# Grant consent to to the app with id = $azureAppId
#*******************************************************************************************************
Function Add-AdminConsentGrant() {
    Param(
        [string] $azureAppId,
        [Microsoft.Azure.Commands.Profile.Models.Core.PSAzureContext] $context
    )

    $url = "https://main.iam.ad.ext.azure.com/api/RegisteredApplications/$($azureAppId)/Consent?onBehalfOfAll=true"
    for ($i = 0; $i -lt 10; $i++) {
        # Try 10 times * 3 s = 30 s
        $token = [Microsoft.Azure.Commands.Common.Authentication.AzureSession]::Instance.AuthenticationFactory.Authenticate( `
            $context.Account, $context.Environment, $context.Tenant.Id, $null, "Never", $null, "74658136-14ec-4630-ad9b-26e160ff0fc6")

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

#*******************************************************************************************************
# Add preauthorize application
#*******************************************************************************************************
Function Add-PreauthorizedApplication() {
    Param(
        $servicePrincipal,
        [string] $azureAppObjectId,
        [string] $azurePreAuthAppId,
        [Microsoft.Azure.Commands.Profile.Models.Core.PSAzureContext] $context
    )

    $url = "https://graph.microsoft.com/beta/applications/" + $azureAppObjectId
    for ($i = 0; $i -lt 3; $i++) {
        $token = [Microsoft.Azure.Commands.Common.Authentication.AzureSession]::Instance.AuthenticationFactory.Authenticate( `
            $context.Account, $context.Environment, $context.Tenant.Id, $null, "Never", $null, "https://graph.microsoft.com")
        if (!$token) {
            break
        }
        try {
            $header = @{
                "Authorization" = "Bearer $($token.AccessToken)"
                "Content-Type"  = "application/json"
            }
            $preAuthBody = "{`"api`": {`"preAuthorizedApplications`": [{`"appId`": `"" + $azurePreAuthAppId + "`","
            $preAuthBody += "`"permissionIds`": [" 
            foreach ($permission in $servicePrincipal.Oauth2Permissions) {
                $preAuthBody += "`"" + $permission.Id + "`","
            }
            $preAuthBody = $preAuthBody.Trim(',');
            $preAuthBody += "]}]}}"
            Invoke-RestMethod -Uri $url -Method PATCH -Body $preAuthBody -Headers $header
            # success
            return
        }
        catch {
            Write-Verbose "$($_.Exception.Message) at $($url) - Retrying..."
            Start-Sleep -s 3
        }
    }
    Write-Verbose "Give up - manually pre-authorize application $($azurePreAuthAppId) in Expose Api..."
}

#*******************************************************************************************************
# Script body
#*******************************************************************************************************
$ErrorActionPreference = "Stop"
$script:ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

# Import Azure tools
Import-Module Az
# For CloudShell
$isCloudShell = $false
if (Get-Module -ListAvailable -Name "AzureAD.Standard.Preview") {
    Write-Host "Importing module AzureAD.Standard.Preview"
    Import-Module "AzureAD.Standard.Preview"
    $isCloudShell = $true

    # Azure cloud shell deliberately hided Connect-AzureAD cmdlet in a 2020 Feb update and used a wrapper with identical
    # name Connect-AzureAD. This change pollutes cmdlet names in this script. The following line fixes the issue:
    # Create an alias for Connect-AzureAD cmdlet depending on the version of AAD module
    # so that the cmdlet could be used easier later
    Set-Alias -Name Connect-AzureADAlias -Value "AzureAD.Standard.Preview\Connect-AzureAD"

# For Windows PowerShell
} elseif (Get-Module -ListAvailable -Name "AzureADPreview")  {
    Write-Host "Importing module AzureADPreview"
    Import-Module "AzureADPreview"
    Set-Alias -Name Connect-AzureADAlias -Value "AzureADPreview\Connect-AzureAD"
} elseif (Get-Module -ListAvailable -Name "AzureAD")  {
    Write-Host "Importing module AzureAD"
    Import-Module  "AzureAD"
    Set-Alias -Name Connect-AzureADAlias -Value "AzureAD\Connect-AzureAD"
} else {
    throw "This script is not compatible with your computer, please use Azure CloudShell https://shell.azure.com/powershell"
}

$selectedContext = Select-Context `
    -context $script:Context `
    -environmentName $script:EnvironmentName

$aadConfig = New-ADApplications `
    -applicationName $script:Name `
    -context $selectedContext

$aadConfigJson = $aadConfig | ConvertTo-Json

if($isCloudShell) {
    Write-Host "aadConfig:"
    Write-Host $aadConfigJson
}

if ($script:Output) {
    $aadConfigJson | Out-File $script:Output
    return
}

return $aadConfig
