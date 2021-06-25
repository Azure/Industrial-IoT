# E2E test

## Background
Poperties of E2E tests:
* black-box
* end-to-end
* high cost tests (long preparation / execution times)

Goal of E2E tests:
* check the customer point of view (validate)
* cover the most important scenarios

## How to write E2E tests
### Preparation
Use the `TestCaseOrderer` attribute with `TestCaseOrderer.FullName` parameter on
your test class, and `PriorityOrder` on your test methods to order them.

Use the `Collection` attribute on your test class to share the context between 
test methods.

Use the `Trait` attribute on your test class to distinguish between orchestrated and standalone mode:
* for orchestrated mode use the `Trait` with the name `TestConstants.TraitConstants.PublisherModeTraitName` and the value `TestConstants.TraitConstants.PublisherModeOrchestratedTraitValue`,
* for standalone mode use the `Trait` with the name `TestConstants.TraitConstants.PublisherModeTraitName` and the value `TestConstants.TraitConstants.PublisherModeStandaloneTraitValue`.

The test class gets the context as a parameter of its constructor.
Use one of the following context types:
* `IIoTPlatformTestContext` - for orchestrated mode
* `IIoTStandaloneTestContext` - for standalone mode

In order for the context to log information the `OutputHelper` of the context needs to be set to the `IOutputHelper` the test class gets as a constructor parameter.

If necessary you can clean the context by calling its `Reset` method, otherwise it's state is shared between test methods, and even between test classes of the same `Collection`. It is recommended to call `Reset` on the context in the first test method of a test class.

All test collections should start by setting the desired mode as a first step. E.g.<br>
`await TestHelper.SwitchToOrchestratedModeAsync(_context);`

Use and extend the `TestHelper` class.

`TestEventProcessor` listens to IoT Hub and analyzes the value changes.

### Executing tests in Visual Studio

You can reuse a test deployment to speed up test development.
Follow these steps:
* Start new E2E pipeline build with `Cleanup` variable set to `false`.
* Find the `ResourceGroupName` in the pipeline logs.
* Navigate to the given resource group on the Azure portal.
* Change tags:
  * add or edit tag named `owner` where the value identifies you,
  * add the tag `DoNotDelete` with the value `true`.
* Add yourself to the KeyVault:
  * open "Access policies" on the azure portal,
  * click "Add Access Policy",
  * at "Configure from template" select "Key & Secret Management",
  * at "Select principal" select your principal,
  * press the "Add" button,
  * press the "Save" button.
* Execute /tools/e2etesting/GetSecrets.ps1 -KeyVaultName &lt;YourKeyVaultName&gt;. You will be asked whether you want to overwrite the settings file or not.
  * if you choose 'yes' just wait for the script to finish and no further steps are needed
  * if you choose 'no' copy the script output to IIoTPlatform-E2E-Tests\Properties\launchSettings.json under environmentVariables
* If you don't want to use the default subscription, add SUBSCRIPTION_ID: "<sub id" to launchSettings.json
* Now you can use Visual Studio to execute your tests.
* Don't forget to clean up by executing the pipeline with these variables set:
  * `Cleanup = false`
  * `UseExisting = true`
  * `ResourceGroupName = <your_resource_group_name>`

### Monitoring Edge modules with Prometheus

While running tests, it can be useful to monitor modules. A quick way to do this is to run a Prometheus Server locally on the Edge VM. Note that this configuration has no persistence and is only meant for experimentation.

- Download the SSH private key in the depoyment Key Vault.
- Open an SSH connection to the Edge VM, also tunneling the port used by the Prometheus Server. From  Bash:

```bash
chmod og-w edge_private_key

ssh -L 9090:localhost:9090 -i edge_private_key sandboxuser@e2etesting-edgevm-XXXXX.westeurope.cloudapp.azure.com
```

- On the Edge VM, start Prometheus Node Exporter:

```bash
sudo docker run -d --net azure-iot-edge -p 9100:9100 --name exporter prom/node-exporter
```

- On the Edge VM, create a file `/tmp/prometheus.yml`:

```yaml
global:
  scrape_interval:     15s # Set the scrape interval to every 15 seconds. Default is every 1 minute.
  evaluation_interval: 15s # Evaluate rules every 15 seconds. The default is every 1 minute.
  # scrape_timeout is set to the global default (10s).

  # Attach these labels to any time series or alerts when communicating with
  # external systems (federation, remote storage, Alertmanager).
  external_labels:
      monitor: 'edge'

scrape_configs:
  # The job name is added as a label `job=<job_name>` to any timeseries scraped from this config.
  - job_name: 'prometheus'

    # metrics_path defaults to '/metrics'
    # scheme defaults to 'http'.

    static_configs:
      - targets: ['localhost:9090']

  - job_name: 'edge-modules'

    static_configs:
      - targets: ['edgeHub:9600', 'edgeAgent:9600', 'publisher_standalone:9702', 'exporter:9100']
```

- On the Edge VM, start Prometheus Server:

```bash
sudo docker run -d --net azure-iot-edge -p 9090:9090 -v /tmp/prometheus.yml:/etc/prometheus/prometheus.yml   prom/prometheus
```

- On your local machine, connect to `http://localhost:9090/` (using the SSH tunnel) to browse metrics. Example queries:
  - [IoT Hub messages per minute](http://localhost:9090/graph?g0.expr=sum%28rate%28iiot_edge_publisher_messages%5B1m%5D%29%29*60&g0.tab=0&g0.stacked=0&g0.range_input=1h
    )
  - [CPU usage](http://localhost:9090/graph?g0.expr=100%20-%20%28avg%20by%20%28instance%29%20%28irate%28node_cpu_seconds_total%7Bmode%3D%22idle%22%7D%5B1m%5D%29%29%20*%20100%29%20&g0.tab=0)

### Create pipeline for forked Publisher

For developing the Publisher on a forked repository, you may want to set up Azure Pipelines in your own tenant.

* Create an Azure Container Registry (ACR) instance with public access.

* Import the Edge module images from MCR into your ACR:

```bash
az acr import --name MYACR --force --source mcr.microsoft.com/azureiotedge-agent:1.1 --image events-and-alarms/azureiotedge-agent:1.1
az acr import --name MYACR --force --source mcr.microsoft.com/azureiotedge-hub:1.1 --image events-and-alarms/azureiotedge-hub:1.1
```

* Build your Publisher:

```bash
pwsh tools/scripts/acr-build.ps1 -Path "modules/src/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher/src" -Registry "MYACR"
```

* Create an ARM service connection with Subscription-level access.

* Update `tools/e2etesting/pipeline_standalone.yml`:
  * Change  `name: '$(AgentPool)'` to `vmImage: windows-latest`

* Import `tools/e2etesting/pipeline_standalone.yml` as a pipeline. Create the following pipeline variables:

| Name                      | Default value                               |
| ------------------------- | ------------------------------------------- |
| `AzureSubscription`       | Your service connection name                |
| `Cleanup`                 | `true`                                      |
| `ContainerRegistryServer` | `YOURACR.azurecr.io/events-and-alarms`      |
| `EdgeVersion`             | `1.1`                                       |
| `PLCImage`                | `YOURACR.azurecr.io/iotedge/opc-plc:latest` |
| `PLCUsername`             | Your ACR username*                          |
| `PLCPassword`             | Your ACR password*                          |
| `Region`                  | `westus`                                    |
| `ResourceGroupName`       | Your resource group name                    |
| `UseExisting`             | `false`                                     |

\* If using a repository with public access enabled, fill a dummy value for `PLCUsername`  and  `PLCPassword`.

* Run the pipeline.
