<#
    .SYNOPSIS
        Make a pod debuggable by attaching a debugger container to it.
    .DESCRIPTION
        This script builds a Docker image for the vsdbg debugger and attaches
        it to a specified pod in a Kubernetes cluster. It requires Docker and
        kubectl to be installed and configured.
    .PARAMETER PodName
        The name of the pod to debug.
    .PARAMETER ContainerName
        The name of the container in the pod to debug.
    .PARAMETER Namespace
        The Kubernetes namespace where the pod is located.
    .PARAMETER ClusterType
        The type of Kubernetes cluster. Default is "microk8s".
    .PARAMETER Image
        If specified, the script will replace the existing image in the pod
        See --set-image in kubectl debug documentation.
#>

param(
    [string] $PodName,
    [string] $ContainerName,
    [string] $Namespace,
    [string] $ClusterType = "microk8s",
    [string] $Image,
    [switch] $Fork
)

$ErrorActionPreference = 'Stop'
$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Path
Import-Module $(Join-Path $(Join-Path $(Split-Path $scriptDirectory) "common") `
    "cluster-utils.psm1") -Force

# Get a list of pods and let user select one
if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
    Write-Host "kubectl is not installed or not in the PATH." -ForegroundColor Red
    exit -1
}
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "docker is not installed or not in the PATH." -ForegroundColor Red
    exit -1
}

# get list of namespaces and let user select one
$namespaces = kubectl get namespaces --no-headers | ForEach-Object {
    $_.Split()[0]
}
if (-not $namespaces) {
    Write-Host "No namespaces found." -ForegroundColor Red
    exit -1
}
if (-not $Namespace) {
    $Namespace = $namespaces | Out-GridView -Title "Select a namespace" -PassThru
    if (-not $Namespace) {
        Write-Host "No namespace selected." -ForegroundColor Yellow
        exit 0
    }
}
else {
    if (-not $namespaces.Contains($Namespace)) {
        Write-Host "Namespace '$Namespace' not found." -ForegroundColor Red
        exit -1
    }
    Write-Host "Using namespace '$Namespace'." -ForegroundColor Green
}

# get list of pods in the namespace and let user select one
$pods = kubectl get pods -n $Namespace --no-headers | ForEach-Object {
    $_.Split()[0]
}
if (-not $pods) {
    Write-Host "No pods found in namespace '$Namespace'." -ForegroundColor Red
    exit -1
}
if (-not $PodName) {
    $PodName = $pods | Out-GridView -Title "Select a pod to debug" -PassThru
    if (-not $PodName) {
        Write-Host "No pod selected." -ForegroundColor Yellow
        exit 0
    }
}
else {
    if (-not $pods.Contains($PodName)) {
        Write-Host "Pod '$PodName' not found in namespace '$Namespace'." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Using pod '$PodName'." -ForegroundColor Green
}

# Remove debug tar image if it exists
Remove-Item -Path debugger.tar -Force -ErrorAction SilentlyContinue

# Build and make .net debugger image available in cluster
$containerTag = Get-Date -Format "MMddHHmmss"
Write-Host "Building debugger image with tag '$containerTag'..." `
    -ForegroundColor Cyan
docker build --progress auto -f Dockerfile -t debugger:$($containerTag) .
docker image save debugger:$($containerTag) -o debugger.tar
Write-Host "Debugger image built with tag '$containerTag'." `
    -ForegroundColor Green

Import-ContainerImage -ClusterType $script:ClusterType `
    -ContainerImage "debugger:$($containerTag)"

while ($true) {
    # Get main container name if not specified
    $containers = $(kubectl get pod $PodName -n $Namespace `
            -o jsonpath-as-json='{.spec.containers[*].name}') | ConvertFrom-Json
    if (-not $containers -or $containers.Count -eq 0) {
        Write-Host "No containers found in pod '$PodName'." -ForegroundColor Red
        exit -1
    }
    # If ContainerName is not specified, use the first container
    if (-not $ContainerName) {
        # if one use it otherwise let user select one
        if ($containers.Count -eq 1) {
            $ContainerName = $containers
            Write-Host "Using container '$ContainerName' in pod '$PodName'." `
                -ForegroundColor Green
        }
        else {
            $ContainerName = $containers `
                | Out-GridView -Title "Select a container to debug" -PassThru
            if (-not $ContainerName) {
                Write-Host "No container selected." -ForegroundColor Yellow
                exit 0
            }
        }
    }
    else {
        if (-not $containers.Contains($ContainerName)) {
            Write-Host "Container '$ContainerName' not found in pod '$PodName'." `
                -ForegroundColor Red
            exit -1
        }
        Write-Host "Attach debugger to container '$ContainerName' in pod '$PodName'." `
            -ForegroundColor Green
    }

    $ephemeralContainers = $(kubectl get pod $PodName -n $Namespace `
        -o jsonpath-as-json='{.spec.ephemeralContainers[*].name}') | ConvertFrom-Json
    if (-not $ephemeralContainers -or -not $ephemeralContainers.Contains("debugger")) {
        # Debugger container is not attached yet, so we can proceed
        break
    }
    Write-Host "Debugger already attached to pod '$PodName' - Restarting pod." `
        -ForegroundColor Yellow
    # Delete the current pod so we can recreate it with the debug container
    kubectl -n $Namespace delete pod $PodName --wait=true
    # Now wait for the pod to be restarted
    Write-Host "Waiting for pod '$PodName' to be restarted..." -ForegroundColor Cyan
    while ($true) {
        $podStatus = kubectl get pod $PodName -n $Namespace -o jsonpath='{.status.phase}'
        if ($podStatus -eq "Running") {
            Write-Host "Pod '$PodName' is running."`
                -ForegroundColor Green
            break
        }
        elseif ($podStatus -eq "Pending") {
            Write-Host "Pod '$PodName' is pending..."`
                -ForegroundColor Yellow
        }
        else {
            Write-Host "Pod '$PodName' is in state '$podStatus'." `
                -ForegroundColor Red
            exit -1
        }
        Start-Sleep -Seconds 1
    }
}

if ($script:Fork.IsPresent) {
    # Check if the pods already contain a debug pod
    $debugPod = "$($PodName)-debug"
    if ($pods.Contains($debugPod)) {
        # delete the existing debug pod
        Write-Host "Debug pod '$($debugPod)' already exists - deleting it." `
            -ForegroundColor Yellow
       # kubectl -n $Namespace delete pod $debugPod --wait=true
    }

    # Attach the debugger container to the specified pod and container
    Write-Host "Forking pod '$PodName' with container '$ContainerName' to '$($debugPod)'..." `
        -ForegroundColor Cyan
    if ($Image) {
         # Replace the existing image in the pod
        kubectl -n $Namespace debug $PodName -n $($Namespace) `
            -it --attach=false `
            --image=docker.io/library/debugger:$($containerTag) `
            --profile=general `
            --container=debugger `
            --keep-annotations=true `
            --keep-labels=true `
            --keep-init-containers=true `
            --keep-liveness=true `
            --keep-readiness=true `
            --keep-startup=true `
            --same-node=true `
            --set-image="$($ContainerName)=$($Image)" `
            --image-pull-policy=IfNotPresent `
            --share-processes=true `
            --copy-to=$($debugPod) `
    }
    else {
        kubectl -n $Namespace debug $PodName -n $($Namespace) `
            -it --attach=false `
            --image=docker.io/library/debugger:$($containerTag) `
            --profile=general `
            --container=debugger `
            --keep-annotations=true `
            --keep-labels=true `
            --keep-init-containers=true `
            --keep-liveness=true `
            --keep-readiness=true `
            --keep-startup=true `
            --same-node=true `
            --image-pull-policy=IfNotPresent `
            --share-processes=true `
            --copy-to=$($debugPod) `
    }
    if (-not $?) {
        Write-Host "Failed to fork pod '$PodName' with container '$ContainerName' to '$($debugPod)'." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Debugging pod '$PodName' as '$($debugPod)' with container '$ContainerName'." `
        -ForegroundColor Green
    $PodName = $debugPod
}
elseif ($Image) {
    # Set the image to the specified one
    Write-Host "Replacing image in pod '$PodName' for container '$ContainerName' with '$Image'..." `
        -ForegroundColor Cyan
    kubectl set image "pod/$($PodName)" "$($ContainerName)=$($Image)" -n $($Namespace)
    if (-not $?) {
        Write-Host "Failed to replace image in pod '$PodName' for container '$ContainerName'." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Image in pod '$PodName' for container '$ContainerName' replaced with '$Image'." `
        -ForegroundColor Green
}

# Now wait for the pod to start
Write-Host "Checking state of pod '$PodName'..." -ForegroundColor Cyan
while ($true) {
    $podStatus = kubectl get pod $PodName -n $Namespace -o jsonpath='{.status.phase}'
    if ($podStatus -eq "Running") {
        if ($Image) {
            $podDescription = $(kubectl get pod $PodName -n $Namespace -o json) `
                | ConvertFrom-Json
            $images = $podDescription.spec.containers.image
            if ($images -contains $Image) {
                Write-Host "Pod '$PodName' is running with '$Image'."`
                    -ForegroundColor Green
                break
            }
            else {
                Write-Host "Pod '$PodName' is running but still using the old image." `
                    -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "Pod '$PodName' is running."`
                -ForegroundColor Green
            break
        }
    }
    elseif ($podStatus -eq "Pending") {
        Write-Host "Pod '$PodName' is pending..."`
            -ForegroundColor Yellow
    }
    else {
        Write-Host "Pod '$PodName' is in state '$podStatus'." `
            -ForegroundColor Red
        exit -1
    }
    Start-Sleep -Seconds 1
}

if (-not $script:Fork.IsPresent) {

    Write-Host "Attaching debugger to pod '$PodName' and container '$ContainerName'..." `
        -ForegroundColor Cyan
    # Attach to running container
    kubectl -n $Namespace debug $PodName -n $($Namespace) `
        --image=docker.io/library/debugger:$($containerTag) `
        --target=$ContainerName `
        --profile=general `
        --container=debugger `
        --image-pull-policy=IfNotPresent `
        --share-processes=true `
        -it --attach=false

    Write-Host "Debugging pod '$PodName' and container '$ContainerName'." `
        -ForegroundColor Green
}

Remove-Item -Path debugger.tar
Write-Host "Visual Studio Debugger listening on localhost:50000." `
    -ForegroundColor Green
kubectl port-forward -n $($Namespace) --address=0.0.0.0 pod/$($PodName) 50000:22

