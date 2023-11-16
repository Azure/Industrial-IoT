#***************************************************************************************************************
# This script supports the TLS 1.2 everywhere project
# It does the following:
#   *   By default it disables TLS 1.O, TLS 1.1, SSLv2, SSLv3 and Enables TLS1.2
#   *   The CipherSuite order is set to the SDL approved version.
#   *   The FIPS MinEncryptionLevel is set to 3.
#   *   RC4 is disabled
#   *   A log with a transcript of all actions taken is generated
#***************************************************************************************************************

#************************************************ SCRIPT USAGE  ************************************************
# .\TLSSettings.ps1
#   -SetCipherOrder         :   Excellence/Min-Bar, default(Excellence), use B to set Min-Bar. (Min-Bar ordering prefers ciphers with smaller key sizes to improve performance over security)
#   -RebootIfRequired       :   $true/$false, default($true), use $false to disable auto-reboot (Settings won't take effect until a reboot is completed)
#   -EnableOlderTlsVersions :   $true/$false, default($false), use $true to explicitly Enable TLS1.0, TLS1.1
#***************************************************************************************************************

#***************************TEAM CAN DETERMINE WHAT CIPHER SUITE ORDER IS CHOSEN  ******************************
# Option B provides the min-bar configuration (small trade-off: performance over security)
# Syntax:     .\TLSSettings.ps1 -SetCipherOrder B
# if no option is supplied, you will get the opportunity for excellence cipher order (small trade-off: security over performance)
# Syntax:     .\TLSSettings.ps1
#***************************************************************************************************************

param (
    [string]$SetCipherOrder = " ",
    [bool]$RebootIfRequired = $true,
    [bool]$EnableOlderTlsVersions = $false
)

#******************* FUNCTION THAT ACTUALLY UPDATES KEYS; WILL RETURN REBOOT FLAG IF CHANGES ***********************
Function Set-CryptoSetting {
    param (
        $regKeyName,
        $value,
        $valuedata,
        $valuetype
    )

    $restart = $false

    # Check for existence of registry key, and create if it does not exist
    If (!(Test-Path -Path $regKeyName)) {
        New-Item $regKeyName | Out-Null
    }


    # Get data of registry value, or null if it does not exist
    $val = (Get-ItemProperty -Path $regKeyName -Name $value -ErrorAction SilentlyContinue).$value


    If ($val -eq $null) {
        # Value does not exist - create and set to desired value
        New-ItemProperty -Path $regKeyName -Name $value -Value $valuedata -PropertyType $valuetype | Out-Null
        $restart = $true
    }
    Else {
        # Value does exist - if not equal to desired value, change it
        If ($val -ne $valuedata) {
            Set-ItemProperty -Path $regKeyName -Name $value -Value $valuedata
            $restart = $true
        }
    }


    $restart
}
#***************************************************************************************************************


#******************* FUNCTION THAT DISABLES RC4 ***********************
Function DisableRC4 {

    $restart = $false
    $subkeys = Get-Item -Path "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL"
    $ciphers = $subkeys.OpenSubKey("Ciphers", $true)

    Write-Log -Message "----- Checking the status of RC4 -----"  -Logfile $logLocation -Severity Information

    $RC4 = $false
    if ($ciphers.SubKeyCount -eq 0) {
        $k1 = $ciphers.CreateSubKey("RC4 128/128")
        $k1.SetValue("Enabled", 0, [Microsoft.Win32.RegistryValueKind]::DWord)
        $restart = $true
        $k2 = $ciphers.CreateSubKey("RC4 64/128")
        $k2.SetValue("Enabled", 0, [Microsoft.Win32.RegistryValueKind]::DWord)
        $k3 = $ciphers.CreateSubKey("RC4 56/128")
        $k3.SetValue("Enabled", 0, [Microsoft.Win32.RegistryValueKind]::DWord)
        $k4 = $ciphers.CreateSubKey("RC4 40/128")
        $k4.SetValue("Enabled", 0, [Microsoft.Win32.RegistryValueKind]::DWord)

        Write-Log -Message "RC4 was disabled "  -Logfile $logLocation -Severity Information
        $RC4 = $true
    }

    If ($RC4 -ne $true) {
        Write-Log -Message "There was no change for RC4 "  -Logfile $logLocation -Severity Information
    }

    $restart
}
#***************************************************************************************************************

#******************* FUNCTION CHECKS FOR PROBLEMATIC FIPS SETTING AND FIXES IT  ***********************
Function Test-RegistryValueForFipsSettings {

    $restart = $false

    $fipsPath = @(
        "HKLM:\System\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp",
        "HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services",
        "HKLM:\System\CurrentControlSet\Control\Terminal Server\DefaultUserConfiguration"
    )

    $fipsValue = "MinEncryptionLevel"


    foreach ($path in $fipsPath) {

        Write-Log -Message "Checking to see if $($path)\$fipsValue exists"  -Logfile $logLocation -Severity Information

        $ErrorActionPreference = "stop"
        Try {

            $result = Get-ItemProperty -Path $path | Select-Object -ExpandProperty $fipsValue
            if ($result -eq 4) {
                set-itemproperty -Path $path -Name $fipsValue -value 3
                Write-Log -Message "Regkey $($path)\$fipsValue was changed from value $result to a value of 3"  -Logfile $logLocation -Severity Information
                $restart = $true
            }
			else {
                Write-Log -Message "Regkey $($path)\$fipsValue left at value $result"  -Logfile $logLocation -Severity Information
			}

        }
        Catch [System.Management.Automation.ItemNotFoundException] {

            Write-Log -Message "Reg path $path was not found" -Logfile $logLocation  -Severity Information
        }
        Catch [System.Management.Automation.PSArgumentException] {

            Write-Log -Message "Regkey $($path)\$fipsValue was not found" -Logfile $logLocation  -Severity Information
        }
        Catch {
            Write-Log -Message "Error of type $($Error[0].Exception.GetType().FullName) trying to get $($path)\$fipsValue"  -Logfile $logLocation -Severity Information
        }
        Finally {$ErrorActionPreference = "Continue"
        }
    }
    $restart
}
#***************************************************************************************************************

#********************************** FUNCTION THAT CREATE LOG DIRECTORY IF IT DOES NOT EXIST *******************************
function CreateLogDirectory {

    $TARGETDIR = "$env:HOMEDRIVE\Logs"
    if ( -Not (Test-Path -Path $TARGETDIR ) ) {
        New-Item -ItemType directory -Path $TARGETDIR | Out-Null
    }

   $TARGETDIR = $TARGETDIR + "\" + "TLSSettingsLogFile.csv"

   return $TARGETDIR
}
#***************************************************************************************************************


#********************************** FUNCTION THAT LOGS WHAT THE SCRIPT IS DOING *******************************
function Write-Log {
    [CmdletBinding()]
    param(
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]$Message,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]$LogFile,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [ValidateSet('Information', 'Warning', 'Error')]
        [string]$Severity = 'Information'
    )


    [pscustomobject]@{
        Time     = (Get-Date -f g)
        Message  = $Message
        Severity = $Severity
    } | ConvertTo-Csv -NoTypeInformation | Select-Object -Skip 1 | Out-File -Append -FilePath $LogFile
}

#********************************TLS CipherSuite Settings *******************************************

# CipherSuites for windows OS < 10
function Get-BaseCipherSuitesOlderWindows()
{
    param
    (
        [Parameter(Mandatory=$true, Position=0)][bool] $isExcellenceOrder
    )
    $cipherorder = @()

    if ($isExcellenceOrder -eq $true)
    {
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384_P384"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256_P256"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384_P384"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256_P256"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384_P384"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256_P256"
    }
    else
    {
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256_P256"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384_P384"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256_P256"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384_P384"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256_P256"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384_P384"
    }

    # Add additional ciphers when EnableOlderTlsVersions flag is set to true
    if ($EnableOlderTlsVersions)
    {
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA_P256"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA_P256"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA_P256"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA_P256"
        $cipherorder += "TLS_RSA_WITH_AES_256_GCM_SHA384"
        $cipherorder += "TLS_RSA_WITH_AES_128_GCM_SHA256"
        $cipherorder += "TLS_RSA_WITH_AES_256_CBC_SHA256"
        $cipherorder += "TLS_RSA_WITH_AES_128_CBC_SHA256"
        $cipherorder += "TLS_RSA_WITH_AES_256_CBC_SHA"
        $cipherorder += "TLS_RSA_WITH_AES_128_CBC_SHA"
    }
    return $cipherorder
}

# Ciphersuites needed for backwards compatibility with Firefox, Chrome
# Server 2012 R2 doesn't support TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384
# Both firefox and chrome negotiate ECDHE_RSA_AES_256_CBC_SHA1, Edge negotiates ECDHE_RSA_AES_256_CBC_SHA384
function Get-BrowserCompatCipherSuitesOlderWindows()
{
    param
    (
        [Parameter(Mandatory=$true, Position=0)][bool] $isExcellenceOrder
    )
    $cipherorder = @()

    if ($isExcellenceOrder -eq $true)
    {
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA_P384"  # (uses SHA-1)
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA_P256"  # (uses SHA-1)
    }
    else
    {
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA_P256"  # (uses SHA-1)
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA_P384"  # (uses SHA-1)
    }
    return $cipherorder
}

# Ciphersuites for OS versions windows 10 and above
function Get-BaseCipherSuitesWin10Above()
{
    param
    (
        [Parameter(Mandatory=$true, Position=0)][bool] $isExcellenceOrder
    )

    $cipherorder = @()

    if ($isExcellenceOrder -eq $true)
    {

        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256"
    }
    else
    {
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384"
    }
    # Add additional ciphers when EnableOlderTlsVersions flag is set to true
    if ($EnableOlderTlsVersions)
    {
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA_P256"
        $cipherorder += "TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA_P256"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA_P256"
        $cipherorder += "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA_P256"
        $cipherorder += "TLS_RSA_WITH_AES_256_GCM_SHA384"
        $cipherorder += "TLS_RSA_WITH_AES_128_GCM_SHA256"
        $cipherorder += "TLS_RSA_WITH_AES_256_CBC_SHA256"
        $cipherorder += "TLS_RSA_WITH_AES_128_CBC_SHA256"
        $cipherorder += "TLS_RSA_WITH_AES_256_CBC_SHA"
        $cipherorder += "TLS_RSA_WITH_AES_128_CBC_SHA"
    }

    return $cipherorder
}


#******************************* TLS Version Settings ****************************************************

function Get-RegKeyPathForTls12()
{
    $regKeyPath = @(
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2",
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Client",
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Server"
    )
    return $regKeyPath
}

function Get-RegKeyPathForTls11()
{
    $regKeyPath = @(
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.1",
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.1\Client",
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.1\Server"
    )
    return $regKeyPath
}

function Get-RegKeypathForTls10()
{
    $regKeyPath = @(
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.0",
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.0\Client",
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.0\Server"
    )
    return $regKeyPath
}

function Get-RegKeyPathForSsl30()
{
    $regKeyPath = @(
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0",
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0\Client",
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0\Server"
    )
    return $regKeyPath
}

function Get-RegKeyPathForSsl20()
{
    $regKeyPath = @(
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 2.0",
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 2.0\Client",
        "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 2.0\Server"
    )
    return $regKeyPath
}

#Initialize reboot value to false
$reboot = $false

#*****************************Create the logfile if not does not exist***************************************
$logLocation = CreateLogDirectory


#Start writing to the logs
Write-Log -Message "========== Start of logging for a script execution =========="  -Logfile $logLocation -Severity Information

$registryPathGoodGuys = @()
$registryPathBadGuys = @()

# we enable TLS 1.2 and disable SSL 2.0, 3.0 in any case
$registryPathGoodGuys += Get-RegKeyPathForTls12

$registryPathBadGuys += Get-RegKeyPathForSsl20
$registryPathBadGuys += Get-RegKeyPathForSsl30

# add TLS 1.0/1.1 to good/bad depending on user's preference
# default is adding TLS 1.0/1.1 to bad
if ($EnableOlderTlsVersions)
{
    $registryPathGoodGuys += Get-RegKeypathForTls10
    $registryPathGoodGuys += Get-RegKeyPathForTls11
    Write-Log -Message "Enabling TLS1.2, TLS1.1, TLS1.0. Disabling SSL3.0, SSL2.0"  -Logfile $logLocation -Severity Information
}
else
{
    $registryPathBadGuys += Get-RegKeypathForTls10
    $registryPathBadGuys += Get-RegKeyPathForTls11
    Write-Log -Message "Enabling TLS1.2. Disabling TLS1.1, TLS1.0, SSL3.0, SSL2.0"  -Logfile $logLocation -Severity Information
}


Write-Log -Message "Check which registry keys exist already and which registry keys need to be created."  -Logfile $logLocation -Severity Information

#******************* CREATE THE REGISTRY KEYS IF THEY DON'T EXIST********************************
# Check for existence of GoodGuy registry keys, and create if they do not exist
For ($i = 0; $i -lt $registryPathGoodGuys.Length; $i = $i + 1) {

	   Write-Log -Message "Checking for existing of key: $($registryPathGoodGuys[$i]) " -Logfile $logLocation  -Severity Information
	   If (!(Test-Path -Path $registryPathGoodGuys[$i])) {
        New-Item $registryPathGoodGuys[$i] | Out-Null
     	  Write-Log -Message "Creating key: $($registryPathGoodGuys[$i]) "  -Logfile $logLocation -Severity Information
 	  }
}

# Check for existence of BadGuy registry keys, and create if they do not exist
For ($i = 0; $i -lt $registryPathBadGuys.Length; $i = $i + 1) {

    Write-Log -Message "Checking for existing of key: $($registryPathBadGuys[$i]) "  -Logfile $logLocation -Severity Information
	   If (!(Test-Path -Path $registryPathBadGuys[$i])) {
        Write-Log -Message "Creating key: $($registryPathBadGuys[$i]) "  -Logfile $logLocation -Severity Information
        New-Item  $registryPathBadGuys[$i] | Out-Null
 	  }
}

#******************* EXPLICITLY DISABLE SSLV2, SSLV3, TLS10 AND TLS11 ********************************
For ($i = 0; $i -lt $registryPathBadGuys.Length; $i = $i + 1) {

    if ($registryPathBadGuys[$i].Contains("Client") -Or $registryPathBadGuys[$i].Contains("Server")) {

        Write-Log -Message "Disabling this key: $($registryPathBadGuys[$i]) "  -Logfile $logLocation -Severity Information
        $result = Set-CryptoSetting $registryPathBadGuys[$i].ToString() Enabled 0 DWord
        $result = Set-CryptoSetting $registryPathBadGuys[$i].ToString() DisabledByDefault 1 DWord
        $reboot = $reboot -or $result
    }
}

#********************************* EXPLICITLY Enable TLS12 ****************************************
For ($i = 0; $i -lt $registryPathGoodGuys.Length; $i = $i + 1) {

    if ($registryPathGoodGuys[$i].Contains("Client") -Or $registryPathGoodGuys[$i].Contains("Server")) {

        Write-Log -Message "Enabling this key: $($registryPathGoodGuys[$i]) "  -Logfile $logLocation -Severity Information
        $result = Set-CryptoSetting $registryPathGoodGuys[$i].ToString() Enabled 1 DWord
        $result = Set-CryptoSetting $registryPathGoodGuys[$i].ToString() DisabledByDefault 0 DWord
        $reboot = $reboot -or $result
    }
}

#************************************** Disable RC4 ************************************************
$result = DisableRC4
$reboot = $reboot -or $result


#************************************** Set Cipher Suite Order **************************************
Write-Log -Message "----- starting ciphersuite order calculation -----"  -Logfile $logLocation -Severity Information
$configureExcellenceOrder = $true
if ($SetCipherOrder.ToUpper() -eq "B")
{
    $configureExcellenceOrder = $false
    Write-Host "The min bar cipher suite order was chosen."
    Write-Log -Message "The min bar cipher suite order was chosen."  -Logfile $logLocation -Severity Information
}
else
{
    Write-Host "The opportunity for excellence cipher suite order was chosen."
    Write-Log -Message "The opportunity for excellence cipher suite order was chosen."  -Logfile $logLocation -Severity Information
}
$cipherlist = @()

if ([Environment]::OSVersion.Version.Major -lt 10)
{
    $cipherlist += Get-BaseCipherSuitesOlderWindows -isExcellenceOrder $configureExcellenceOrder
    $cipherlist += Get-BrowserCompatCipherSuitesOlderWindows -isExcellenceOrder $configureExcellenceOrder
}
else
{
    $cipherlist += Get-BaseCipherSuitesWin10Above -isExcellenceOrder $configureExcellenceOrder
}
$cipherorder = [System.String]::Join(",", $cipherlist)
 Write-Host "Appropriate ciphersuite order : $cipherorder"
 Write-Log -Message "Appropriate ciphersuite order : $cipherorder"  -Logfile $logLocation -Severity Information

$CipherSuiteRegKey = "HKLM:\SOFTWARE\Policies\Microsoft\Cryptography\Configuration\SSL\00010002"

if (!(Test-Path -Path $CipherSuiteRegKey))
{
    New-Item $CipherSuiteRegKey | Out-Null
    $reboot = $True
    Write-Log -Message "Creating key: $($CipherSuiteRegKey) "  -Logfile $logLocation -Severity Information
}

$val = (Get-Item -Path $CipherSuiteRegKey -ErrorAction SilentlyContinue).GetValue("Functions", $null)
Write-Log -Message "Previous cipher suite value: $val  "  -Logfile $logLocation -Severity Information
Write-Log -Message "New cipher suite value     : $cipherorder  "  -Logfile $logLocation -Severity Information

if ($val -ne $cipherorder)
{
    Write-Log -Message "Cipher suite order needs to be updated. "  -Logfile $logLocation -Severity Information
    Write-Host "The original cipher suite order needs to be updated", `n, $val
    Set-ItemProperty -Path $CipherSuiteRegKey -Name Functions -Value $cipherorder
    Write-Log -Message "Cipher suite value was updated. "  -Logfile $logLocation -Severity Information
    $reboot = $True
}
else
{
    Write-Log -Message "Cipher suite order does not need to be updated. "  -Logfile $logLocation -Severity Information
	Write-Log -Message "Cipher suite value was not updated as there was no change. " -Logfile $logLocation -Severity Information
}

#****************************** CHECK THE FIPS SETTING WHICH IMPACTS RDP'S ALLOWED CIPHERS **************************
#Check for FipsSettings
Write-Log -Message "Checking to see if reg keys exist and if MinEncryptionLevel is set to 4"  -Logfile $logLocation -Severity Information
$result = Test-RegistryValueForFipsSettings
$reboot = $reboot -or $result


#************************************** REBOOT **************************************

if ($RebootIfRequired)
{
    Write-Log -Message "You set the RebootIfRequired flag to true. If changes are made, the system will reboot "  -Logfile $logLocation -Severity Information
    # If any settings were changed, reboot
    If ($reboot)
    {
        Write-Log -Message "Rebooting now... "  -Logfile $logLocation -Severity Information
        Write-Log -Message "Using this command: shutdown.exe /r /t 5 /c ""Crypto settings changed"" /f /d p:2:4 "  -Logfile $logLocation -Severity Information
        Write-Host "Rebooting now..."
        shutdown.exe /r /t 5 /c "Crypto settings changed" /f /d p:2:4
    }
    Else
    {
        Write-Host "Nothing get updated."
        Write-Log -Message "Nothing get updated. "  -Logfile $logLocation -Severity Information
    }
}
else
{

    Write-Log -Message "You set the RebootIfRequired flag to false. If changes are made, the system will NOT reboot "  -Logfile $logLocation -Severity Information
    Write-Log -Message "No changes will take effect until a reboot has been completed. "  -Logfile $logLocation -Severity Information
    Write-Log -Message "Script does not include a reboot by design" -Logfile $logLocation -Severity Information
}
Write-Log -Message "========== End of logging for a script execution =========="  -Logfile $logLocation -Severity Information
