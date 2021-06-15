# Edge Event Processor

[Home](readme.md)

## Overview

The edge event processor processes events received from edge modules.  This includes

* Discovery progress events forwarded to SignalR
* Discovery results forwarded to [Onboarding Micro Service](onboarding.md)

The edge event processor is an event processor host and can be scaled out to handle the configured number of partitions.  It connects to a "events" consumer group on IoT Hub.

## Docker image

`docker pull mcr.microsoft.com/iot/industrial-iot-events-processor:latest`
