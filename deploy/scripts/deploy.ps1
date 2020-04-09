<#
 .SYNOPSIS
    Deploys Industrial IoT services to Azure

 .DESCRIPTION
    Deploys the Industrial IoT services dependencies and optionally micro services and UI to Azure.

 .PARAMETER type
    The type of deployment (minimum, local, services, app, all)

 .PARAMETER version
    Set to "preview" or another mcr image tag to deploy - if not set deploys last released images ("latest").

 .PARAMETER version
    Set to "preview" or another mcr image tag to deploy - if not set deploys last released images ("latest").

 .PARAMETER resourceGroupName
    Can be the name of an existing or a new resource group

 .PARAMETER resourceGroupLocation
    Optional, a resource group location. If specified, will try to create a new resource group in this location.

 .PARAMETER subscriptionId
    Optional, the subscription id where resources will be deployed.

 .PARAMETER subscriptionName
    Or alternatively the subscription name.

 .PARAMETER accountName
    The account name to use if not to use default.

 .PARAMETER applicationName
    The name of the application if not local deployment.

 .PARAMETER aadConfig
    The aad configuration object (use aad-register.ps1 to create object).  If not provides calls aad-register.ps1.

 .PARAMETER context
    A previously created az context to be used as authentication.

 .PARAMETER aadApplicationName
    The application name to use when registering aad application.  If not set, uses applicationName

 .PARAMETER acrRegistryName
    An optional name of a Azure container registry to deploy containers from.

 .PARAMETER acrSubscriptionName
    The subscription of the container registry if differemt from the specified subscription.

 .PARAMETER environmentName
    The cloud environment to use (defaults to Azure Cloud).
#>

param(
    [ValidateSet("minimum", "local", "services", "app", "all")] [string] $type = "all",
    [string] $version,
    [string] $applicationName,
    [string] $resourceGroupName,
    [string] $resourceGroupLocation,
    [string] $subscriptionName,
    [string] $subscriptionId,
    [string] $accountName,
    [string] $aadApplicationName,
    [string] $acrRegistryName,
    [string] $acrSubscriptionName,
    $aadConfig,
    $context = $null,
    [switch] $testAllDeploymentOptions,
    [string] $environmentName = "AzureCloud"
)

#*******************************************************************************************************
# Login and select subscription to deploy into
#*******************************************************************************************************
Function Select-Context() {
    [OutputType([Microsoft.Azure.Commands.Profile.Models.Core.PSAzureContext])]
    Param(
        $environment,
        [Microsoft.Azure.Commands.Profile.Models.Core.PSAzureContext] $context
    )

    $rootDir = Get-RootFolder $script:ScriptDir
    $contextFile = Join-Path $rootDir ".user"
    if (!$context) {
        # Migrate .user file into root (next to .env)
        if (!(Test-Path $contextFile)) {
            $oldFile = Join-Path $script:ScriptDir ".user"
            if (Test-Path $oldFile) {
                Move-Item -Path $oldFile -Destination $contextFile
            }
        }
        if (Test-Path $contextFile) {
            $profile = Import-AzContext -Path $contextFile
            if (($null -ne $profile) `
                    -and ($null -ne $profile.Context) `
                    -and ($null -ne (Get-AzSubscription))) {
                $context = $profile.Context
            }
        }
    }
    if (!$context) {
        try {
            $connection = Connect-AzAccount -Environment $environment.Name `
                -ErrorAction Stop
            $context = $connection.Context
        }
        catch {
            throw "The login to the Azure account was not successful."
        }
    }

    $subscriptionDetails = $null
    if (![string]::IsNullOrEmpty($script:subscriptionName)) {
        $subscriptionDetails = Get-AzSubscription -SubscriptionName $script:subscriptionName
        if (!$subscriptionDetails -and !$script:interactive) {
            throw "Invalid subscription provided with -subscriptionName"
        }
    }

    if (!$subscriptionDetails -and ![string]::IsNullOrEmpty($script:subscriptionId)) {
        $subscriptionDetails = Get-AzSubscription -SubscriptionId $script:subscriptionId
        if (!$subscriptionDetails -and !$script:interactive) {
            throw "Invalid subscription provided with -subscriptionId"
        }
    }

    if (!$subscriptionDetails) {
        $subscriptions = Get-AzSubscription | Where-Object { $_.State -eq "Enabled" }

        if ($subscriptions.Count -eq 0) {
            throw "No active subscriptions found - exiting."
        }
        elseif ($subscriptions.Count -eq 1) {
            $subscriptionId = $subscriptions[0].Id
        }
        else {
            if (!$script:interactive) {
                throw "Provide a subscription to use using -subscriptionId or -subscriptionName"
            }
            Write-Host "Please choose a subscription from this list (using its Index):"
            $script:index = 0
            $subscriptions | Format-Table -AutoSize -Property `
                 @{Name="Index"; Expression = {($script:index++)}},`
                 @{Name="Subscription"; Expression = {$_.Name}},`
                 @{Name="Id"; Expression = {$_.SubscriptionId}}`
            | Out-Host
            while ($true) {
                $option = Read-Host ">"
                try {
                    if ([int]$option -ge 1 -and [int]$option -le $subscriptions.Count) {
                        break
                    }
                }
                catch {
                    Write-Host "Invalid index '$($option)' provided."
                }
                Write-Host "Choose from the list using an index between 1 and $($subscriptions.Count)."
            }
            $subscriptionId = $subscriptions[$option - 1].Id
        }
        $subscriptionDetails = Get-AzSubscription -SubscriptionId $subscriptionId
        if (!$subscriptionDetails) {
            throw "Failed to get details for subscription $($subscriptionId)"
        }
    }

    # Update context
    $writeProfile = $false
    if ($context.Subscription.Id -ne $subscriptionDetails.Id) {
        $context = ($subscriptionDetails | Set-AzContext)
        # If file exists - silently update profile
        $writeProfile = Test-Path $contextFile
    }
    # If file does not exist yet - ask
    if (!(Test-Path $contextFile) -and $script:interactive) {
        $reply = Read-Host -Prompt "Save credentials in .user file [y/n]"
        if ($reply -match "[yY]") {
            Write-Host ".user file will be used as persisted context."
            Write-Host "Enure you do not share it and do delete it when no longer needed."
            $writeProfile = $true
        }
    }
    if ($writeProfile) {
        Save-AzContext -Path $contextFile
    }

    Write-Host "Azure subscription $($context.Subscription.Name) ($($context.Subscription.Id)) selected."
    return $context
}

#*******************************************************************************************************
# Get private registry credentials
#*******************************************************************************************************
Function Select-RegistryCredentials() {
    # set private container registry source if provided at command line
    if ([string]::IsNullOrEmpty($script:acrRegistryName) `
            -and [string]::IsNullOrEmpty($script:acrSubscriptionName)) {
        return $null
    }

    if (![string]::IsNullOrEmpty($script:acrSubscriptionName) `
            -and ($context.Subscription.Name -ne $script:acrSubscriptionName)) {
        $acrSubscription = Get-AzSubscription -SubscriptionName $script:acrSubscriptionName
        if (!$acrSubscription) {
            Write-Warning "Specified container registry subscription $($script:acrSubscriptionName) not found."
        }
        $containerContext = Get-AzContext -ListAvailable | Where-Object {
            $_.Subscription.Name -eq $script:acrSubscriptionName
        }
    }

    if (!$containerContext) {
        # use current context
        $containerContext = $context
        Write-Host "Try using current authentication context to access container registry."
    }
    if ($containerContext.Length -gt 1) {
        $containerContext = $containerContext[0]
    }
    if ([string]::IsNullOrEmpty($script:acrRegistryName)) {
        # use default dev images repository name - see acr-build.ps1
        $script:acrRegistryName = "industrialiotdev"
    }

    Write-Host "Looking up credentials for $($script:acrRegistryName) registry."

    try {
        $registry = Get-AzContainerRegistry -DefaultProfile $containerContext `
        | Where-Object { $_.Name -eq $script:acrRegistryName }
    }
    catch {
        $registry = $null
    }
    if (!$registry) {
        Write-Warning "$($script:acrRegistryName) registry not found."
        return $null
    }
    try {
        $creds = Get-AzContainerRegistryCredential -Registry $registry `
            -DefaultProfile $containerContext
    }
    catch {
        $creds = $null
    }
    if (!$creds) {
        Write-Warning "Failed to get credentials for $($script:acrRegistryName)."
        return $null
    }
    return @{
        dockerServer   = $registry.LoginServer
        dockerUser     = $creds.Username
        dockerPassword = $creds.Password
    }
}

#*******************************************************************************************************
# Filter locations for provider and resource type
#*******************************************************************************************************
Function Select-ResourceGroupLocations() {
    param (
        $locations,
        $provider,
        $typeName
    )
    $regions = @()
    foreach ($item in $(Get-AzResourceProvider -ProviderNamespace $provider)) {
        foreach ($resourceType in $item.ResourceTypes) {
            if ($resourceType.ResourceTypeName -eq $typeName) {
                foreach ($region in $resourceType.Locations) {
                    $regions += $region
                }
            }
        }
    }
    if ($regions.Count -gt 0) {
        $locations = $locations | Where-Object {
            return $_.DisplayName -in $regions
        }
    }
    return $locations
}

#*******************************************************************************************************
# Get locations
#*******************************************************************************************************
Function Get-ResourceGroupLocations() {
    # Filter resource namespaces
    $locations = Get-AzLocation | Where-Object {
        foreach ($provider in $script:requiredProviders) {
            if ($_.Providers -notcontains $provider) {
                return $false
            }
        }
        return $true
    }

    # Filter resource types - TODO read parameters from table
    $locations = Select-ResourceGroupLocations -locations $locations `
        -provider "Microsoft.Devices" -typeName "ProvisioningServices"

    return $locations
}

#*******************************************************************************************************
# Select location
#*******************************************************************************************************
Function Select-ResourceGroupLocation() {
    $locations = Get-ResourceGroupLocations

    if (![string]::IsNullOrEmpty($script:resourceGroupLocation)) {
        foreach ($location in $locations) {
            if ($location.Location -eq $script:resourceGroupLocation -or `
                    $location.DisplayName -eq $script:resourceGroupLocation) {
                $script:resourceGroupLocation = $location.Location
                return
            }
        }
        if ($interactive) {
            throw "Location '$script:resourceGroupLocation' is not a valid location."
        }
    }
    Write-Host "Please choose a location for your deployment from this list (using its Index):"
    $script:index = 0
    $locations | Format-Table -AutoSize -property `
            @{Name="Index"; Expression = {($script:index++)}},`
            @{Name="Location"; Expression = {$_.DisplayName}} `
    | Out-Host
    while ($true) {
        $option = Read-Host -Prompt ">"
        try {
            if ([int]$option -ge 1 -and [int]$option -le $locations.Count) {
                break
            }
        }
        catch {
            Write-Host "Invalid index '$($option)' provided."
        }
        Write-Host "Choose from the list using an index between 1 and $($locations.Count)."
    }
    $script:resourceGroupLocation = $locations[$option - 1].Location
}


#*******************************************************************************************************
# Update resource group tags
#*******************************************************************************************************
Function Set-ResourceGroupTags() {
    Param(
        [string] $state,
        [string] $version
    )
    $resourceGroup = Get-AzResourceGroup -ResourceGroupName $script:resourceGroupName
    if (!$resourceGroup) {
        return
    }
    $tags = $resourceGroup.Tags
    if (!$tags) {
        $tags = @{}
    }
    $update = $false
    if (![string]::IsNullOrEmpty($state)) {
        if ($tags.ContainsKey("IoTSuiteState")) {
            if ($tags.IoTSuiteState -ne $state) {
                $tags.IoTSuiteState = $state
                $update = $true
            }
        }
        else {
            $tags += @{ "IoTSuiteState" = $state }
            $update = $true
        }
    }
    if (![string]::IsNullOrEmpty($version)) {
        if ($tags.ContainsKey("IoTSuiteVersion")) {
            if ($tags.IoTSuiteVersion -ne $version) {
                $tags.IoTSuiteVersion = $version
                $update = $true
            }
        }
        else {
            $tags += @{ "IoTSuiteVersion" = $version }
            $update = $true
        }
    }
    $type = "AzureIndustrialIoT"
    if ($tags.ContainsKey("IoTSuiteType")) {
        if ($tags.IoTSuiteType -ne $type) {
            $tags.IoTSuiteType = $type
            $update = $true
        }
    }
    else {
        $tags += @{ "IoTSuiteType" = $type }
        $update = $true
    }
    if (!$update) {
        return
    }
    $resourceGroup = Set-AzResourceGroup -Name $script:resourceGroupName -Tag $tags
}

#*******************************************************************************************************
# Get or create new resource group for deployment
#*******************************************************************************************************
Function Select-ResourceGroup() {

    $first = $true
    while ([string]::IsNullOrEmpty($script:resourceGroupName) `
            -or ($script:resourceGroupName -notmatch "^[a-z0-9-_]*$")) {
        if (!$script:interactive) {
            throw "Invalid resource group name specified which is mandatory for non-interactive script use."
        }
        if ($first -eq $false) {
            Write-Host "Use alphanumeric characters as well as '-' or '_'."
        }
        else {
            Write-Host
            Write-Host "Please provide a name for the resource group."
            $first = $false
        }
        $script:resourceGroupName = Read-Host -Prompt ">"
    }

    $resourceGroup = Get-AzResourceGroup -Name $script:resourceGroupName `
        -ErrorAction SilentlyContinue
    if (!$resourceGroup) {
        Write-Host "Resource group '$script:resourceGroupName' does not exist."
        Select-ResourceGroupLocation
        $resourceGroup = New-AzResourceGroup -Name $script:resourceGroupName `
            -Location $script:resourceGroupLocation
        Write-Host "Created new resource group $($script:resourceGroupName) in $($resourceGroup.Location)."
        Set-ResourceGroupTags -state "Created"
        return $True
    }
    else {
        Set-ResourceGroupTags -state "Updating"
        $script:resourceGroupLocation = $resourceGroup.Location
        Write-Host "Using existing resource group $($script:resourceGroupName)..."
        return $False
    }
}

#******************************************************************************
# Generate a random password
#******************************************************************************
Function New-Password() {
    param(
        $length = 15
    )
    $punc = 46..46
    $digits = 48..57
    $lcLetters = 65..90
    $ucLetters = 97..122
    $password = `
        [char](Get-Random -Count 1 -InputObject ($lcLetters)) + `
        [char](Get-Random -Count 1 -InputObject ($ucLetters)) + `
        [char](Get-Random -Count 1 -InputObject ($digits)) + `
        [char](Get-Random -Count 1 -InputObject ($punc))
    $password += get-random -Count ($length - 4) `
        -InputObject ($punc + $digits + $lcLetters + $ucLetters) |`
        ForEach-Object -begin { $aa = $null } -process { $aa += [char]$_ } -end { $aa }

    return $password
}

#******************************************************************************
# Get env file content from deployment
#******************************************************************************
Function Get-EnvironmentVariables() {
    Param(
        $deployment
    )
    if (![string]::IsNullOrEmpty($script:resourceGroupName)) {
        Write-Output "PCS_RESOURCE_GROUP=$($script:resourceGroupName)"
    }

    $var = $deployment.Outputs["keyVaultUri"].Value
    if (![string]::IsNullOrEmpty($var)) {
        Write-Output "PCS_KEYVAULT_URL=$($var)"
    }
    $var = $script:aadConfig.ServiceId
    if (![string]::IsNullOrEmpty($var)) {
        Write-Output "PCS_KEYVAULT_APPID=$($var)"
        $var = $script:aadConfig.ServiceSecret
        if (![string]::IsNullOrEmpty($var)) {
            Write-Output "PCS_KEYVAULT_SECRET=$($var)"
        }
        $var = $deployment.Outputs["tenantId"].Value
        if (![string]::IsNullOrEmpty($var)) {
            Write-Output "PCS_AUTH_TENANT=$($var)"
        }
    }

    $var = $deployment.Outputs["tsiUrl"].Value
    if (![string]::IsNullOrEmpty($var)) {
        Write-Output "PCS_TSI_URL=$($var)"
    }

    $var = $deployment.Outputs["serviceUrl"].Value
    if (![string]::IsNullOrEmpty($var)) {
        Write-Output "PCS_SERVICE_URL=$($var)"
    }
    $var = $deployment.Outputs["appUrl"].Value
    if (![string]::IsNullOrEmpty($var)) {
        Write-Output "PCS_APP_URL=$($var)"
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

#******************************************************************************
# Write or output .env file
#******************************************************************************
Function Write-EnvironmentVariables() {
    Param(
        $deployment
    )

    # find the top most folder
    $rootDir = Get-RootFolder $script:ScriptDir

    $writeFile = $false
    if ($script:interactive) {
        $ENVVARS = Join-Path $rootDir ".env"
        $prompt = "Save environment as $ENVVARS for local development? [y/n]"
        $reply = Read-Host -Prompt $prompt
        if ($reply -match "[yY]") {
            $writeFile = $true
        }
        if ($writeFile) {
            if (Test-Path $ENVVARS) {
                $prompt = "Overwrite existing .env file in $rootDir? [y/n]"
                if ( $reply -match "[yY]" ) {
                    Remove-Item $ENVVARS -Force
                }
                else {
                    $writeFile = $false
                }
            }
        }
    }
    if ($writeFile) {
        Get-EnvironmentVariables $deployment | Out-File -Encoding ascii `
            -FilePath $ENVVARS

        Write-Host
        Write-Host ".env file created in $rootDir."
        Write-Host
        Write-Warning "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
        Write-Warning "!The file contains security keys to your Azure resources!"
        Write-Warning "! Safeguard the contents of this file, or delete it now !"
        Write-Warning "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
        Write-Host
    }
    else {
        Get-EnvironmentVariables $deployment | Out-Default
    }
}

#*******************************************************************************************************
# Deploy azuredeploy.json
#*******************************************************************************************************
Function New-Deployment() {
    Param(
        $context
    )

    $templateParameters = @{ }

    if ([string]::IsNullOrEmpty($script:repo)) {
        # Try get repo name / TODO
        $script:repo = "https://github.com/Azure/Industrial-IoT"
    }

    if ([string]::IsNullOrEmpty($script:branchName)) {
        # Try get branch name
        $script:branchName = $env:BUILD_SOURCEBRANCH
        if (![string]::IsNullOrEmpty($script:branchName)) {
            if ($script:branchName.StartsWith("refs/heads/")) {
                $script:branchName = $script:branchName.Replace("refs/heads/", "")
            }
            else {
                $script:branchName = $null
            }
        }
        if ([string]::IsNullOrEmpty($script:branchName)) {
            try {
                $argumentList = @("rev-parse", "--abbrev-ref", "@{upstream}")
                $symbolic = (& "git" $argumentList 2>&1 | ForEach-Object { "$_" });
                if ($LastExitCode -ne 0) {
                    throw "git $($argumentList) failed with $($LastExitCode)."
                }
                $remote = $symbolic.Split('/')[0]
                $argumentList = @("remote", "get-url", $remote)
                $giturl = (& "git" $argumentList 2>&1 | ForEach-Object { "$_" });
                if ($LastExitCode -ne 0) {
                    throw "git $($argumentList) failed with $($LastExitCode)."
                }
                $script:repo = $giturl
                $script:branchName = $symbolic.Replace("$($remote)/", "")
                if ($script:branchName -eq "HEAD") {
                    Write-Warning "$($symbolic) is not a branch - using master."
                    $script:branchName = "master"
                }
            }
            catch {
                Write-Warning "$($_.Exception.Message).  Using master branch."
                $script:repo = "https://github.com/Azure/Industrial-IoT"
                $script:branchName = "master"
            }
        }
    }

    Set-ResourceGroupTags -state "Deploying" -version $script:branchName
    Write-Host "Deployment will use '$($script:branchName)' branch in '$($script:repo)'."
    $templateParameters.Add("branchName", $script:branchName)
    $templateParameters.Add("repoUrl", $script:repo)

    if (($script:type -eq "local") -or ($script:type -eq "minimum")) {
        if ([string]::IsNullOrEmpty($script:applicationName) `
                -or ($script:applicationName -notmatch "^[a-z0-9-]*$")) {
            $script:applicationName = $script:resourceGroupName
        }
        if ($script:type -eq "minimum") {
            $templateParameters.Add("deploymentLevel", "Minimum")
        }
    }
    else {
        $first = $true
        while ([string]::IsNullOrEmpty($script:applicationName) `
                -or ($script:applicationName -notmatch "^[a-z0-9-]*$")) {
            if (!$script:interactive) {
                throw "Invalid application name specified which is mandatory for non-interactive script use."
            }
            if ($first -eq $false) {
                Write-Host "You can only use alphanumeric characters as well as '-'."
            }
            else {
                Write-Host
                Write-Host "Please specify a name for your application."
                $first = $false
            }
            if ($script:resourceGroupName -match "^[a-z0-9-]*$") {
                Write-Host "Hit enter to use $($script:resourceGroupName)."
            }
            $script:applicationName = Read-Host -Prompt ">"
            if ([string]::IsNullOrEmpty($script:applicationName)) {
                $script:applicationName = $script:resourceGroupName
            }
        }

        if ([string]::IsNullOrEmpty($script:version)) {
            $script:version = "latest"
        }
        $templateParameters.Add("imagesTag", $script:version)
        $creds = Select-RegistryCredentials
        if ($creds) {
            $templateParameters.Add("dockerServer", $creds.dockerServer)
            $templateParameters.Add("dockerUser", $creds.dockerUser)
            $templateParameters.Add("dockerPassword", $creds.dockerPassword)

            # see acr-build.ps1 for naming logic
            $namespace = $script:branchName
            if ($namespace.StartsWith("feature/")) {
                $namespace = $namespace.Replace("feature/", "")
            }
            elseif ($namespace.StartsWith("release/") -or ($namespace -eq "master")) {
                $namespace = "public"
            }
            $namespace = $namespace.Replace("_", "/").Substring(0, [Math]::Min($namespace.Length, 24))
            $templateParameters.Add("imagesNamespace", $namespace)
            Write-Host "Using $($script:version) $($namespace) images from $($creds.dockerServer)."
        }
        else {
            $templateParameters.Add("dockerServer", "mcr.microsoft.com")
            Write-Host "Using $($script:version) images from mcr.microsoft.com."
        }

        if ($script:type -eq "all") {
            $templateParameters.Add("siteName", $script:applicationName)
            $templateParameters.Add("numberOfLinuxGateways", 1)
            $templateParameters.Add("numberOfWindowsGateways", 1)
            $templateParameters.Add("numberOfSimulations", 1)

            # Get all vm skus available in the location and in the account
            $availableVms = Get-AzComputeResourceSku | Where-Object {
                ($_.ResourceType.Contains("virtualMachines")) -and `
                ($_.Locations -icontains $script:resourceGroupLocation) -and `
                ($_.Restrictions.Count -eq 0)
            }
            # Sort based on sizes and filter minimum requirements
            $availableVmNames = $availableVms `
                | Select-Object -ExpandProperty Name -Unique
            $vmSizes = Get-AzVMSize $script:resourceGroupLocation `
                | Where-Object { $availableVmNames -icontains $_.Name } `
                | Where-Object {
                    ($_.NumberOfCores -ge 2) -and `
                    ($_.MemoryInMB -ge 8192) -and `
                    ($_.OSDiskSizeInMB -ge 1047552) -and `
                    ($_.ResourceDiskSizeInMB -gt 8192)
                } `
                | Sort-Object -Property `
                    NumberOfCores,MemoryInMB,ResourceDiskSizeInMB,Name
            # Pick top
            if ($vmSizes.Count -ne 0) {
                $vmSize = $vmSizes[0].Name
                Write-Host "Using $($vmSize) as VM size for all edge simulation hosts..."
                $templateParameters.Add("edgeVmSize", $vmSize)
            }

            $adminUser = "sandboxuser"
            $adminPassword = New-Password
            $templateParameters.Add("edgePassword", $adminPassword)
            $templateParameters.Add("edgeUserName", $adminUser)
        }
        if ($script:type -eq "app") {
            $templateParameters.Add("siteName", $script:applicationName)
        }
        if ($script:type -eq "services") {
            $templateParameters.Add("serviceSiteName", $script:applicationName)
        }
    }

    $aadAddReplyUrls = $false
    if (!$script:aadConfig) {
        if ([string]::IsNullOrEmpty($script:aadApplicationName)) {
            $script:aadApplicationName = $script:applicationName
        }

        # register aad application
        Write-Host
        Write-Host "Registering client and services AAD applications in your tenant..."
        $script:aadConfig = & (Join-Path $script:ScriptDir "aad-register.ps1") `
            -Context $context -Name $script:aadApplicationName

        Write-Host "Client and services AAD applications registered..."
        Write-Host
        $aadAddReplyUrls = $true
    }
    elseif (($script:aadConfig -is [string]) -and (Test-Path $script:aadConfig)) {
        # read configuration from file
        $script:aadConfig = Get-Content -Raw -Path $script:aadConfig | ConvertFrom-Json
    }

    if (![string]::IsNullOrEmpty($script:aadConfig.ServicePrincipalId)) {
        $templateParameters.Add("servicePrincipalId", $script:aadConfig.ServicePrincipalId)
    }
    if (![string]::IsNullOrEmpty($script:aadConfig.ServiceId)) {
        $templateParameters.Add("serviceAppId", $script:aadConfig.ServiceId)
    }
    if (![string]::IsNullOrEmpty($script:aadConfig.ServiceSecret)) {
        $templateParameters.Add("serviceAppSecret", $script:aadConfig.ServiceSecret)
    }
    if (![string]::IsNullOrEmpty($script:aadConfig.ClientId)) {
        $templateParameters.Add("clientAppId", $script:aadConfig.ClientId)
    }
    if (![string]::IsNullOrEmpty($script:aadConfig.ClientSecret)) {
        $templateParameters.Add("clientAppSecret", $script:aadConfig.ClientSecret)
    }
    if (![string]::IsNullOrEmpty($script:aadConfig.Authority)) {
        $templateParameters.Add("authorityUri", $script:aadConfig.Authority)
    }
    if (![string]::IsNullOrEmpty($script:aadConfig.Audience)) {
        $templateParameters.Add("serviceAudience", $script:aadConfig.Audience)
    }

    # register providers
    $script:requiredProviders | ForEach-Object {
        Register-AzResourceProvider -ProviderNamespace $_
    } | Out-Null

    while ($true) {
        try {
            Write-Host "Starting deployment..."

            if (![string]::IsNullOrEmpty($adminUser) -and ![string]::IsNullOrEmpty($adminPassword)) {
                Write-Host
                Write-Host "To troubleshoot simulation use the following User and Password to log into the VM's:"
                Write-Host
                Write-Host $adminUser
                Write-Host $adminPassword
                Write-Host
            }

            # Start the deployment
            $templateFilePath = Join-Path (Join-Path (Split-Path $ScriptDir) "templates") "azuredeploy.json"
            $deployment = New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName `
                -TemplateFile $templateFilePath -TemplateParameterObject $templateParameters

            if ($deployment.ProvisioningState -ne "Succeeded") {
                Set-ResourceGroupTags -state "Failed"
                throw "Deployment $($deployment.ProvisioningState)."
            }

            Set-ResourceGroupTags -state "Complete"
            Write-Host "Deployment succeeded."

            #
            # Add reply urls
            #
            $replyUrls = New-Object System.Collections.Generic.List[System.String]
            if ($aadAddReplyUrls) {
                # retrieve existing urls
                $app = Get-AzADApplication -ObjectId $aadConfig.ClientPrincipalId
                if ($app.ReplyUrls -and ($app.ReplyUrls.Count -ne 0)) {
                    $replyUrls = $app.ReplyUrls;
                }
            }
            $website = $deployment.Outputs["appUrl"].Value
            if (![string]::IsNullOrEmpty($website)) {
                Write-Host
                Write-Host "The deployed application can be found at:"
                Write-Host $website
                Write-Host
                if (![string]::IsNullOrEmpty($script:aadConfig.ClientPrincipalId)) {
                    if (!$aadAddReplyUrls) {
                        Write-Host "To be able to use the application you need to register the following"
                        Write-Host "reply url for AAD application $($script:aadConfig.ClientPrincipalId):"
                        Write-Host "$($website)/signin-oidc"
                    }
                    else {
                        $replyUrls.Add("$($website)/signin-oidc")
                    }
                }
            }

            if ($aadAddReplyUrls -and ![string]::IsNullOrEmpty($script:aadConfig.ClientPrincipalId)) {
                $serviceUri = $deployment.Outputs["serviceUrl"].Value

                if (![string]::IsNullOrEmpty($serviceUri)) {
                    $replyUrls.Add($serviceUri + "/twin/swagger/oauth2-redirect.html")
                    $replyUrls.Add($serviceUri + "/registry/swagger/oauth2-redirect.html")
                    $replyUrls.Add($serviceUri + "/history/swagger/oauth2-redirect.html")
                    $replyUrls.Add($serviceUri + "/vault/swagger/oauth2-redirect.html")
                    $replyUrls.Add($serviceUri + "/publisher/swagger/oauth2-redirect.html")
                    $replyUrls.Add($serviceUri + "/onboarding/swagger/oauth2-redirect.html")
                    $replyUrls.Add($serviceUri + "/configuration/swagger/oauth2-redirect.html")
                    $replyUrls.Add($serviceUri + "/edge/manage/swagger/oauth2-redirect.html")
                    $replyUrls.Add($serviceUri + "/edge/jobs/swagger/oauth2-redirect.html")
                    $replyUrls.Add($serviceUri + "/jobs/swagger/oauth2-redirect.html")
                }

                $replyUrls.Add("http://localhost:9080/twin/swagger/oauth2-redirect.html")
                $replyUrls.Add("http://localhost:9080/registry/swagger/oauth2-redirect.html")
                $replyUrls.Add("http://localhost:9080/history/swagger/oauth2-redirect.html")
                $replyUrls.Add("http://localhost:9080/vault/swagger/oauth2-redirect.html")
                $replyUrls.Add("http://localhost:9080/publisher/swagger/oauth2-redirect.html")
                $replyUrls.Add("http://localhost:9080/onboarding/swagger/oauth2-redirect.html")
                $replyUrls.Add("http://localhost:9080/configuration/swagger/oauth2-redirect.html")
                $replyUrls.Add("http://localhost:9080/edge/manage/swagger/oauth2-redirect.html")
                $replyUrls.Add("http://localhost:9080/edge/jobs/swagger/oauth2-redirect.html")
                $replyUrls.Add("http://localhost:9080/jobs/swagger/oauth2-redirect.html")

                $replyUrls.Add("http://localhost:5000/signin-oidc")
                $replyUrls.Add("https://localhost:5001/signin-oidc")
            }

            if ($aadAddReplyUrls) {
                # register reply urls in client application registration
                Write-Host
                Write-Host "Registering reply urls for $($aadConfig.ClientPrincipalId)..."

                try {
                    # assumes we are still connected
                    $replyUrls.Add("urn:ietf:wg:oauth:2.0:oob")
                    $replyUrls = ($replyUrls | sort-object –Unique)

                    # TODO
                    #    & (Join-Path $script:ScriptDir "aad-update.ps1") `
                    #        $context `
                    #        -ObjectId $aadConfig.ClientPrincipalId -ReplyUrls $replyUrls
                    Update-AzADApplication -ObjectId $aadConfig.ClientPrincipalId -ReplyUrl $replyUrls `
                        | Out-Null

                    Write-Host "Reply urls registered in client app $($aadConfig.ClientPrincipalId)..."
                    Write-Host
                }
                catch {
                    Write-Host $_.Exception.Message
                    Write-Host
                    Write-Host "Registering reply urls failed. Please add the following urls to"
                    Write-Host "the client app '$($aadConfig.ClientPrincipalId)' manually:"
                    $replyUrls | ForEach-Object { Write-Host $_ }
                }
            }

            #
            # Create environment file
            #
            Write-EnvironmentVariables -deployment $deployment

            if (![string]::IsNullOrEmpty($website)) {
                # Try open application
                Start-Process $website -ErrorAction SilentlyContinue | Out-Null
            }
            return
        }
        catch {
            $ex = $_
            Write-Host $_.Exception.Message
            Write-Host "Deployment failed."

            $deleteResourceGroup = $false
            if (!$script:interactive) {
                $deleteResourceGroup = $script:deleteOnErrorPrompt
            }
            else {
                $retry = Read-Host -Prompt "Try again? [y/n]"
                if ($retry -match "[yY]") {
                    continue
                }
                if ($script:deleteOnErrorPrompt) {
                    $reply = Read-Host -Prompt "Delete resource group? [y/n]"
                    $deleteResourceGroup = ($reply -match "[yY]")
                }
            }
            if ($deleteResourceGroup) {
                try {
                    Write-Host "Removing resource group $($script:resourceGroupName)..."
                    Remove-AzResourceGroup -ResourceGroupName $script:resourceGroupName -Force
                }
                catch {
                    Write-Warning $_.Exception.Message
                }
            }
            throw $ex
        }
    }
}

#*******************************************************************************************************
# Test all deployment options and resource locations one by one
#*******************************************************************************************************
Function Test-All-Deployment-Options() {
    Param(
        $context
    )

    while ([string]::IsNullOrEmpty($script:resourceGroupName) `
            -or ($script:resourceGroupName -notmatch "^[a-z0-9-_]*$")) {
        Write-Host
        $script:resourceGroupName = Read-Host "Please provide test resource group prefix"
    }
    $testGroup = $script:resourceGroupName

    $script:repo = "https://github.com/Azure/Industrial-IoT"
    $script:branchName = "master"
    $script:interactive = $false
    $script:deleteOnErrorPrompt = $false
    # register aad application
    $script:aadConfig = & (Join-Path $script:ScriptDir "aad-register.ps1") `
        -Context $context -Name $script:aadApplicationName

    Get-ResourceGroupLocations | ForEach-Object {
        $script:resourceGroupLocation = $_.Location
        foreach ($deployType in @("all", "app", "services", "local")) {
            $script:type = $deployType
            $script:resourceGroupName = "$($testGroup)_$($script:resourceGroupLocation)_$($script:type)"
            $existing = Get-AzResourceGroup -ResourceGroupName $script:resourceGroupName `
                -ErrorAction SilentlyContinue
            if (!$existing) {
                try {
                    Write-Host("Deploying to $($script:resourceGroupName)...")
                    New-AzResourceGroup -Name $script:resourceGroupName `
                        -Location $script:resourceGroupLocation | Out-Null
                    $script:applicationName = $script:resourceGroupName.Replace("_", "")
                    New-Deployment -context $context | Out-Null

                    Remove-AzResourceGroup -ResourceGroupName $script:resourceGroupName -Force `
                        -ErrorAction SilentlyContinue | Out-Null
                    New-AzResourceGroup -Name $script:resourceGroupName -Location $script:resourceGroupLocation `
                        -ErrorAction SilentlyContinue | Out-Null
                }
                catch {
                    Write-Host
                    Write-Host
                    Write-Warning("$($script:type) in $($script:resourceGroupLocation) failed with $($_.Exception.Message)")
                    Write-Warning($_)
                    Write-Host
                    Write-Host

                    break
                }
            }
        }
    }
}

#*******************************************************************************************************
# Script body
#*******************************************************************************************************
$ErrorActionPreference = "Stop"
$script:ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path
$script:interactive = !$script:context

$script:requiredProviders = @(
    "microsoft.devices",
    "microsoft.documentdb",
    "microsoft.signalrservice",
    "microsoft.servicebus",
    "microsoft.eventhub",
    "microsoft.storage",
    "microsoft.keyvault",
    "microsoft.managedidentity",
    "microsoft.timeseriesinsights",
    "microsoft.web",
    "microsoft.compute",
    "microsoft.containerregistry"
)

Write-Host "Signing in ..."
Write-Host
Import-Module Az
$script:context = Select-Context -context $script:context `
    -environment (Get-AzEnvironment -Name $script:environmentName)

if ($testAllDeploymentOptions.IsPresent) {
    Test-All-Deployment-Options -context $script:context
    return
}

$script:deleteOnErrorPrompt = Select-ResourceGroup
New-Deployment -context $script:context
