# SignalR Telemetry Forwarder

[Home](readme.md)

## Overview

The SignalR telemetry forwarder agent is an event processor that forwardes Telemetry event to interested client applications using SignalR.  It connects to the secondary Telemetry EventHub which receives processed and decoded edge telemetry messages, e.g. from the OPC Publisher module (Pub/Sub).

This is the same EventHub that Azure Timeseries Insights (TSI) can connect to for historian query capability.
