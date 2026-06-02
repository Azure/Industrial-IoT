<#
 .SYNOPSIS
    Shared helper for emitting CI variables to both Azure DevOps and
    GitHub Actions in a backwards-compatible way.

 .DESCRIPTION
    The internal Azure DevOps pipeline consumes ##vso[task.setvariable ...]
    output. The new GitHub Actions workflows in .github/workflows/ consume
    $GITHUB_OUTPUT (job outputs) and $GITHUB_ENV (environment variables for
    later steps in the same job).

    Both targets are written transparently so the same PowerShell scripts
    work unchanged on both systems.

 .EXAMPLE
    . $PSScriptRoot/_ci.ps1
    Set-CIVariable -Name 'KeyVaultName' -Value 'my-kv-12345'
    Set-CIVariable -Name 'SshPrivateKey' -Value $key -Secret
#>

function Set-CIVariable {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Value,
        [switch] $Secret
    )

    $isGitHub = ![string]::IsNullOrWhiteSpace($env:GITHUB_ACTIONS)

    # IMPORTANT (security): on GitHub Actions, register the mask BEFORE any
    # other line that contains the secret value. The ##vso[...] line below
    # would otherwise leak the secret in plain text to the GH log because
    # GH does not parse ADO logging commands.
    if ($Secret.IsPresent -and $isGitHub) {
        # ::add-mask:: requires a single-line value; for multi-line secrets
        # (e.g. SSH private keys) we mask each non-empty line individually so
        # any partial echo in subsequent steps is redacted.
        foreach ($line in ($Value -split "`r?`n")) {
            if (![string]::IsNullOrEmpty($line)) {
                Write-Host "::add-mask::$line"
            }
        }
    }

    # Azure DevOps: ##vso[task.setvariable ...] with optional issecret=true.
    if ($Secret.IsPresent) {
        Write-Host "##vso[task.setvariable variable=$Name;issecret=true]$Value"
    }
    else {
        Write-Host "##vso[task.setvariable variable=$Name]$Value"
    }

    if (![string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
        # Multi-line safe via heredoc delimiter (random GUID).
        if ($Value -match "`r|`n") {
            $delim = [Guid]::NewGuid().ToString('N')
            "$Name<<$delim" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
            $Value          | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
            $delim          | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
        }
        else {
            "$Name=$Value" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
        }
    }

    if (![string]::IsNullOrWhiteSpace($env:GITHUB_ENV)) {
        if ($Value -match "`r|`n") {
            $delim = [Guid]::NewGuid().ToString('N')
            "$Name<<$delim" | Out-File -FilePath $env:GITHUB_ENV -Append -Encoding utf8
            $Value          | Out-File -FilePath $env:GITHUB_ENV -Append -Encoding utf8
            $delim          | Out-File -FilePath $env:GITHUB_ENV -Append -Encoding utf8
        }
        else {
            "$Name=$Value" | Out-File -FilePath $env:GITHUB_ENV -Append -Encoding utf8
        }
    }
}
