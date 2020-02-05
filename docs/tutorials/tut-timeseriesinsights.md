# Use Time Series Insights to visualize telemetry sent from OPC Publisher

[Home](readme.md)

## Prerequisites

Follow the instructions to [publish data to IoT Hub using the engineering tool](tut-publish-data.md).

## Overview

The OPC Publisher module connects to OPC UA servers and publishes data from these servers to IoT Hub.   The Telemetry processor in the Industrial IoT platform processes these events and forwards contextualized samples to TSI and other consumers.  

A Timeseries Insights Environment is automatically created as part of the [deployment](../deploy/howto-deploy-all-in-one.md) step run as prerequisite to the tutorials.

This how-to guide shows you how to visualize and analyze the OPC UA Telemetry using this Time Series Insights environment.

## Time Series Insights explorer

1. The Time Series Insights explorer is a web app you can use to visualize your telemetry.  To retrieve the url of the application open the `.env` file saved as a result of the deployment.  Open a browser to the Url in the `PCS_TSI_URL` variable.  

(TODO)

## Define and apply a new Model

(TODO)

## Connect Time Series Insights to Power BI

You can also connect connect the Time Series Insights environment to Power BI.  For more information, see [How to connect TSI to Power BI](https://docs.microsoft.com/en-us/azure/time-series-insights/how-to-connect-power-bi) and [Visualize data from TSI in Power BI](https://docs.microsoft.com/en-us/azure/time-series-insights/concepts-power-bi).

## Next Steps

- To learn more aboutthe Time Series Insights explorer, see [Azure Time Series Insights explorer](https://docs.microsoft.com/en-us/azure/time-series-insights/time-series-insights-update-explorer).
- [Visualize OPC UA Pub/Sub Telemtry in Power BI](tut-power-bi-cdm.md)
- [Deploy IoT Edge to discover and connect your own assets](../deploy/howto-install-iot-edge.md)
