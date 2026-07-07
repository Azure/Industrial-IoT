Param(
    [string]
    $ResourceGroupName,
    [string]
    $ServicePrincipalName,
    [string]
    # The role to grant. Defaults to "Key Vault Secrets Officer" (get/list/set/delete)
    # which matches the previous get/list/set access policy.
    $RoleDefinitionName = "Key Vault Secrets Officer"
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName not set."
}

$keyVaults = Get-AzKeyVault -ResourceGroupName $ResourceGroupName

if (!$keyVaults) {
    Write-Error "Could not find any KeyVaults in Resource Group ($ResourceGroupName)"
}

function Grant-KvRole {
    Param([Parameter(Mandatory=$true)][string]$VaultScope, [Parameter(Mandatory=$true)][string]$PrincipalObjectId, [Parameter(Mandatory=$true)][string]$Role)
    $existing = Get-AzRoleAssignment -ObjectId $PrincipalObjectId -RoleDefinitionName $Role -Scope $VaultScope -ErrorAction SilentlyContinue
    if (!$existing) {
        try {
            New-AzRoleAssignment -ObjectId $PrincipalObjectId -RoleDefinitionName $Role -Scope $VaultScope | Out-Null
        }
        catch {
            # A least-privilege principal may lack
            # Microsoft.Authorization/roleAssignments/write when the role is
            # pre-assigned at a parent (subscription) scope. Tolerate the self-grant
            # failure: if access is already effective via inheritance the test run
            # still reads the vault; otherwise the missing-secret warnings downstream
            # make the gap obvious.
            Write-Warning "Could not assign '$Role' on '$VaultScope': $($_.Exception.Message). Continuing; relying on inherited/pre-assigned access."
        }
    }
}

if ($ServicePrincipalName) {
    # ServicePrincipalName may be either an objectId or appId; resolve to objectId.
    $sp = Get-AzADServicePrincipal -ApplicationId $ServicePrincipalName -ErrorAction SilentlyContinue
    if (!$sp) {
        $sp = Get-AzADServicePrincipal -ObjectId $ServicePrincipalName -ErrorAction SilentlyContinue
    }
    if (!$sp) {
        $sp = Get-AzADServicePrincipal -DisplayName $ServicePrincipalName -ErrorAction SilentlyContinue
    }
    if (!$sp) {
        Write-Error "Could not resolve ServicePrincipal '$ServicePrincipalName' to an objectId."
    }
    $objectId = $sp.Id
    $keyVaults | %{
        Write-Host "Granting role '$RoleDefinitionName' on vault '$($_.VaultName)' to Service Principal '$ServicePrincipalName' (objectId $objectId)"
        Grant-KvRole -VaultScope $_.ResourceId -PrincipalObjectId $objectId -Role $RoleDefinitionName
    }
} else {
    $azContext = Get-AzContext
    if (!$azContext -or !$azContext.Account.Id) {
        Write-Error "Not logged in" -ErrorAction Stop
    }
    # Resolve the current user to an objectId.
    if ($azContext.Account.Type -eq 'User') {
        $user = Get-AzADUser -UserPrincipalName $azContext.Account.Id -ErrorAction SilentlyContinue
        if (!$user) {
            $user = Get-AzADUser -Mail $azContext.Account.Id -ErrorAction SilentlyContinue
        }
        if (!$user) {
            Write-Error "Could not resolve current user '$($azContext.Account.Id)' to an objectId."
        }
        $objectId = $user.Id
    } else {
        # ServicePrincipal / ManagedIdentity context.
        $sp = Get-AzADServicePrincipal -ApplicationId $azContext.Account.Id -ErrorAction SilentlyContinue
        if (!$sp) {
            Write-Error "Could not resolve current principal '$($azContext.Account.Id)' to an objectId."
        }
        $objectId = $sp.Id
    }
    $keyVaults | %{
        Write-Host "Granting role '$RoleDefinitionName' on vault '$($_.VaultName)' to current principal '$($azContext.Account.Id)' (objectId $objectId)"
        Grant-KvRole -VaultScope $_.ResourceId -PrincipalObjectId $objectId -Role $RoleDefinitionName
    }
}
