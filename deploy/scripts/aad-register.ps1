<#
 .SYNOPSIS
    Registers required applications

 .DESCRIPTION
    Registers required applications in aad and returns object containing information

 .PARAMETER Name
    The Name prefix under which to register the applications

 .PARAMETER Context
    A previously created az context (optional)
#>

param(
    [Parameter(Mandatory = $true)] [string] $Name,
    $Context,
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
        return $context
    }

    $rootDir = Get-RootFolder $script:ScriptDir
    $contextFile = Join-Path $rootDir ".user"
    if (Test-Path $contextFile) {
        # Migrate .user file into root (next to .env)
        if (!(Test-Path $contextFile)) {
            $oldFile = Join-Path $script:ScriptDir ".user"
            if (Test-Path $oldFile) {
                Move-Item -Path $oldFile -Destination $contextFile
            }
        }
        $profile = Import-AzContext -Path $contextFile
        if ($profile.Context) {
            return $profile.Context
        }
    }
    try {
        $profile = Connect-AzAccount `
            -Environment $environmentName `
            -ErrorAction Stop

        $reply = Read-Host -Prompt "Save credention in .user file [y/n]"
        if ($reply -match "[yY]") {
            Save-AzContext -Path $contextFile -Profile $connection
        }
        return $profile.Context
    }
    catch {
        throw "The login to the Azure account was not successful."
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
        try {
            $creds = Connect-AzureAD `
                -AzureEnvironmentName $context.Environment.Name `
                -TenantId $context.Tenant.Id `
                -AccountId $context.Account.Id `
                -Credential $context.Account.Credential
        }
        catch {
            # For some accounts $context.Account.Id may be first.last@something.com which might 
            # not be correct UserPrincipalName. In those cases we will prompt for another login.
            $creds = Connect-AzureAD `
                -AzureEnvironmentName $context.Environment.Name `
                -TenantId $context.Tenant.Id `
        
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
                $user = (Get-AzureADUser -SearchString $creds.Account.Id)[0]
            }
        }
        catch {
            Write-Verbose "Getting user principal for $($creds.Account.Id) failed."
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

        Set-AzureADApplication -ObjectId $webAadApplication.ObjectId `
            -RequiredResourceAccess $requiredResourcesAccess `
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
        }
    }
    catch {
        $ex = $_

        Write-Host
        Write-Host "An error occurred: $($ex.Exception.Message)"
        Write-Host
        Write-Host "Ensure you have installed the AzureAD cmdlets:"
        Write-Host "1) Run Powershell as an administrator"
        Write-Host "2) in the PowerShell window, type: Install-Module AzureAD"
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
    for ($i = 0; $i -lt 20; $i++) {
        # try 20 times * 3 seconds = 1 minute
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
            # success
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

Import-Module Az
try {
    # Try first release version
    Import-Module AzureAD
}
catch {
    # Fallback to preview
    Import-Module AzureAD.Standard.Preview
}

$selectedContext = Select-Context `
    -context $script:Context `
    -environmentName $script:EnvironmentName

$aadConfig = New-ADApplications `
    -applicationName $script:Name `
    -context $selectedContext

if ($script:Output) {
    $aadConfig | ConvertTo-Json | Out-File $script:Output
    return
}
return $aadConfig
