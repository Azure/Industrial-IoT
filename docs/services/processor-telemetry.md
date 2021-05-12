# Edge Telemetry processor

[Home](readme.md)

## Overview

The telemetry processor processes all edge telemetry in that it

* Filters out edge events and discards them (processed by the [Edge Event processor](events.md)).
* Decodes binary PubSub (UADP) network messages
* Converts PubSub Network messages into simple messages
* Forwards these and other telemetry to a secondary Event Hub to [forward to applications](ux.md).

The edge telemetry processor is an event processor host and can be scaled out to handle the configured number of partitions.  It connects to the "telemetry" consumer group on IoT Hub.

## Docker image

`docker pull mcr.microsoft.com/iot/industrial-iot-telemetry-processor:latest`
