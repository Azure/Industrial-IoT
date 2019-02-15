<#
 .SYNOPSIS
    Deploys to Azure

 .DESCRIPTION
    Deploys an Azure Resource Manager template of choice

 .PARAMETER resourceGroupName
    The resource group where the template will be deployed.

 .PARAMETER webAppName
    The host name prefix of the web application. 

 .PARAMETER webServiceName
    The host name prefix of the web application. 

 .PARAMETER aadConfig
    The AAD configuration the template will be configured with.

 .PARAMETER groupsConfig
    The certificate groups configuration.

 .PARAMETER environment
    Set the web app environment configuration. (Production,Development)

 .PARAMETER interactive
    Whether to run in interactive mode

#>

param(
    [Parameter(Mandatory=$True)] [string] $resourceGroupName,
    [string] $webAppName = $null,
    [string] $webServiceName = $null,
    $aadConfig = $null,
    [string] $groupsConfig = $null,
    [string] $environment = "Production",
    $interactive = $true
)

#******************************************************************************
# Generate a random password
#******************************************************************************
Function CreateRandomPassword() {
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
    $password += get-random -Count ($length -4) `
        -InputObject ($punc + $digits + $lcLetters + $ucLetters) |`
         % -begin { $aa = $null } -process {$aa += [char]$_} -end {$aa}

    return $password
}

#******************************************************************************
# Script body
#******************************************************************************
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

# Register RPs
Register-AzureRmResourceProvider -ProviderNamespace "microsoft.web" | Out-Null

$templateParameters = @{ }

try {
    # Try set branch name as current branch
    $output = cmd /c "git rev-parse --abbrev-ref HEAD" 2`>`&1
    $branchName = ("{0}" -f $output);
    Write-Host "Deployment will use configuration from '$branchName' branch."
    # $templateParameters.Add("branchName", $branchName)
}
catch {
    # $templateParameters.Add("branchName", "master")
}

# Configure auth
if ($aadConfig) {
    if (![string]::IsNullOrEmpty($aadConfig.Audience)) { 
        $templateParameters.Add("aadAudience", $aadConfig.Audience)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ServiceId)) { 
        $templateParameters.Add("aadServiceId", $aadConfig.ServiceId)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ServiceObjectId)) { 
        $templateParameters.Add("aadServicePrincipalId", $aadConfig.ServicePrincipalId)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ServiceSecret)) { 
        $templateParameters.Add("aadServiceSecret", $aadConfig.ServiceSecret)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ClientId)) { 
        $templateParameters.Add("aadClientId", $aadConfig.ClientId)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ClientSecret)) { 
        $templateParameters.Add("aadClientSecret", $aadConfig.ClientSecret)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ModuleId)) { 
        $templateParameters.Add("aadModuleId", $aadConfig.ModuleId)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ModuleSecret)) { 
        $templateParameters.Add("aadModuleSecret", $aadConfig.ModuleSecret)
    }
    if (![string]::IsNullOrEmpty($aadConfig.TenantId)) { 
        $templateParameters.Add("aadTenantId", $aadConfig.TenantId)
    }
    if (![string]::IsNullOrEmpty($aadConfig.Instance)) { 
        $templateParameters.Add("aadInstance", $aadConfig.Instance)
    }
    if (![string]::IsNullOrEmpty($aadConfig.UserPrincipalId)) { 
        $templateParameters.Add("aadUserPrincipalId", $aadConfig.UserPrincipalId)
    }
}

# Configure groups
if (![string]::IsNullOrEmpty($groupsConfig)) { 
    $templateParameters.Add("groupsConfig", $groupsConfig)
}

# Set web app site name
if ($interactive -and [string]::IsNullOrEmpty($webAppName)) {
    $webAppName = Read-Host "Please specify a web applications site name"
}

if (![string]::IsNullOrEmpty($webAppName)) { 
    $templateParameters.Add("webAppName", $webAppName)
}

# Set web service site name
if ($interactive -and [string]::IsNullOrEmpty($webServiceName)) {
    $webServiceName = Read-Host "Please specify a web service site name"
}

if (![string]::IsNullOrEmpty($webServiceName)) { 
    $templateParameters.Add("webServiceName", $webServiceName)
}

# Configure web app environment
if (![string]::IsNullOrEmpty($environment)) { 
    $templateParameters.Add("environment", $environment)
}

# Start the deployment
$templateFilePath = Join-Path $ScriptDir "template.json"
Write-Host "Starting deployment..."
$deployment = New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName `
    -TemplateFile $templateFilePath -TemplateParameterObject $templateParameters

$webAppPortalUrl = $deployment.Outputs["webAppPortalUrl"].Value
$webAppServiceUrl = $deployment.Outputs["webAppServiceUrl"].Value
$webAppPortalName = $deployment.Outputs["webAppPortalName"].Value
$webAppServiceName = $deployment.Outputs["webAppServiceName"].Value
$keyVaultBaseUrl = $deployment.Outputs["keyVaultBaseUrl"].Value
$opcVaultBaseUrl = $deployment.Outputs["opcVaultBaseUrl"].Value
$cosmosDBEndpoint = $deployment.Outputs["cosmosDBEndpoint"].Value

if ($aadConfig -and $aadConfig.ClientObjectId) {
    # 
    # Update client application to add reply urls to required permissions.
    #
    $adClient = Get-AzureADApplication -ObjectId $aadConfig.ClientObjectId 
    Write-Host "Adding ReplyUrls to client AD:"
    $replyUrls = $adClient.ReplyUrls
    # web app
    $replyUrls.Add($webAppPortalUrl + "/signin-oidc")
    # swagger
    $replyUrls.Add($webAppServiceUrl + "/oauth2-redirect.html")
    Write-Host $webAppPortalUrl"/signin-oidc"
    Write-Host $webAppServiceUrl"/oauth2-redirect.html"
    Set-AzureADApplication -ObjectId $aadConfig.ClientObjectId -ReplyUrls $replyUrls -HomePage $webAppPortalUrl
}

if ($aadConfig -and $aadConfig.ServiceObjectId) {
    # 
    # Update client application to add reply urls to required permissions.
    #
    $adService = Get-AzureADApplication -ObjectId $aadConfig.ServiceObjectId 
    Write-Host "Adding ReplyUrls to service AD:"
    $replyUrls = $adService.ReplyUrls
    # service
    $replyUrls.Add($webAppServiceUrl + "/signin-oidc")
    Write-Host $webAppServiceUrl"/signin-oidc"
    Set-AzureADApplication -ObjectId $aadConfig.ServiceObjectId -ReplyUrls $replyUrls -HomePage $webServicePortalUrl
}

Return $webAppPortalUrl, $webAppServiceUrl, $keyVaultBaseUrl, $opcVaultBaseUrl, $cosmosDBEndpoint
