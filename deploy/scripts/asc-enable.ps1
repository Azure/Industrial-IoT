<#
 .SYNOPSIS
    Deploys Industrial IoT services to Azure

 .DESCRIPTION
    Deploys the Industrial IoT services dependencies and optionally micro services and UI to Azure.

 .PARAMETER subscriptionId
    The subscription id where resources will be deployed

 .PARAMETER resourceGroupName
    The name of an existing resource group

 .PARAMETER workspaceName
    The name of loganalytics workspace

 .PARAMETER iotHubName
    The name of iot hub

#>

#*******************************************************************************************************
# Get Access token using AzureRM module
#*******************************************************************************************************
function Get-AzureRmCachedAccessToken() {
    $ErrorActionPreference = 'Stop'
  
    if(-not (Get-Module AzureRm.Profile)) {
        Import-Module AzureRm.Profile
    }
    $azureRmProfileModuleVersion = (Get-Module AzureRm.Profile).Version
    # refactoring performed in AzureRm.Profile v3.0 or later
    if($azureRmProfileModuleVersion.Major -ge 3) {
        $azureRmProfile = [Microsoft.Azure.Commands.Common.Authentication.Abstractions.AzureRmProfileProvider]::Instance.Profile
        if(-not $azureRmProfile.Accounts.Count) {
            Write-Error "Ensure you have logged in before calling this function."    
        }
    } else {
        # AzureRm.Profile < v3.0
        $azureRmProfile = [Microsoft.WindowsAzure.Commands.Common.AzureRmProfileProvider]::Instance.Profile
        if(-not $azureRmProfile.Context.Account.Count) {
            Write-Error "Ensure you have logged in before calling this function."    
        }
    }
  
    $currentAzureContext = Get-AzureRmContext
    $profileClient = New-Object Microsoft.Azure.Commands.ResourceManager.Common.RMProfileClient($azureRmProfile)
    Write-Debug ("Getting access token for tenant" + $currentAzureContext.Tenant.TenantId)
    $token = $profileClient.AcquireAccessToken($currentAzureContext.Tenant.TenantId)
    $token.AccessToken
}

#*******************************************************************************************************
# Get an access token
#*******************************************************************************************************
function Get-AzureRmBearerToken() {
    $ErrorActionPreference = 'Stop'
    ('Bearer {0}' -f (Get-AzureRmCachedAccessToken))
}

#***************************************************************************************************************************
# Set Azure Sentinel Solution to Log Analytics workspace, enable ASC for IoT and create custom alert for security assessment
#***************************************************************************************************************************
function SetSolutionEnableASCForIotAndCreateAlert() {
    Param(
        [string] $subscriptionId,
        [string] $resourceGroupName,
        [string] $workspaceName,
        [string] $iotHubName
    )

        $resourceGroup = Get-AzureRmResourceGroup -Name $resourceGroupName
        $contentType = "application/json"
        $token = Get-AzureRmBearerToken
        $headers = @{
            Authorization = $token
        };
        $urlPrefix = "https://management.azure.com/subscriptions/" + $subscriptionId + "/resourceGroups/" + $resourceGroupName + "/providers/";

        ## add azure sentinel to log analytics workspace
        $enableSecuritySolutionUrl = $urlPrefix + "Microsoft.OperationsManagement/solutions/SecurityInsights(" + $workspaceName + ")?api-version=2015-11-01-preview";
        $bodyForSecuritySolution = @{
          location = $resourceGroup.Location;
          properties = @{
              workspaceResourceId = "/subscriptions/" + $subscriptionId + "/resourcegroups/" + $resourceGroupName + "/providers/microsoft.operationalinsights/workspaces/" + $workspaceName;
            };
            plan = @{
              name = "SecurityInsights(" + $workspaceName + ")";
              publisher = "Microsoft";
              product = "OMSGallery/SecurityInsights";
              promotionCode = ""
            }
        };
        $json = $bodyForSecuritySolution | ConvertTo-Json
        Invoke-RestMethod -Method PUT -Uri $enableSecuritySolutionUrl -ContentType $contentType -Headers $headers -Body $json;

        ## enable asc for iot and add workspace
        $iotSecuritysiteName = "AzureIotSolution-" + $workspaceName.substring($workspaceName.length - 5, 5)
        $enableASCForIotUrl = $urlPrefix + "Microsoft.Security/IoTSecuritySolutions/" + $iotSecuritysiteName + "?api-version=2017-08-01-preview";
        $bodyForASCForIoT = @{
            location = $resourceGroup.Location;
            properties = @{
                displayName = $iotSecuritysiteName;
                status = "Enabled";
                export = @("RawEvents");
                disabledDataSources = @();
                workspace = "/subscriptions/" + $subscriptionId + "/resourcegroups/" + $resourceGroupName + "/providers/microsoft.operationalinsights/workspaces/" + $workspaceName;
                iotHubs = @( "/subscriptions/" + $subscriptionId + "/resourceGroups/" + $resourceGroupName + "/providers/Microsoft.Devices/IotHubs/" + $iotHubName )
            }
        };
        $json2 = $bodyForASCForIoT | ConvertTo-Json
        Invoke-RestMethod -Method PUT -Uri $enableASCForIotUrl -ContentType $contentType -Headers $headers -Body $json2;


        ## create custom alert for security assessment
        $alertname = "SecurityAssesment-api";
        $createAlertUrl = $urlPrefix + "Microsoft.OperationalInsights/workspaces/" + $workspaceName + "/providers/Microsoft.SecurityInsights/alertRules/" + $alertName + "?api-version=2019-01-01-preview";
        $bodyForAlert = @{
            id = "/subscriptions/" + $subscriptionId + "/resourceGroups/" + $resourceGroupName + "/providers/Microsoft.OperationalInsights/workspaces/" + $workspaceName + "/providers/Microsoft.SecurityInsights/alertRules/" + $alertName;
            name = $alertName;
            type = "Microsoft.SecurityInsights/alertRules";
            properties = @{
               displayName = $alertName;
               description = "";
               severity = "Medium";
               enabled = $true;
               query = 'SecurityIoTRawEvent | where RawEventName == "ConfigurationError" and parse_json(EventDetails)["ErrorType"] == "NotOptimal" | extend AccountCustomEntity = EventDetails';
               queryFrequency = "PT15M";
               queryPeriod = "PT5H";
               triggerOperator = "GreaterThan";
               triggerThreshold = 1;
               suppressionDuration = "PT5H";
               suppressionEnabled = $true
            }
         };
        $json3 = $bodyForAlert | ConvertTo-Json
        Invoke-RestMethod -Method PUT -Uri $createAlertUrl -ContentType $contentType -Headers $headers -Body $json3;
}

SetSolutionEnableASCForIotAndCreateAlert $args[0] $args[1] $args[2] $args[3]