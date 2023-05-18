# Observability

[Home](./readme.md)

## Table Of Contents <!-- omit in toc -->

- [Observability](#observability)
  - [Logging](#logging)
  - [Runtime diagnostics output](#runtime-diagnostics-output)
  - [Available metrics](#available-metrics)
  - [Metrics collector](#metrics-collector)
  - [Metrics Dashboard](#metrics-dashboard)
    - [Setup Steps](#setup-steps)
      - [Create docker images](#create-docker-images)
      - [Create IoT Edge modules](#create-iot-edge-modules)
    - [View Prometheus dashboard](#view-prometheus-dashboard)
    - [View Grafana dashboard](#view-grafana-dashboard)
  - [Distributed Tracing](#distributed-tracing)

## Logging

Logging can be configured using the `--ll` [command line argument](./commandline.md). It allows to set a desired log level. More fine grained logging can be configured using an appsettings.json file as per .net documentation.

It is also possible to enable more detailed logging of the OPC UA stack using the `--sl` option.

To get the logs the `iotedge` command line utility can be used. Run the following from the local command line:

```bash
iotedge logs -f <container name>
```

These logs contain the current state of operation and can be used to narrow down an issue.

Additionally, logs from edge modules can be fetched [via direct methods](https://github.com/Azure/iotedge/blob/master/doc/built-in-logs-pull.md) describes how to fetch and upload logs from Edge modules.

## Runtime diagnostics output

The OPC Publisher emits metrics through its Prometheus endpoint (`/metrics`). To learn more about how to create a local metrics dashboard for OPC Publisher V2.7, refer to the tutorial [here](./observability.md).

To measure the performance of OPC Publisher, the `di` parameter can be used to print metrics to the log in the interval specified (in seconds).

The following table describes the actual instruments that are logged per endpoint:

| Log line item name                      | Diagnostic info property name                       | Description |
|-----------------------------------------|-------------------------------------|-------------|
| # Ingestion duration                    | ingestionDuration                   | How long the data flow inside the publisher has been executing after it was created (either from file or API) |
| # Ingress DataChanges (from OPC)        | ingressDataChanges                  | The number of OPC UA subscription notification messages with data value changes that have been received by publisher inside this data flow |
| # Ingress ValueChanges (from OPC)       | ingressValueChanges                 | The number of value changes inside the OPC UA subscription notifications processed by the data flow. |
| # Ingress EventNotifications (from OPC) | ingressEventNotifications           | The number of OPC UA subscription notification messages with events that have been received by publisher so far inside this data flow |
| # Ingress EventChanges (from OPC)       | ingressEvents                       | The number of events that were part of these OPC UA subscription notifications that were so far processed by the data flow. |
| # Ingress BatchBlock buffer size        | ingressBatchBlockBufferSize         | The number of messages awaiting encoding and sending tot he telemetry message destination inside the data flow pipeline. |
| # Encoding Block input / output size    | encodingBlockInputSize              | The number of messages awaiting encoding into the output format. |
| # Encoding Block input / output size    | encodingBlockOutputSize             | The number of messages already encoded and waiting to be sent to the telemetry message destination. |
| # Encoder Notifications processed       | encoderNotificationsProcessed       | The total number of subscription notifications processed by the encoder stage of the data flow pipeline since the pipeline started. |
| # Encoder Notifications dropped         | encoderNotificationsDropped         | The total number of subscription notifications that were dropped because they could not be encoded, e.g., due to their size being to large to fit into the message. |
| # Encoder IoT Messages processed        | encoderIoTMessagesProcessed         | The total number of encoded messages produced by the encoder since the start of the pipeline. |
| # Encoder avg Notifications/Message     | encoderAvgNotificationsMessage      | The average number of subscription notifications that were presssed into a message. |
| # Encoder avg IoT Message body size     | encoderAvgIoTMessageBodySize        | The average size of the message body produced over the course of the pipeline run. |
| # Encoder avg IoT Chunk (4 Kb) usage    | encoderAvgIoTChunkUsage             | The average use of IoT Hub chunks (4k). |
| # Estimated IoT Chunks (4 KB) per day   | estimatedIoTChunksPerDay            | An estimate of how many chunks are used per day by publisher which enables correct sizing of the IoT Hub to avoid data loss due to throttling. |
| # Outgress Batch Block buffer size      | outgressBatchBlockBufferSize        | The number of messages that are waiting to be sent to all configured telemetry message destination via the message sink. |
| # Outgress input bufffer count          | outgressInputBufferCount            | The aggregated number of messages waiting in the input buffer of the configured telemetry message destination sinks. |
| # Outgress input buffer dropped         | outgressInputBufferDropped          | The aggregated number of messages that were dropped in any of the configured telemetry message destination sinks. |
| # Outgress IoT message count            | outgressIoTMessageCount             | The aggregated number of messages that were sent by all configured telemetry message destination sinks. |
|                                         | sentMessagesPerSec                  | Publisher throughput meaning the number of messages sent to the telemetry message destination (e.g., IoT Hub / Edge Hub) per second |
| # Connection retries                    | connectionRetries                   | How many times connections to the OPC UA server broke and needed to be reconnected as it pertains to the data flow. |
| # Opc endpoint connected?               | opcEndpointConnected                | Whether the pipeline is currently connected to the OPC UA server endpoint or in a reconnect attempt. |
| # Montitored Opc nodes succeeded count  | monitoredOpcNodesSucceededCount     | How many of the configured monitored items have been established successfully inside the data flow's OPC UA subscription and should be producing data. |
| # Montitored Opc nodes failed count     | monitoredOpcNodesFailedCount        | How many of the configured monitored items inside the data flow failed to be created in the subscription (the logs will provide more information). |

## Available metrics

By convention, all instrument names start with _"iiot"_, e.g., `iiot_<component>_<metric>`. Metrics are collected through the .net Observability infrastructure. For backwards compatibility and to support IoT Edge metrics collector, metrics from OPC Publisher are exposed in Prometheus format on path _/metrics_ on the default HTTP server port (see `-p` [command line argument](./commandline.md). When binding the HTTPS port to 9072 on the host machine, the URL becomes <https://localhost:9072/metrics>.

![metrics](./media/metrics.jpeg)

One can combine information from multiple metrics to understand and paint a bigger picture of the state of the publisher. The following table describes the individual default metrics which are in Prometheus format.

| Instrument Name | Dimensions | Description | Type |
|-----------------|------------|-------------|------|
| iiot_edge_module_start                                  | module, version, deviceid, timestamp_utc, runid | Edge modules start timestamp with version. Used to calculate uptime, device restarts.   | gauge     |
| iiot_edge_publisher_monitored_items                     | subscription                                     | Total monitored items per subscription                                                  | gauge     |
| iiot_edge_device_reconnected                             | device, timestamp_utc                           | OPC UA server reconnection count                                                        | gauge     |
| iiot_edge_device_disconnected                            | device, timestamp_utc                           | OPC UA server disconnection count                                                       | gauge     |
| iiot_edge_reconnected                                     | module, device, timestamp_utc                   | Edge device reconnection count                                                          | gauge     |
| iiot_edge_disconnected                                    | module, device, timestamp_utc                   | Edge device disconnection count                                                         | gauge     |
| iiot_edge_publisher_messages_duration                   | le (less than equals)                            | Time taken to send messages from publisher. Used to calculate P50, P90, P95, P99, P99.9 | histogram |
| iiot_edge_publisher_value_changes                       | deviceid, module, triggerid                      | OPC UA server ValuesChanges delivered for processing                                    | gauge     |
| iiot_edge_publisher_value_changes_per_second          | deviceid, module, triggerid                      | OPC UA server ValuesChanges delivered for processing per second                         | gauge     |
| iiot_edge_publisher_data_changes                        | deviceid, module, triggerid                      | OPC UA server DataChanges delivered for processing                                      | gauge     |
| iiot_edge_publisher_data_changes_per_second           | deviceid, module, triggerid                      | OPC UA server DataChanges delivered for processing                                      | gauge     |
| iiot_edge_publisher_iothub_queue_size                  | deviceid, module, triggerid                      | Queued messages to IoTHub                                                               | gauge     |
| iiot_edge_publisher_iothub_queue_dropped_count        | deviceid, module, triggerid                      | IoTHub dropped messages count                                                           | gauge     |
| iiot_edge_publisher_sent_iot_messages                  | deviceid, module, triggerid                      | Messages sent to IoTHub                                                                 | gauge     |
| iiot_edge_publisher_sent_iot_messages_per_second     | deviceid, module, triggerid                      | Messages sent to IoTHub per second                                                      | gauge     |
| iiot_edge_publisher_connection_retries                  | deviceid, module, triggerid                      | Connection retries to OPC UA server                                                     | gauge     |
| iiot_edge_publisher_encoded_notifications               | deviceid, module, triggerid                      | Encoded OPC UA server notifications count                                               | gauge     |
| iiot_edge_publisher_dropped_notifications               | deviceid, module, triggerid                      | Dropped OPC UA server notifications count                                               | gauge     |
| iiot_edge_publisher_processed_messages                  | deviceid, module, triggerid                      | Processed IoT messages count                                                            | gauge     |
| iiot_edge_publisher_notifications_per_message_average | deviceid, module, triggerid                      | OPC UA sever notifications per IoT message average                                      | gauge     |
| iiot_edge_publisher_encoded_message_size_average      | deviceid, module, triggerid                      | Encoded IoT message body size average                                                   | gauge     |
| iiot_edge_publisher_chunk_size_average                 | deviceid, module, triggerid                      | IoT Hub chunk size average                                                              | gauge     |
| iiot_edge_publisher_estimated_message_chunks_per_day | deviceid, module, triggerid                      | Estimated IoT Hub chunks charged per day                                                | gauge     |
| iiot_edge_publisher_is_connection_ok                 | deviceid, module, triggerid                      | Is the endpoint connection ok?                                                          | gauge     |
| iiot_edge_publisher_good_nodes                       | deviceid, module, triggerid                      | How many nodes are receiving data for this endpoint?                                    | gauge     |
| iiot_edge_publisher_bad_nodes                        | deviceid, module, triggerid                      | How many nodes are misconfigured for this endpoint?                                     | gauge     |

You can use the IoT Edge [Metrics Collector](#metrics-collector) module to collect the metrics.

Edge modules would be instrumented with [Prometheus](https://github.com/prometheus-net/prometheus-net) metrics. Each module would expose the metrics on a pre-defined port.  The `metricscollector` module would use the configuration settings to scrape metrics in a defined interval. It would then push the scraped metrics to Log Analytics Workspace. Using Kusto, we could then query our metrics from workspace. We are creating and deploying an Azure Workbook which would provide insights into the edge modules. This would act as our primary monitoring system for edge modules.

## Metrics collector

The metrics collector module pulls metrics exposed by OPC Publisher and pushes them to the Log Analytics workspace.

The default configuration to enable scraping metrics is:

```json
{ 
    "schemaVersion": "1.0",
    "scrapeFrequencySecs": 120,
    "metricsFormat": "Json",
    "syncTarget": "AzureLogAnalytics",
    "endpoints": {
        "opcpublisher": "http://opcpublisher/metrics"
    }
}
```

> This assumes the http server in OPC Pubilsher has not been disabled. Disabling the internal HTTP server also disables the Prometheus endpoint.

## Metrics Dashboard

**Please note: This feature is only available in OPC Publisher's version 2.7 and above.**

OPC Publisher is instrumented with Prometheus metrics. For troubleshooting and debugging, a local dashboard on Azure IoT Edge can be configured.
This tutorial guides through the complete setup of viewing a Grafana dashboard displaying OPC Publisher and EdgeHub metrics for bird's eye view to quickly drill down into the issues in case of failures.
In a nutshell, two Docker images (Prometheus and Grafana) must be created and deployed as Azure IoT Edge Modules. The Prometheus Module scrapes the metrics from OPC Publisher and EdgeHub, while Grafana is used for visualization of metrics.

### Setup Steps

#### Create docker images

1. Create and configure [Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal) to have an admin account enabled.  **Registry name** and **password**  are needed for pushing the docker images of Prometheus and Grafana.

    ![01](./media/01.JPG)

2. Create and push images to this ACR:

    - Download the zip (_metricsdashboard.zip_) located in this folder and extract it.

    - Navigate to **prometheus** folder as shown:

      ![02](./media/02.JPG)

    - Edit _prometheus.yml_ if needed. It defaults to scraping EdgeHub metrics at 9600 and OPC Publisher metrics at 9702. If only OPC Publisher metrics are needed, then remove the scraping config of EdgeHub.

    - Run _buildimage.bat_ and enter the _registryname_, _password_ and _tagname_ when prompted. It will push the **edgeprometheus** image to container registry.

      ![03](./media/03.JPG)

    - Navigate back to the **grafana** folder and run _buildimage.bat_ located in this folder. Enter the same information to push the **edgegrafana** image to ACR.

3. Now go to the portal and verify that the images are present, as shown in the image below.

   ![04](./media/04.JPG)

#### Create IoT Edge modules

1. IoT Edge should now be configured to expose metrics. Navigate to the IoT Edge device to be used and perform the following steps:

    - Select **Set Modules** to update the configuration

    - Enter the registry information in the **Container Registry Credentials** so that it can authenticate with the Azure Container Registry.

     ![05](./media/05.JPG)

    - Optional: Enable metrics of _edgeHub_ (Note: As of release 1.0.10, metrics are automatically exposed by default on http port **9600** of the EdgeHub)

      - Select the **Runtime Settings** option and for the EdgeHub and Agent, make sure that the
        stable version is selected with version 1.0.9.4+

        ![06](./media/06.JPG)

      - Adjust the EdgeHub **Create Options** to include the Exposed Ports directive as shown above.

      - Add the following environment variables to the EdgeHub. (Not needed for version 1.0.10+)

        **ExperimentalFeatures__Enabled         : true**

        **ExperimentalFeaturesEnable__Metrics   : true**

      - Save the dialog to complete the runtime adjustments.

    - Expose port on publisher module:

      - Navigate to the publisher and expose port **9702** to allow the metrics to be scraped, by adding the exposed ports option in the Container Create Options in the Azure portal.

        ![07](./media/07.JPG)

      - Select **Update** to complete the changes made to the OPC publisher module.

2. Now add new modules based on the Prometheus and Grafana images created previously.

    - Prometheus:

      - Add a new “IoT Edge Module” from the “+ Add” drop down list.

      - Add a module named **prometheus** with the image uploaded previously to the Azure Container Registry.

        ![08](./media/08.JPG)

      - In the **Container Create Options** expose and bind the Prometheus port 9090.

        ```json
        {
            "Hostname": "prometheus",
            "ExposedPorts": {
                "9090/tcp": {}
            },
            "HostConfig": {
                "PortBindings": {
                    "9090/tcp": [
                        {
                            "HostPort": 9090
                        }
                    ]
                }
            }
        }
        ```

      - Click **Update** to complete the changes to the Prometheus module.

      - Select **Review and Create** and complete the deployment by choosing **Create**.

    - Grafana:

      - Add a new “IoT Edge Module” from the “+ Add” drop down list.

      - Add a module named **grafana** with the image uploaded previously to the Azure Container Registry.

        ![09](./media/09.JPG)

      - Update the environment variables to control access to the Grafana dashboard.

        - GF_SECURITY_ADMIN_USER - admin

        - GF_SECURITY_ADMIN_PASSWORD - iiotgrafana

        - GF_SERVER_ROOT_URL  - <http://localhost:3000>

          ![10](./media/10.JPG)

      - In the **Container Create Options** expose and bind the Grafana port 3000.

        ```json
        {
            "Hostname": "grafana",
            "ExposedPorts": {
                "3000/tcp": {}
            },
            "HostConfig": {
                "PortBindings": {
                    "3000/tcp": [
                        {
                            "HostPort": 3000
                        }
                    ]
                }
              }
         }
        ```

      - Click **Update** to complete the changes to the Grafana module.

      - Select **Review and Create** and complete the deployment by choosing **Create**.

    - Verify all the modules are running successfully:

      ![15](./media/15.JPG)

### View Prometheus dashboard

- Navigate to the Prometheus dashboard through a web browser on the IoT Edge’s host on port 9090.

  - <http://{edge> host IP or name}:9090/graph
  - **Note**: When using a VM, make sure to add an inbound rule for port 9090

- Check the target state by navigating to **/targets**

  ![12](./media/12.JPG)

- Metrics can be viewed as shown:

  ![11](./media/11.JPG)

- ![13](./media/13.JPG)

### View Grafana dashboard

- Navigate to the Grafana dashboard through a web browser against the edge’s host on port 3000.
  - <http://{edge> host IP or name}:3000
  - When prompted for a user name and password enter the values entered in the environment variables
  - **Note**: When using a VM, make sure to add an inbound rule for port 3000
  
- Prometheus has already been configured as a data source and can now be directly accessed. Prometheus is scraping EdgeHub and OPC Publisher metrics.

- Select the dashboards option to view the available dashboards and select “Publisher” to view the pre-configured dashboard as shown below.

  ![14](./media/14.JPG)

- One could easily add/remove more panels to this dashboard as per the needs.

## Distributed Tracing

Distributed tracing will be supported in a future release.
