<#
 .SYNOPSIS
    Creates a new enrollment in dps

 .DESCRIPTION
    Creates a new random enrollment in dps and returns enrollment information

 .PARAMETER dpsConnString
    The Azure Device Provisioning Service connection string

 .PARAMETER os
    The operating system to enroll
#>
param(
    [Parameter(Mandatory)]
    [string] $dpsConnString,
    [Parameter(Mandatory)]
    [string] $os
)

#******************************************************************************
# Generate a random key
#******************************************************************************
Function New-Key() {
    param(
        $length = 15
    )
    $digits = 48..57
    $lcLetters = 65..90
    $password = `
        [char](Get-Random -Count 1 -InputObject ($lcLetters)) + `
        [char](Get-Random -Count 1 -InputObject ($digits))
    $password += get-random -Count ($length - 4) `
        -InputObject ($digits + $lcLetters) |`
        ForEach-Object -begin { $aa = $null } -process { $aa += [char]$_ } -end { $aa }
    return $password
}

$registrationId = (New-Key).ToLower()

# Parse connection string
$hostName = $null
$keyName = $null
$key = $null
$dpsConnString.Split(';') | ForEach-Object {
    $kv = $_
    $x = "HostName="
    if ($kv.StartsWith($x)) {
        $hostName = $kv.Replace($x, "").Trim()
        return
    }
    $x = "SharedAccessKeyName="
    if ($kv.StartsWith($x)) {
        $keyName = $kv.Replace($x, "").Trim()
    }
    $x = "SharedAccessKey="
    if ($kv.StartsWith($x)) {
        $key = $kv.Replace($x, "").Trim()
        return
    }
}

# Create sas token
Add-Type -AssemblyName System.Web
$audience = $hostName
$expires=([DateTimeOffset]::Now.ToUnixTimeSeconds()) + 300
$signatureString=[System.Web.HttpUtility]::UrlEncode($audience)+ "`n" + [string]$expires
$hmac = New-Object System.Security.Cryptography.HMACSHA256
$hmac.key = [Convert]::FromBase64String($key)
$signature = $HMAC.ComputeHash([Text.Encoding]::UTF8.GetBytes($signatureString))
$signature = [Convert]::ToBase64String($signature)
$sasToken = "SharedAccessSignature " `
    + "sr=" + [System.Web.HttpUtility]::UrlEncode($audience) `
    + "&sig=" + [System.Web.HttpUtility]::UrlEncode($signature) `
    + "&se=" + $expires `
    + "&skn=" + $keyName

# Create enrollment

$headers = @{"Authorization" = $sasToken; "Content-Type" = "application/json"}
Add-Type -AssemblyName System.Net
$deviceId = [System.Net.Dns]::GetHostName()
$body = @{
    attestation = @{
        type = "symmetricKey"
    }
    deviceId = $deviceId
    initialTwin = @{
        tags = @{
            __type__ = "iiotedge"
            os = $os
        }
    }
    registrationId = $registrationId
    capabilities = @{
        iotEdge = $true
    }
} | ConvertTo-Json


$uri = "https://$($hostName)/enrollments/$($registrationId)?api-version=2019-03-31"
try {
    $response = $body | Invoke-RestMethod -Method Put -Headers $headers -Uri $uri
    return @{
        registrationId = $response.registrationId
        primaryKey = $response.attestation.symmetricKey.primaryKey
    }
} catch {
    Write-Host $_.Exception.Message
    return $null
}
