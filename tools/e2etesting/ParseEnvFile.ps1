param(
    $EnvFile,
    $SettingsToSave
)

if (!$EnvFile) {
    $EnvFile = [System.IO.Path]::Combine($PSScriptRoot, ".env")
}

if (!(Test-Path $EnvFile)) {
    Write-Host "##vso[task.complete result=Failed]Could not locate .env-File in $($EnvFile), exiting..."
}

Write-Host "Using .env file '$($EnvFile)'..."

$envContent = Get-Content $EnvFile
$allowedSettingsKeys = $SettingsToSave.Split(",")

foreach ($line in $envContent) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    if ($line.StartsWith("#")) { continue }

    $parts = $line -split "=",2

    if ($parts.Length -ne 2) { continue }
    if ([string]::IsNullOrWhiteSpace($parts[0])) { continue }
    if ([string]::IsNullOrWhiteSpace($parts[1])) { continue }

    $key = $parts[0]
    $value = $parts[1]

    if ($allowedSettingsKeys -notcontains $key) { continue }

    Write-Host "Assigning Pipelines-Variable '$($key)'..."
    Write-Host "##vso[task.setvariable variable=$($key);issecret=true]$($value)"
}