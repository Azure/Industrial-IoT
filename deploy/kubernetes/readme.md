# Azure IoT Operations Development Environment Setup

This guide explains how to set up a local development environment for Azure IoT Operations (AIO) and how to debug OPC Publisher.

> This functionality is **experimental**. OPC Publisher running as connector in Azure IoT Operations is not supported by Microsoft.
> **Use the Azure IoT Operations built in OPC UA connectivity support for production!**

## Cluster Setup

The [`cluster-setup.ps1`](./cluster-setup.ps1) PowerShell script sets up a local Kubernetes cluster and connects it to Azure Arc.

> Only version 1.2 or higher of Azure IoT Operations is supported.

### Prerequisites

- Windows 11 with PowerShell
- Azure subscription with required permissions
- If you intend to use microk8s - install it using the public installer **including multipass** before running the script.

### Key Parameters

> This script must be run as administrator and is not intended to set up production clusters.

```powershell
.\cluster-setup.ps1 -Name <cluster-name> [OPTIONS]

Required:
  -Name                 # Name for the cluster and associated resources

Optional:
  -InstanceName         # Name of the AIO instance (default: same as cluster name)
  -InstanceNamespace    # Kubernetes namespace (default: azure-iot-operations)
  -ResourceGroup        # Azure resource group (default: same as cluster name)
  -SharedFolderPath     # Path for shared storage (default: C:\Shared)
  -Location             # Azure region (default: westus)
  -ClusterType          # Cluster type: microk8s, kind, k3d, minikube (default: microk8s)
  -Connector            # OPC Publisher deployment: None, Official, Local, Debug (default: Local)
  -OpsVersion           # Azure IoT Operations version
  -OpsTrain             # Release train: integration, stable, dev (default: integration)
  -Force                # Force reinstallation
```

## Debugging OPC Publisher Akri Connector

The debug folder contains scripts to build and debug the OPC Publisher.

### Building

Use [`debug/build.ps1`](./debug/build.ps1) to build a debug version.

```powershell
.\debug\build.ps1 [OPTIONS]

Options:
  -Configuration        # Build configuration: Debug or Release (default: Debug)
  -ClusterType          # Target cluster type (default: microk8s)
  -StartDebugger        # Start debugger after build
```

The script:

1. Builds OPC Publisher in debug configuration
2. Creates and tags a container image
3. Imports the image into your cluster
4. Optionally starts the `debug.ps1` script pointing to the built container.

### Attaching the Debugger

Use [`debug/debug.ps1`](./debug/debug.ps1) to attach the debugger:

```powershell
.\debug\debug.ps1 [OPTIONS]

Options:
  -PodName              # Target pod name (will prompt if not specified)
  -ContainerName        # Target container name (will prompt if not specified)
  -Namespace            # Kubernetes namespace (default: azure-iot-operations)
  -ClusterType          # Cluster type (default: microk8s)
  -ReplaceImage         # Replace existing image in the pod
```

The script:

1. Builds a debugger container
2. Attaches it to the target pod
3. Sets up port forwarding for the debugger (localhost:50000)

### Example Connector Debug Workflow

0. If you are using microk8s, install it using the public instructions. Otherwise replace `microk8s` below with another cluster type (will be installed)

1. Open a terminal as Administrator and deploy a full Azure IoT Operations (minimum version is 1.2.x) cluster using

    ```powershell
    .\cluster-setup.ps1 -Name <cluster-name> -Connector Local -ClusterType microk8s
    ```

2. Build a debug version of OPC Publisher.

    ```powershell
    cd .\debug
    .\build.ps1 -Configuration Debug -ClusterType microk8s -StartDebugger
    ```

3. Follow the prompts to select the namespace (e.g.azure-iot-operations) and a OPC Publisher pod

4. Connect your IDE to the debug port

   1. Open the solution file in Visual Studio 2022 and "Debug->Attach to Process...
   2. Select Connection type: SSH
   3. Connect to localhost:50000 with user name "root" and password as displayed by build/debug.ps1 script
   4. Select the "dotnet" process to attach to.
