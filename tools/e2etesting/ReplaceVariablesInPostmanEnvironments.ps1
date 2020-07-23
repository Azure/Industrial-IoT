param(
    $Path,
    $ServiceUrl,
    $TenantId,
    $ClientId,
    $ClientSecret,
    $ApplicationName
)

if (!$Path) {
    $Path = $PSScriptRoot
    Write-Host "Path not specified, using '$($Path)'."
}

$environmentFiles = Get-ChildItem -Path $Path -Recurse -Filter "*.postman_environment.json"

Write-Host "Found $($environmentFiles.Length) postman environment files."

foreach ($environmentFile in $environmentFiles) {
    Write-Host "Processing '$($environmentFile.FullName)'..."
    $content = Get-Content $environmentFile.FullName -Raw

    $content = $content -replace "{{ServiceUrl}}", $ServiceUrl
    $content = $content -replace "{{TenantId}}", $TenantId
    $content = $content -replace "{{ClientId}}", $ClientId
    $content = $content -replace "{{ClientSecret}}", $ClientSecret
    $content = $content -replace "{{ApplicationName}}", $ApplicationName

    $content | Out-File $environmentFile.FullName -Encoding utf8 -Force
}
