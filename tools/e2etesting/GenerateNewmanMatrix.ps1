param($Path)

if (!$Path) {
    $Path = $PSScriptRoot
    Write-Host "Path not specified, using '$($Path)'."
}

$collectionFiles = Get-ChildItem -Path $Path -Recurse -Filter "*.postman_collection.json"

$jobs = @{}

foreach ($collectionFile in $collectionFiles) {
    $environmentFile = $collectionFile.FullName.Replace("postman_collection", "postman_environment")

    if (!(Test-Path $environmentFile)) {
        Write-Host "Could not locate environment file '$($environmentFile)' for collection '$($collectionFile.FullName)', skipping..."
        continue
    }

    Write-Host "Adding collection '$($collectionFile.FullName)' with environment '$($environmentFile)' to job list..."
    $jobs.Add($collectionFile.Name, @{ "collectionFile" = $collectionFile.Fullname; "environmentFile" = $environmentFile })
}

Write-Host ("##vso[task.setVariable variable=postmanJobsMatrix;isOutput=true] {0}" -f ($jobs | ConvertTo-Json -Compress))

