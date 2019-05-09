Param(
    [string]
    $WorkingDirectory,

    <# The full url of the repository. For better performance, it's recommended to use a local registry. Example: "localhost:5000/samples/opc-publisher" #>
    #[Parameter(Mandatory=$true)]
    [string]
    $Repository,

    <# The name of the Dockerfile to build. If not set, the script scans for "Docker*debug"-files recursivly in the current path. If not found, it  #>
    [string]
    $DockerFile,

    <# If you use a local registry (Repository starting with 'localhost'), the script can ensure that the local registry is running. That takes some time, so you can skip this check with this parameter. #>
    [switch]
    $SkipLocalRegistryCheck,


    <# Flag if the module manifest should be updated. If set, the ModulesFile-Parameter must be set. #>
    [switch]$UpdateModuleManifest,

    <# The name of the module in IoT Edge. If not set, the module name is determined by the repository name (last part after the /). #>
    [string]$ModuleName,

    <# The connection string of the IoT Hub (not the device!). Must have owner rights. Example: "HostName=iothub-kenbk.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=XXX" #>
    [string]
    $IothubConnectionString,

    <# The Id of the Edge device in IoT Hub. Example: "SampleDevice" #>
    [string]
    $DeviceId,

    <# The file with the module deployment template. Must contain the Placeholder {{LABEL}}. Example: ".\modules.json" #>
    [string]
    $ModulesFile,

    <# Flag if the script should attach to the log output after the module has been updated. #>
    [switch]
    $AttachToLogs
)

if (!$WorkingDirectory) {
    $WorkingDirectory = (Get-Location).Path
}

if (!$Repository) {
    $sln = Get-ChildItem -Path $WorkingDirectory -Filter "*.sln" | select -First 1

    if ($sln) {
        Write-Host "Repository not specified, trying to construct using solution file name '$($sln.FullName)'..."
        $sln = $sln.Name.Replace($sln.Extension, "")
        $Repository = "localhost:5000/$sln"
    }
}

if (!$Repository) {
    Write-Error "Please specify a repository name."
    return
}

if (!$ModuleName) {
    $ModuleName = $Repository.Split("/") | select -Last 1
}

if (!$ModulesFile) {
    $ModulesFile = Join-Path $WorkingDirectory "modules.json"
}

if (!$DockerFile) {
    $DockerFile = Get-ChildItem -Path $WorkingDirectory -Filter "Docker*debug" -Recurse | select -ExpandProperty FullName | select -First 1

    if (!$DockerFile) {
        $DockerFile = Join-Path $WorkingDirectory "Dockerfile"
    }
}

Write-Host "Dockerfile: $($Dockerfile)"
Write-Host "Repository: $($Repository)"
Write-Host "Module Name: $($ModuleName)"
Write-Host "IoT Hub Connection String: $($IothubConnectionString)"
Write-Host "Device Id: $($DeviceId)"
Write-Host "Update Module Manifest: $($UpdateModuleManifest)"
Write-Host "Modules File: $($ModulesFile)"
Write-Host "Attach to logs: $($AttachToLogs)"
Write-Host

# Check if specified Dockerfile exists.
if (!(Test-Path $DockerFile -ErrorAction SilentlyContinue)) {
    Write-Error "Could not find Dockerfile '$($Dockerfile)'."
    return
}

# Validate parameters for module update
if ($UpdateModuleManifest) {
    if (!$IothubConnectionString -or !$DeviceId -or !$ModuleName) {
        Write-Error "To update the modules in Iot Hub, IotHubConnectionString, DeviceId and ModuleName are required."
        return
    }

    if (!(Test-Path $ModulesFile -ErrorAction SilentlyContinue)) {
        Write-Error "Could not find Modules-File $($ModulesFile)."
        return
    }

    $modules = Get-Content $ModulesFile -Raw

    if (!$modules.Contains("{{LABEL}}") -or !$modules.Contains("{{MODULENAME}}")) {
        Write-Error "The given modules-file does not contain placeholder {{LABEL}} and/or {{MODULENAME}}"
        return
    }

    $command = Get-Command "az.cmd" -ErrorAction SilentlyContinue

    if (!$command) {
        Write-Error "Azure CLI ist not installed: Please install it first from here and rerun the script: https://aka.ms/installazurecliwindows"
        return
    }
}

Write-Host

Write-Host "Checking, if Docker is installed..."
# Check if docker command is available
$command = Get-Command "docker.exe" -ErrorAction SilentlyContinue

if (!$command) {
    Write-Error "docker.exe-Command is not available. Please make sure that docker is installed."
    return
}

Write-Host "Checking, if Docker is up and running..."
# Check if docker is running
$dockerRunning = . docker info

if ($LASTEXITCODE -ne 0) {
    Write-Host $dockerRunning
    Write-Error "Please make sure that docker is running correctly."
    return
}

if (!$SkipLocalRegistryCheck) {
    # Check if a local registry should be used. If so, check if it is available. If not, create and start it.
    if ($Repository.ToLower().StartsWith("localhost")) {
        $port = [int]::Parse($Repository.Split("/")[0].Split(":")[1])

        Write-Host "Checking if local registry exists..."
        $registryContainer = . docker container ls --format "{{json .}}" -a | ConvertFrom-Json | ? { $_.Names -eq "registry"}

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Error while getting containers."
            return
        }

        if (!$registryContainer) {
            Write-Host "Local registry not found, creating..."
            . docker run -d -p "$($port):$($port)" --restart=always --name registry registry:2
            #. docker run -d -p 5000:5000 --restart=always --name registry registry:2

            if ($LASTEXITCODE -ne 0) {
                Write-Error "Could not create local registry. Try to restart local Docker."
                return
            }

            Write-Host "Local registry created."
        }

        $containerInfo = (. docker inspect registry | ConvertFrom-Json)[0]

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Error while getting container info."
            return
        }

        if ($containerInfo.State.Status -ne "running") {
            Write-Host "Container is not running, starting..."
            . docker start registry

            if ($LASTEXITCODE -ne 0) {
                Write-Error "Could not start local registry."
                return
            }

            Write-Host "Registry started."
        }

        Write-Host "Registry is running."
    }
}

Write-Host

Write-Host "Getting latest tag from local images..."

# Determine the tag and label

[Double]$latestTag = -1

(. docker images --format "{{json . }}" $Repository) | ConvertFrom-Json | select -ExpandProperty Tag | % {
    try {
        $latestTag = [Math]::Max($latestTag, [Double]::Parse($_, [CultureInfo]::InvariantCulture))
    }
    catch {}
}

if ($latestTag -eq -1) {
    $latestTag = 1
}

$latestTagAsString = $latestTag.ToString([CultureInfo]::GetCultureInfo("en-GB"))
$newTag = [Double]$latestTag + 0.001
$newTagAsString = $newTag.ToString([CultureInfo]::GetCultureInfo("en-GB"))

Write-Host "Latest tag: $($latestTagAsString)"
Write-Host "Incrementing tag to: $($newTagAsString)"

$label = $repository + ":" + $newTagAsString

Write-Host "New Label: $($label)"
Write-Host

$location = (Get-Location).Path
Set-Location $WorkingDirectory

Write-Host "Building docker image..."
. docker build -t "$($label)" -f "$($DockerFile)" .

Set-Location $location

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error while building docker image. Check logs for details."
    return
}

. docker push "$($label)"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error while pushing image to registry. Check logs for details."
    return
}

if ($UpdateModuleManifest) {
    Write-Host
    $iotHubName = $IothubConnectionString.Split(";")[0].Split("=")[1].Split(".")[0]

    Write-Host "Updating module manifest..."
    $modules = Get-Content $ModulesFile -Raw
    $modules = $modules.Replace("{{LABEL}}", $label).Replace("{{MODULENAME}}", $ModuleName)

    $tempFilename = Join-Path $env:TEMP ([Guid]::NewGuid())

    Write-Host "Saving module manifest as temp file to $($tempFilename)..."
    $modules | Out-File $tempFilename

    Write-Host "Adding IoT Edge Az extension..."
    az extension add --name azure-cli-iot-ext

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Error while adding azure-cli-iot-ext Extension to Az."
        return
    }

    Write-Host "Updating modules in IoT Edge device..."
    az iot edge set-modules --hub-name "$($iotHubName)" --device-id "$($DeviceId)" --content "$($tempFilename)" -l "$($IothubConnectionString)" | Out-Null
    Remove-Item $tempFilename -Force
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Error while updating modules in Edge device."
        return
    }
}

if ($AttachToLogs) {
    $service = Get-Service iotedge -ErrorAction SilentlyContinue

    if (!$service) {
        Write-Host "IoT Edge Runtime not installed"

        $ioTEdgeDeviceConnectionString = . az iot hub device-identity show-connection-string --device-id "$($DeviceId)" --login "$($IothubConnectionString)" | ConvertFrom-Json

        if (!$ioTEdgeDeviceConnectionString) {
            Write-Error "Could not retrieve Iot Edge Device Connection string."
            return
        }

        $ioTEdgeDeviceConnectionString = $ioTEdgeDeviceConnectionString.connectionString

        Write-Host "Install IoT Edge Runtime..."
        . {Invoke-WebRequest -useb aka.ms/iotedge-win} | Invoke-Expression; Install-SecurityDaemon -Manual -ContainerOs Linux -DeviceConnectionString "$($ioTEdgeDeviceConnectionString)"
    }

    Write-Host
    Write-Host "Waiting for updated Edge module..." -NoNewline

    $command = Get-Command "iotedge" -ErrorAction SilentlyContinue

    if (!$command) {
        Write-Error "Could not find iotedge runtime."
        return
    }

    $found = $false
    while (!$found) {
        Start-Sleep -Seconds 2
        $modules = . iotedge list

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Error while calling the IoT Edge Runtime."
            return
        }

        foreach ($module in $modules) {
            if ($module.ToLower().Contains($label.ToLower())) {
                $found = $true
                break
            }
        }

        Write-Host "." -NoNewline
    }

    Write-Host "Done."

    Write-Host
    Write-Host "Displaying log output. Press STRG+C to exit..."
    Write-Host "=============================================="
    Write-Host

    . docker logs -f $ModuleName
}

