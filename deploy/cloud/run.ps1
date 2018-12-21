<#
 .SYNOPSIS
    Deploys to Azure

 .DESCRIPTION
    Deploys an Azure Resource Manager template of choice

 .PARAMETER resourceGroupName
    The resource group where the template will be deployed. 

 .PARAMETER aadConfig
    The AAD configuration the template will be configured with.

 .PARAMETER interactive
    Whether to run in interactive mode
#>

param(
    [Parameter(Mandatory=$True)] [string] $resourceGroupName,
    $aadConfig = $null,
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
#Register-AzureRmResourceProvider -ProviderNamespace "microsoft.compute" | Out-Null

# Set admin password
$adminPassword = CreateRandomPassword
$templateParameters = @{ 
    # adminPassword = $adminPassword
}

try {
    # Try set branch name as current branch
    $output = cmd /c "git rev-parse --abbrev-ref HEAD" 2`>`&1
    $branchName = ("{0}" -f $output);
    Write-Host "VM deployment will use configuration from '$branchName' branch."
    # $templateParameters.Add("branchName", $branchName)
}
catch {
    # $templateParameters.Add("branchName", "master")
}

# Configure auth
if ($aadConfig) {
    if (![string]::IsNullOrEmpty($aadConfig.Audience)) { 
        # $templateParameters.Add("authAudience", $aadConfig.Audience)
    }
    if (![string]::IsNullOrEmpty($aadConfig.ClientId)) { 
        # $templateParameters.Add("aadClientId", $aadConfig.ClientId)
    }
    if (![string]::IsNullOrEmpty($aadConfig.TenantId)) { 
        # $templateParameters.Add("aadTenantId", $aadConfig.TenantId)
    }
    if (![string]::IsNullOrEmpty($aadConfig.Instance)) { 
        # $templateParameters.Add("aadInstance", $aadConfig.Instance)
    }
}


# Set website name
if ($interactive) {
    $webAppName = Read-Host "Please specify a website name"
    if (![string]::IsNullOrEmpty($webAppName)) { 
        $templateParameters.Add("webAppName", $webAppName)
    }
}

# Start the deployment
$templateFilePath = Join-Path $ScriptDir "template.json"
Write-Host "Starting deployment..."
$deployment = New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName `
    -TemplateFile $templateFilePath -TemplateParameterObject $templateParameters

$webAppPortalUrl = $deployment.Outputs["webAppPortalUrl"].Value
$webAppServiceUrl = $deployment.Outputs["webAppServiceUrl"].Value
#$adminUser = $deployment.Outputs["adminUsername"].Value

if ($aadConfig -and $aadConfig.ClientObjectId) {
    # 
    # Update client application to add reply urls required permissions.
    #
    Write-Host "Adding ReplyUrls:"
    $replyUrls = New-Object System.Collections.Generic.List[System.String]
    $replyUrls.Add($webAppPortalUrl)
    $replyUrls.Add($webAppPortalUrl + "/oauth2-redirect.html")
    Write-Host $webAppPortalUrl + "/oauth2-redirect.html"
    # still connected
    Set-AzureADApplication -ObjectId $aadConfig.ClientObjectId -ReplyUrls $replyUrls
}

Write-Host
Write-Host "To access the web portal go to:"
Write-Host $webAppPortalUrl
Write-Host
Write-Host "To access the web service go to:"
Write-Host $webAppServiceUrl
Write-Host
#Write-Host "Use the following User and Password to log onto your VM:"
#Write-Host 
#Write-Host $adminUser
#Write-Host $adminPassword
#Write-Host 
