# Edge Telemetry processor

[Home](readme.md)

## Overview

The telemetry processor processes all edge telemetry in that it

* Filters out edge events and discards them (processed by the [Edge Event processor](events.md)).
* Decodes binary Pub/Sub (UADP) network messages
* Converts Pub/Sub Network messages into simple messages 
* Forwards these and other telemetry to a secondary Event Hub to [forward to applications](ux.md), process through TSI and/or store in [Datalake](cdm.md).

The edge telemetry processor is an event processor host and can be scaled out to handle the configured number of partitions.  It connects to the "telemetry" consumer group on IoT Hub.