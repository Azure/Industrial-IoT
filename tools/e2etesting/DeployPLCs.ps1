Param(
    [string]
    $ResourceGroupName,
    [int]
    $NumberOfSimulations = 18,
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
    $PLCImage = "mcr.microsoft.com/iotedge/opc-plc:latest",
    [string]
    $ResourcesPrefix = "e2etesting",
    [Double]
    $MemoryInGb = 0.5,
    [int]
    $CpuCount = 1
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName not set."
}

## Login if required

$context = Get-AzContext

if (!$context) {
    Write-Host "Logging in..."
    Login-AzAccount -Tenant $TenantId
    $context = Get-AzContext
}

## Check if resource group exists

$resourceGroup = Get-AzResourceGroup -Name $resourceGroupName

if (!$resourceGroup) {
    Write-Error "Could not find Resource Group '$($ResourceGroupName)'."
}

Write-Host "Resource Group: $($ResourceGroupName)"

## Determine suffix for testing resources

$testSuffix = $resourceGroup.Tags["TestingResourcesSuffix"]

if (!$testSuffix) {
    $testSuffix = Get-Random -Minimum 10000 -Maximum 99999

    $tags = $resourceGroup.Tags
    $tags+= @{"TestingResourcesSuffix" = $testSuffix}
    Set-AzResourceGroup -Name $resourceGroup.ResourceGroupName -Tag $tags | Out-Null
    $resourceGroup = Get-AzResourceGroup -Name $resourceGroup.ResourceGroupName
}

## Check if KeyVault exists
$keyVault = Get-AzKeyVault -ResourceGroupName $ResourceGroupName

if ($keyVault.Count -ne 1) {
    Write-Error "keyVault could not be automatically selected in Resource Group '$($ResourceGroupName)'."    
} 

Write-Host "Key Vault: $($keyVault.VaultName)"

## Ensure Azure Container Instances ##

$allAciNames = @()

for ($i = 0; $i -lt $NumberOfSimulations; $i++) {
    $name = "$($ResourcesPrefix)-simulation-aci-" + $i.ToString().PadLeft(3, "0") + "-" + $testSuffix
    $allAciNames += $name
}

$existingACIs = Get-AzContainerGroup -ResourceGroupName $ResourceGroupName | select -ExpandProperty Name

$aciNamesToCreate = @()

$allAciNames | %{
    if (!@($existingACIs).Contains($_)) {
        $aciNamesToCreate += $_
    }
}

Write-Host

$jobs = @()

if ($aciNamesToCreate.Length -gt 0) {
    foreach ($aciNameToCreate in $aciNamesToCreate) {
        Write-Host "Creating ACI $($aciNameToCreate)..."

        $script = {
            Param($Name)
            $aciCommand = "/bin/sh -c './opcplc --ctb --pn=50000 --autoaccept --nospikes --nodips --nopostrend --nonegtrend --nodatavalues --sph --wp=80 --sn=$($using:NumberOfSlowNodes) --sr=$($using:SlowNodeRate) --st=$($using:SlowNodeType) --fn=$($using:NumberOfFastNodes) --fr=$($using:FastNodeRate) --ft=$($using:FastNodeType) --ph=$($Name).$($using:resourceGroup.Location).azurecontainer.io'"
            $aci = New-AzContainerGroup -ResourceGroupName $using:ResourceGroupName -Name $Name -Image $using:PLCImage -OsType Linux -Command $aciCommand -Port @(50000,80) -Cpu $using:CpuCount -MemoryInGB $using:MemoryInGb -IpAddressType Public -DnsNameLabel $Name
        }

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

$containerInstances = Get-AzContainerGroup -ResourceGroupName $ResourceGroupName | ?{ $_.Name.StartsWith($ResourcesPrefix) }

$plcSimNames = ""
foreach ($ci in $containerInstances) {
    $plcSimNames += $ci.Fqdn + ";"
}

Write-Host "Adding/Updating KeyVault-Secret 'plc-simulation-urls' with value '$($plcSimNames)'..."
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'plc-simulation-urls' -SecretValue (ConvertTo-SecureString $plcSimNames -AsPlainText -Force) | Out-Null

Write-Host "Deployment finished."

