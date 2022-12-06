Param(
    [string]
    $ResourceGroupName,
    [int]
    $NumberOfSimulations = 10,
    [Guid]
    $TenantId,
    [int]
    $NumberOfSlowNodes = 250,
    [int]
    $SlowNodeRate = 10,
    [string]
    $SlowNodeType =  "uint",
    [int]
    $NumberOfFastNodes = 50,
    [int]
    $FastNodeRate = 1,
    [string]
    $FastNodeType = "uint",
    [string]
    $PLCImage,
    [string]
    $ResourcesPrefix = "e2etesting",
    [Double]
    $MemoryInGb = 0.5,
    [int]
    $CpuCount = 1,
    [bool]
    $UsePrivateIp = $false
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$PLCImage) {
    $PLCImage = "mcr.microsoft.com/iotedge/opc-plc:2.5.7"
}

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName not set."
}

## Check if resource group exists
$resourceGroup = az group show --name $ResourceGroupName | ConvertFrom-Json

if (!$resourceGroup) {
    Write-Error "Could not find Resource Group '$($ResourceGroupName)'."
}

Write-Host "Resource Group: $($ResourceGroupName)"

## Determine suffix for testing resources

$testSuffix = $resourceGroup.tags.TestingResourcesSuffix

if (!$testSuffix) {
    $testSuffix = Get-Random -Minimum 10000 -Maximum 99999
    # TODO: don't override the original tags
    az group update --name $resourceGroup --tags TestingResourcesSuffix=$testSuffix
}

## Check if KeyVault exists
$keyVault = "e2etestingkeyVault" + $testSuffix
$keyVaultList = az keyvault list --resource-group $ResourceGroupName | ConvertFrom-Json
if ($keyVaultList.Count -ne 1){
    Write-Error "keyVault could not be automatically selected in Resource Group '$($ResourceGroupName)'."  
}

$keyVault = $keyVaultList.name

Write-Host "The following Key Vault has been selected: $keyVault"

## Ensure Azure Container Instances ##

$allAciNames = @()

for ($i = 0; $i -lt $NumberOfSimulations; $i++) {
    $name = "$($ResourcesPrefix)-simulation-aci-" + $i.ToString().PadLeft(3, "0") + "-" + $testSuffix
    $allAciNames += $name
}

$existingACIs = az container list --resource-group $ResourceGroupName  --query "[?starts_with(name,'$ResourcesPrefix') ].name"  | ConvertFrom-Json

$aciNamesToCreate = @()

$allAciNames | %{
    if (!@($existingACIs).Contains($_)) {
        $aciNamesToCreate += $_
    }
}

Write-Host "Creating container instances with PLC Image $($PLCImage)..."

$jobs = @()

if ($aciNamesToCreate.Length -gt 0) {
    if ($UsePrivateIp -eq $false) {
        $script = {
            Param($Name)

            # create
            $ports = @(50000, 80)
            az container create --resource-group $using:ResourceGroupName --name $Name --image $using:PLCImage --os-type Linux --ports @ports --cpu $using:CpuCount --memory $using:MemoryInGb --ip-address Public --dns-name-label $Name
            $container = az container show --resource-group $using:ResourceGroupName --name $Name | ConvertFrom-Json
            if (!$container.ipAddress.ip) {
                throw "Create container failed with exit code: $LASTEXITCODE"
            }
            # update
            $aciCommand = "/bin/sh -c './opcplc --ph $($container.ipAddress.fqdn) --cdn $($container.ipAddress.ip) --ctb --pn=50000 --autoaccept --nospikes --nodips --nopostrend --nonegtrend --nodatavalues --sph --wp=80 --sn=$($using:NumberOfSlowNodes) --sr=$($using:SlowNodeRate) --st=$($using:SlowNodeType) --fn=$($using:NumberOfFastNodes) --fr=$($using:FastNodeRate) --ft=$($using:FastNodeType)'"
            az container create --resource-group $using:ResourceGroupName --name $Name --image $using:PLCImage --os-type Linux --ports @ports --cpu $using:CpuCount --memory $using:MemoryInGb --ip-address Public --dns-name-label $Name --command $aciCommand
            if ($LASTEXITCODE -ne 0) {
                az container delete -y --resource-group $using:ResourceGroupName --name $Name
                throw "Update container failed with exit code: $LASTEXITCODE"
            }
        }
    }
    else {
        Write-Host "Creating containers with private IP addresses"
        ## Set vNet and subNet parameters for nested edge
        $networkResourceGroup = $ResourceGroupName + "-RG-network"
        $vNet =  az resource show --name "PurdueNetwork" --resource-group $networkResourceGroup --resource-type "Microsoft.Network/virtualNetworks" --query id
        $subNet = "3-L2-OT-AreaSupervisoryControl"
        Write-Host "vNet resource id is $vNet"

        $script = {
            Param($Name)

            #create
            $ports = @(50000, 80)
            az container create --resource-group $using:ResourceGroupName --name $Name --image $using:PLCImage --os-type Linux --ports @ports --cpu $using:CpuCount --memory $using:MemoryInGb --ip-address Private --vnet $using:vNet --subnet $using:subNet
            $container = az container show --resource-group $using:ResourceGroupName --name $Name | ConvertFrom-Json
            if (!$container.ipAddress.ip) {
                throw "Create container failed with exit code: $LASTEXITCODE"
            }

            # update
            $aciCommand = "/bin/sh -c './opcplc --ph $($container.ipAddress.fqdn) --cdn $($container.ipAddress.ip) --ctb --pn=50000 --autoaccept --nospikes --nodips --nopostrend --nonegtrend --nodatavalues --sph --wp=80 --sn=$($using:NumberOfSlowNodes) --sr=$($using:SlowNodeRate) --st=$($using:SlowNodeType) --fn=$($using:NumberOfFastNodes) --fr=$($using:FastNodeRate) --ft=$($using:FastNodeType)'"
            az container create --resource-group $using:ResourceGroupName --name $Name --image $using:PLCImage --os-type Linux --ports @ports --cpu $using:CpuCount --memory $using:MemoryInGb --ip-address Private --vnet $using:vNet --subnet $using:subNet --command $aciCommand
            if ($LASTEXITCODE -ne 0) {
                az container delete -y --resource-group $using:ResourceGroupName --name $Name
                throw "Update container failed with exit code: $LASTEXITCODE"
            }
        }
    }

    foreach ($aciNameToCreate in $aciNamesToCreate) {
        Write-Host "Creating ACI $($aciNameToCreate)..."
        $job = Start-Job -Scriptblock $script -ArgumentList $aciNameToCreate
        $jobs += $job
    }

    Write-Host "Waiting for deployments to finish..."

    $working = $true

    while($working) {
        $working = $false
        foreach ($job in $jobs) {
            if ($job.JobStateInfo.State -eq 'Running') {
                $working = $true
                break
            }
        }
        Start-Sleep -Seconds 1
    }

    Write-Host "Deployment finished."

    Wait-Job -Job $jobs | Out-Null

    foreach ($job in $jobs) {
        if ($job.JobStateInfo.State -ne 'Completed') {
            Receive-Job -Job $job
            Write-Error "Error while deploying ACI."
        }
    }
}

## Write ACI FQDNs to KeyVault ##
Write-Host
Write-Host "Getting IPs of ACIs for simulated PLCs..."
$ipList = az container list --resource-group $ResourceGroupName  --query "[?starts_with(name,'$ResourcesPrefix') ].ipAddress.ip"  | ConvertFrom-Json

if ($ipList.Count -eq 0) {
    Write-Error "No Azure Container Instances have been deployed. Please check that quota is not exceeded for this region."
}

foreach ($ip in $ipList) {
    Write-Host $ip
    $plcSimNames += $ip + ";"
}

try {
    Write-Host "Adding/Updating KeyVault-Secret 'plc-simulation-urls' with value '$($plcSimNames)'..."
    az keyvault secret set --vault-name $keyVault --name "plc-simulation-urls" --value $plcSimNames > $null
}
catch {
    Write-Host "Plc simulation urls are empty"
}

Write-Host "Deployment finished."