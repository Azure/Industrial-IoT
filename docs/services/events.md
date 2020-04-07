# SignalR Service

[Home](readme.md)

Namespace: Microsoft.Azure.IIoT.Services.OpcUa.Events

## Overview

The SignalR event service forwards ...

* Registry update events (from ServiceBus Topics)
* Discovery Progress (from ServiceBus Topics)
* telemetry samples from the secondary Telemetry EventHub

over SignalR to clients and thus provides a reactive UX experience.

The secondary Telemetry EventHub receives processed and decoded edge telemetry messages from the OPC Publisher module (Pub/Sub).  This is the same EventHub that Azure Timeseries Insights (TSI) can connect to for historian query capability.
