
$errOut = $($dp = & { az iot ops dataflow datapoint show `
    --instance $iotOps.name `
    --name "datapoint" `
    --resource-group $rg.Name `
    --subscription $SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$dp) {
    Write-Host "Creating Data Point endpoint in $($iotOps.id)..." -ForegroundColor Cyan
    $configFileContent = @{
          mode = "Enabled"
          operations = @(
            @{
              operationType = "Source"
              sourceSettings = @{
                endpointRef = "myenpoint1"
                assetRef = ""
                serializationFormat = "Json"
                dataSources = @("testfrom")
              }
            }
            @{
              operationType = "Destination"
              destinationSettings = @{
                endpointRef = "myenpoint2"
                dataDestination = "test"
              }
            }
          )
        }
    }
    $errOut = $($dp = & { az iot ops dataflow datapoint create `
        --name "datapoint" `
        --instance $iotOps.name `
        --resource-group $rg.Name `
        --endpoint-name $eventHubEndpointName `
        --subscription $SubscriptionId `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if (-not $? -or !$dp) {
        Write-Host "Error creating Data Point endpoint - $($errOut)." `
            -ForegroundColor Red
    }
    Write-Host "Data Point endpoint $($dp.id) created in $($iotOps.id)." `
        -ForegroundColor Green
}
else {
    Write-Host "Data Point endpoint $($dp.id) exists in $($iotOps.id)." `
        -ForegroundColor Green
}