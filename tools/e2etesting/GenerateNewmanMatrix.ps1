param($Path)

if (!$Path) {
    $Path = $PSScriptRoot
    Write-Host "Path not specified, using '$($Path)'."
}

$collectionFiles = Get-ChildItem -Path $Path -Recurse -Filter "*.postman_collection.json"

$jobs = @{}

foreach ($collectionFile in $collectionFiles) {
    Write-Host "Adding collection '$($collectionFile.FullName)'  to job list..."
    $jobs.Add($collectionFile.Name, @{ "collectionFile" = $collectionFile.Fullname;  })
}

Write-Host ("##vso[task.setVariable variable=postmanJobsMatrix;isOutput=true] {0}" -f ($jobs | ConvertTo-Json -Compress))
