# Azure IoT Operations Development Environment Setup

This guide explains how to set up a local development environment for Azure IoT Operations (AIO)
and how to install OPC Publisher as a Akri connector.

> This functionality is **experimental**. OPC Publisher running as connector in Azure IoT Operations
> is not supported by Microsoft.
> **Use the Azure IoT Operations built in OPC UA connectivity support for production!**

## Cluster Setup

The [`cluster-setup.ps1`](./cluster-setup.ps1) PowerShell script sets up a local Kubernetes
cluster and connects it to Azure Arc. It is a convenient way to boot strap a Azure IoT
Operations cluster on Windows targeting K3D, MicroK8s, Minikube and Kind.

> Linux is currently not supported / has not been tested.
> Currently only K3D and MicroK8s have been tested.
> Only version 1.2 or higher of Azure IoT Operations is supported which is currently in
> preview.

### Prerequisites

- Windows 11 with PowerShell.
- Azure subscription with required permissions
- All other dependencies are installed, however, if you intend to use microk8s install it using the
  [public installer](https://microk8s.io/docs/install-windows) **including multipass** before
  running the script.

> The script must be run as administrator and is not intended to set up production clusters.

### Using the cluster setup script

```powershell
# Basic usage
.\cluster-setup.ps1 -Name <cluster-name>

# Full syntax with all parameters
.\cluster-setup.ps1 `
    -Name <cluster-name> `                          # Required: Cluster and resource name
    [-InstanceName <instance-name>] `               # Optional: AIO instance name (default: same as cluster)
    [-InstanceNamespace <namespace>] `              # Optional: K8s namespace (default: azure-iot-operations)
    [-ResourceGroup <resource-group>] `             # Optional: Resource group (default: same as cluster)
    [-SharedFolderPath <path>] `                    # Optional: Shared storage path (default: C:\Shared)
    [-Location <location>] `                        # Optional: Azure region (default: westus)
    [-ClusterType <type>] `                         # Optional: microk8s, kind, k3d, minikube (default: microk8s)
    [-Connector <type>] `                           # Optional: None, Official, Local, Debug (default: Official)
    [-OpsVersion <version>] `                       # Optional: Azure IoT Operations version
    [-OpsTrain <train>] `                           # Optional: integration, stable, dev (default: integration)
    [-Force]                                        # Optional: Force reinstallation

# Examples

# Basic setup with defaults
.\cluster-setup.ps1 -Name "myCluster"

# Custom configuration with different cluster type
.\cluster-setup.ps1 `
    -Name "myCluster" `
    -ClusterType "kind" `
    -InstanceNamespace "iot-ops" `
    -Connector "Local"

# Development setup with specific version and train
.\cluster-setup.ps1 `
    -Name "devCluster" `
    -OpsVersion "1.2.31" `
    -OpsTrain "integration" `
    -Connector "Debug" `
    -Force

```

### Akri and Azure Asset and Device Registry Integration

The cluster setup script by default deploys the officially built OPC Publisher as a Akri
connector into the cluster. The connector will handle publishing of data that is configured
in Assets that are bound to a device endpoint. These can be created in ADR using the AZ CLI
as well as the Digital Operations experience user interface.

There are 2 devices installed by default pointing to 2 running OPC PLC simulation servers
running in the cluster.

The connector will discover assets in OPC PLC via the AssetTypes configuration option on
the device "none" endpoint. It will also add all endpoints of the server to the device
and publishes a discovered device resource to Akri with the additional endpoints and the
same identifier as the original device.

For convenience the script also enables the insecure listener on the MQTT broker. Forward
the insecure port (1883) locally using the `.\debug\forward-mq.ps1`.

### Onboarding discovered Assets and Devices

The import or onboarding flow can be managed using the Digital Operations Experience user
interface. The `.\iotops\onboard-discovered-resources.ps1` powershell script can be used
to effectively copy the discovered resource into Azure Device Registry and allows you to
bypass the DOE experience.

#### Using the onboarding script

```powershell
# Basic usage
.\iotops\onboard-discovered-resources.ps1 -AdrNamespaceName <namespace> -ResourceGroup <resource-group>

# Full syntax with all options
.\iotops\onboard-discovered-resources.ps1 `
    -AdrNamespaceName <namespace> `                   # Required: ADR namespace name
    -ResourceGroup <resource-group> `                 # Required: Azure resource group
    [-SubscriptionId <subscription-id>] `             # Optional: Azure subscription ID
    [-Location <location>] `                          # Optional: Azure region (default: westus)
    [-TenantId <tenant-id>] `                         # Optional: Azure tenant ID
    [-RunOnce] `                                      # Optional: Run once and exit
    [-Force] `                                        # Optional: Force update existing resources
    [-SkipLogin]                                      # Optional: Skip Azure login
```

You can run the script in a loop or just once. Note that the discovered devices and assets will
take several minutes to be synchronized into Azure and that can happen in any order. Run the script
again or in loop mode to bring all resources in and back down to the cluster.

## Advanced

The debug folder contains scripts to build and debug the OPC Publisher.

### Adding more sample servers to the cluster

The `.\simulation\deploy.ps1` script allows you to deploy not just OPC PLC but also the test server
included in this repository. It also allows you to add other simulations.  For more information
check out the script help.

### Building OPC Publisher and deploying into the cluster

You can use [`.\debug\build.ps1`](./debug/build.ps1) to build a debug version.

```powershell
# Basic usage
.\debug\build.ps1

# Full syntax with all parameters
.\debug\build.ps1 `
    [-Configuration <config>] `                     # Optional: Debug or Release (default: Debug)
    [-ClusterType <type>] `                        # Optional: Target cluster type (default: microk8s)
    [-StartDebugger]                               # Optional: Start debugger after build

# Examples

# Build with default settings (Debug configuration)
.\debug\build.ps1

# Build release configuration for specific cluster
.\debug\build.ps1 `
    -Configuration "Release" `
    -ClusterType "kind"

# Build and start debugging session
.\debug\build.ps1 `
    -Configuration "Debug" `
    -StartDebugger
```

The script builds the OPC Publisher and starts the `debug.ps1` script pointing to the built container.
Debugging is described in the next section.

### Attaching the Debugger

Use [`.\debug\debug.ps1`](./debug/debug.ps1) to attach the debugger to the running OPC Publisher that
can be connected to the Visual Studio 2022 or Visual Studio Code debugger:

```powershell
# Basic usage (interactive)
.\debug\debug.ps1

# Full syntax with all parameters
.\debug\debug.ps1 `
    [-PodName <pod>] `                            # Optional: Target pod name (prompts if omitted)
    [-ContainerName <container>] `                # Optional: Target container name (prompts if omitted)
    [-Namespace <namespace>] `                    # Optional: K8s namespace (default: azure-iot-operations)
    [-ClusterType <type>] `                       # Optional: Cluster type (default: microk8s)
    [-Image <image-name>] `                       # Optional: Debug image to replace in pod
    [-Fork]                                       # Optional: Create new pod instead of modifying existing

# Examples

# Interactive debugging (prompts for pod and container)
.\debug\debug.ps1

# Debug specific pod and container
.\debug\debug.ps1 `
    -PodName "opc-publisher" `
    -ContainerName "publisher"

# Debug with custom namespace and image
.\debug\debug.ps1 `
    -PodName "opc-publisher" `
    -Namespace "iot-debug" `
    -Image "debug-publisher:latest" `
    -Fork
```

Once the debugger is attached you can open the solution file in Visual Studio 2022 and
attach to the selected container using a SSH connection to localhost:50000.