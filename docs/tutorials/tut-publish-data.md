# How to publish data from your Assets using the Engineering Tool

[Home](readme.md)

## Prerequisites

Follow the instructions to [discover and browse assets using the engineering tool](tut-discover-assets.md).

## List publishers

To publish data the platform makes use the deployed [OPC Publisher](../modules/publisher.md) modules.  The OPC Publisher module receives publish instructions ("jobs") and executes these long running jobs.   A publish job specifies the server and its nodes (Variables/events) that should be monitored and whose samples should be sent to Azure IoT Hub.

All Publishers deployed to your gateways are listed in under *Publisher*.

![Publisher](../media/eng-tool-publisher.png)

## Subscribe to a variables in the Simulation

(TODO)

## Next steps

- [Visualize and analyze your data in Time Series Insights](tut-timeseries-insights.md)