# Migrating OPC Publisher from 2.9 to 3.0 <!-- omit in toc -->

[Home](./readme.md)

## Table Of Contents <!-- omit in toc -->

- [Overview](#overview)
- [What breaks and what to do about it](#what-breaks-and-what-to-do-about-it)
  - [Samples and FullSamples messaging modes removed](#samples-and-fullsamples-messaging-modes-removed)
  - [The plaintext HTTP listener is now genuinely off by default](#the-plaintext-http-listener-is-now-genuinely-off-by-default)
- [What changed on the wire](#what-changed-on-the-wire)
  - [Default messaging mode](#default-messaging-mode)
  - [Message format in PubSub mode](#message-format-in-pubsub-mode)
  - [Heartbeat indicator removed from data set messages](#heartbeat-indicator-removed-from-data-set-messages)
  - [Message batching behaviour](#message-batching-behaviour)
- [What was removed and why](#what-was-removed-and-why)
  - [Samples and FullSamples messaging modes](#samples-and-fullsamples-messaging-modes)
  - [Heartbeat indicator in JSON data set messages](#heartbeat-indicator-in-json-data-set-messages)
  - [Avro and Avro+Gzip encoding with schema publishing](#avro-and-avrogzip-encoding-with-schema-publishing)
  - [Automatic topic routing using OPC UA browse paths](#automatic-topic-routing-using-opc-ua-browse-paths)
  - [Batch size and writer group partitions](#batch-size-and-writer-group-partitions)
- [What to check after upgrading](#what-to-check-after-upgrading)
- [Rolling back to 2.9](#rolling-back-to-29)

## Overview

OPC Publisher 3.0 publishes telemetry through the native OPC UA PubSub runtime that ships with the UA-.NETStandard 2.0 stack, replacing the custom encoder used in 2.x. This is a standards-compliance upgrade: the runtime produces messages defined in OPC UA Part 14, over any configured transport, without application-layer workarounds.

The practical consequence for operations is:

- **One hard failure**: if a deployment specifies `Samples` or `FullSamples` as the messaging mode, 3.0 refuses to start and names the replacement. Everything else that was dropped is accepted and silently ignored, so existing command lines and `published_nodes.json` files that do not name those modes still start without modification.
- **Wire format changes**: downstream consumers that parse OPC Publisher telemetry will need to be checked before or immediately after the upgrade.

## What breaks and what to do about it

### Samples and FullSamples messaging modes removed

**This is one of two changes that prevent startup** — the other is [Avro encoding](#avro-and-avrogzip-encoding-with-schema-publishing). Everything else listed in this guide is accepted and ignored so an existing configuration still loads.

OPC Publisher 2.x defaulted to `--mm=Samples`. The `Samples` and `FullSamples` modes emitted a proprietary `MonitoredItemMessage` format that predates OPC UA PubSub and has no representation in Part 14. The native PubSub runtime cannot produce it.

If your deployment sets `--mm=Samples` or `--mm=FullSamples` on the command line, in the module twin, or in a `published_nodes.json` `MessagingMode` field, 3.0 will refuse to start with an error naming the replacement.

| Removed mode | Error message names | Use instead |
| --- | --- | --- |
| `Samples` | `PubSub` | `--mm=PubSub` |
| `FullSamples` | `FullNetworkMessages` | `--mm=FullNetworkMessages` |

**Action required:**

1. Search your command line, environment variables, module twin, and `published_nodes.json` for any occurrence of `Samples` or `FullSamples` as a messaging mode value.
2. Replace `Samples` with `PubSub` and `FullSamples` with `FullNetworkMessages`.
3. Update any downstream consumer that parses the old `MonitoredItemMessage` wire format — see [What changed on the wire](#what-changed-on-the-wire) below.

If no messaging mode was specified the deployment already uses the 2.9 default, which was `PubSub`. No change is needed for the mode itself, but verify the wire format section below.

### The plaintext HTTP listener is now genuinely off by default

This one does not stop startup — it stops **clients** that talked to OPC
Publisher over plain HTTP.

`--unsecurehttp` has always described itself as `Default: disabled`, and warned
in the same sentence that the listener exposes the api key on the network. It
was never actually off. `UnsecureHttpServerPort` resolved to its default port
whenever the setting was absent, empty or unparseable — which is every
deployment that did not set it — so the documented "disabled" state could not be
reached by any configuration input, and OPC Publisher listened on **9071** (or
**80** when running as root in a container) on all interfaces regardless.

3.0 makes the behaviour match the documentation.

**Action required** if anything talks to OPC Publisher over `http://`:

1. Prefer moving the client to the TLS port (**9072**, or **443** as root in a
   container). The api key is sent in clear text on the plaintext listener, so
   this is the reason the option warned against it in the first place.
2. If you cannot, pass `--unsecurehttp` to restore the previous behaviour on the
   default port, or `--unsecurehttp=<port>` to pick one.
3. Container deployments that published the plaintext port (for example
   `-p 9071:80`, or a `PortBindings` entry in IoT Edge `createOptions`) need the
   option added as well — publishing a port the process no longer listens on
   silently yields connection refused.

The symptom of missing this is a refused connection, not a silent fallback.

The MCP tool endpoint (`--mcp`) is refused on the plaintext listener even when
you enable it, because those tools can extract secure channel keys. See
[MCP](./mcp.md#the-plaintext-listener).

## What changed on the wire

### Default messaging mode

| Version | Default `--mm` |
| --- | --- |
| 2.x (up to 2.8) | `Samples` |
| 2.9 | `PubSub` |
| 3.0 | `PubSub` |

The default did not change between 2.9 and 3.0. If you have been running 2.9 with the default, or have been explicitly setting `--mm=PubSub`, the message structure your consumers receive is the same as before.

### Message format in PubSub mode

Messages are now produced by the native OPC UA PubSub runtime rather than the custom encoder. The envelope and field names match what `--mm=PubSub` produced in 2.9, with the following differences you should verify:

- `DataSetWriterId` in **strict mode** (`-c` / `--strict`) is a unique integer within the writer group, not the writer name string. Outside strict mode it remains the writer name string for backward compatibility.
- Writer group and dataset writer identities are derived from the resolved configuration. If you are migrating from `Samples` or `FullSamples` mode, changing the messaging mode changes these identities. Consumers that key on the generated identities, and retained metadata topics, are re-created after the migration.

For the full current wire format including field masks, see [Telemetry Message Formats](./messageformats.md).

### Heartbeat indicator removed from data set messages

Up to 2.9, OPC Publisher could write an extra `Heartbeat: true` member into a data set message when the message was produced by a heartbeat rather than a fresh value change. That member is not part of OPC UA Part 14. 3.0 publishes through the standards-compliant PubSub encoder, which has no notion of it.

The `Heartbeat` flag (`0x400000`) in `DataSetFieldContentMask` is still accepted so existing configurations keep loading, but it no longer changes anything on the wire.

**Detecting a heartbeat in 3.0:**

Because the re-sent value is identical to the previously reported one (including its `SourceTimestamp` and `ServerTimestamp`), a heartbeat and a real value change already looked the same in 2.9 to consumers that did not inspect the indicator.

To tell them apart, compare the `SourceTimestamp` of consecutive values for a field: a heartbeat repeats the previous timestamp exactly, whereas a genuine change carries a new one.

> **Caveat:** this comparison is only reliable when the heartbeat behaviour is left at its default. `HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps` deliberately advances the timestamps, making heartbeats indistinguishable from value changes by design. See the [Heartbeat messages](./messageformats.md#heartbeat-messages) section of the message formats reference for full details.

### Message batching behaviour

The native PubSub runtime emits one message per sample and does not coalesce multiple samples into a single message. The `--bs` (`BatchSize`) option is accepted and ignored (see [below](#batch-size-and-writer-group-partitions)). Use `--bi` (`BatchTriggerInterval`) to control how frequently the runtime samples, and `--om` (`MaxNetworkMessageSendQueueSize`) to bound the send queue.

## What was removed and why

All removals except the messaging modes follow the same rule: **the option is accepted and ignored**, so an existing command line or `published_nodes.json` still starts. Only `Samples` and `FullSamples` fail loudly, because silently changing the wire format of a running deployment is worse than refusing to start.

### Samples and FullSamples messaging modes

The `MonitoredItemMessage` format these modes produced pre-dates OPC UA PubSub and has no representation in Part 14. The native PubSub runtime cannot produce it, and no standards-compliant receiver understands it. Rather than silently reshape a deployment's messages, 3.0 refuses to start with a clear error.

See [Samples and FullSamples messaging modes removed](#samples-and-fullsamples-messaging-modes-removed) above for migration steps. The removed format is documented in [Samples mode encoding (Removed in 3.0)](./messageformats.md#samples-mode-encoding-removed-in-30) for reference while adapting existing consumers.

### Heartbeat indicator in JSON data set messages

The `Heartbeat` member was an extension beyond Part 14 that the custom encoder injected into data set messages. The standards-compliant encoder has no facility for non-standard fields. See [Heartbeat indicator removed from data set messages](#heartbeat-indicator-removed-from-data-set-messages) above for the replacement approach.

### Avro and Avro+Gzip encoding with schema publishing

The Avro encoder and its schema infrastructure were removed as part of the migration to the UA-.NETStandard 2.0 stack.

> **This one stops the module from starting.** Unlike the other removals below, `--me=Avro` and `--me=AvroGzip` are **not** accepted and ignored. There is no Avro messaging profile, so the configuration is rejected at startup with a `ConfigurationErrorsException` that lists the combinations of `--mm` and `--me` that are supported. A deployment that passes an Avro encoding will fail to come up until the option is changed.

If you need compact binary encoding, use UADP (`--me=Uadp`). If you need JSON with a published schema, use JSON encoding with `--ps=true`.

### Automatic topic routing using OPC UA browse paths

The `--uns` / `DataSetRouting` option appended a monitored item's OPC UA browse path to the MQTT topic for each notification. That path is discovered from the server at runtime rather than configured in advance, so the native PubSub runtime — which configures its topic once and publishes all messages to it — cannot express it.

`--uns` and the `DataSetRouting` field in `published_nodes.json` are accepted and ignored. Messages are published to the configured topic without a browse path suffix.

To build a topic hierarchy, use the `--ttt` (telemetry topic template) option or the per-writer-group topic template field. The template placeholders `{WriterGroup}`, `{DataSetWriter}`, and `{DataSetName}` give you structured paths without requiring runtime browse calls. See [Transports](./transports.md) for template syntax.

### Batch size and writer group partitions

| Option | Long name | Status in 3.0 |
| --- | --- | --- |
| `--bs` | `BatchSize` | Accepted and ignored |
| `--wgp` | `DefaultWriterGroupPartitionCount` | Accepted and ignored |
| `--rdb` | `RemoveDuplicatesFromBatch` | Accepted and ignored |
| `--unp` | `UseNativePubSub` | Accepted and ignored |

The native PubSub runtime publishes a writer group through a single transport connection and emits one message per sample, so there is nothing to partition or coalesce.

- `--bi` (`BatchTriggerInterval`) still controls how frequently the runtime samples and emits. Default: 10 000 ms (10 seconds).
- `--om` (`MaxNetworkMessageSendQueueSize`) still bounds the send queue and controls when backpressure is applied to subscriptions. Default: 4096 messages.

## What to check after upgrading

1. **Search for `Samples` and `FullSamples`** in command lines, module twin properties, environment variables, and `published_nodes.json` files. Change them before starting 3.0 — the publisher will not start with either value set.

2. **Audit downstream consumers** for reliance on:
   - The `MonitoredItemMessage` format (flat top-level `NodeId`, `Value`, `Timestamp` structure with a `$$ContentType: application/x-monitored-item-json-v1` property). Consumers must be updated to accept OPC UA PubSub data set messages instead.
   - The `Heartbeat: true` field on data set messages. Replace any code that checks this with a `SourceTimestamp` comparison between consecutive messages for the same field.

3. **Change any Avro encoding before you upgrade.** `--me=Avro` and `--me=AvroGzip` are rejected at startup, so a module configured that way will not come up. See [Avro and Avro+Gzip encoding with schema publishing](#avro-and-avrogzip-encoding-with-schema-publishing) for alternatives.

4. **Check topic routing.** If you relied on `DataSetRouting` to append browse paths to MQTT topics, the topics are now the configured template without a path suffix. Update subscriptions or routing rules on the broker side.

5. **Review retained MQTT metadata.** If the messaging mode changed (e.g. from `Samples` to `PubSub`) writer group and writer identities change too. Retained metadata messages under the old identities will remain on the broker until they are overwritten or cleared.

6. **Check for clients using plain HTTP.** Anything calling the REST api over `http://` — a script, a dashboard, a health probe, or a container port mapping such as `-p 9071:80` — now gets connection refused unless `--unsecurehttp` is passed. Prefer moving those clients to the TLS port. See [The plaintext HTTP listener is now genuinely off by default](#the-plaintext-http-listener-is-now-genuinely-off-by-default).

7. **Nothing else to do for most deployments.** `published_nodes.json` files that do not name a removed messaging mode, and command lines that do not use the options listed above, start unchanged.

## Rolling back to 2.9

Rolling back is redeploying the previous module image. The configuration you
changed to satisfy 3.0 does not have to be changed back, but two things do not
return to their pre-upgrade behaviour on their own.

**1. Options that 3.0 ignored become active again.**

This is the one that surprises people, and it is the direct inverse of the rule
that made the upgrade easy. Everything in [What was removed and
why](#what-was-removed-and-why) except the messaging modes is *accepted and
ignored* by 3.0 — the settings are still sitting in your command line and
`published_nodes.json`, doing nothing. 2.9 honours them again the moment it
starts.

| Setting still in your config | Inert under 3.0 | Active again under 2.9 |
| --- | --- | --- |
| `--bs` / `BatchSize` | one message per sample | batches of that many samples per message |
| `--uns` / `DataSetRouting` | topic as configured | browse path appended to the topic |
| writer group partitions | ignored | writer group split across partitions |

So a consumer that was fine with 3.0's output can start receiving batched
messages, or messages on browse-path-suffixed topics, purely because of a
rollback. If you tuned any of these for 2.x and no longer want them, remove
them from the configuration rather than relying on 3.0 having ignored them.

**2. The wire format reverts.**

Everything in [What changed on the wire](#what-changed-on-the-wire) applies in
reverse. If you updated a downstream consumer to read OPC UA PubSub data set
messages, it has to keep handling the 2.9 format too, or be rolled back in step
with the publisher. Plan for a consumer that tolerates both across the window in
which either version might be running.

Also reverting: the `Heartbeat` indicator reappears on data set messages, and
retained MQTT metadata is republished under whatever writer group and writer
identities 2.9 computes. Retained messages written by 3.0 under different
identities stay on the broker until they are overwritten or cleared — the same
caution as item 5 above, in the other direction.

**What does not need reverting**

The values you had to change to start 3.0 are all valid in 2.9:

- `--mm=PubSub` and `--mm=FullNetworkMessages` are both accepted by 2.9, and
  `PubSub` was already the 2.9 default.
- `--me=Json` and `--me=Uadp` are both accepted by 2.9.

So a configuration that starts 3.0 also starts 2.9, and there is no need to put
`Samples` or `Avro` back to roll back.

**Before you upgrade**

Keep a copy of the `published_nodes.json` and the deployment manifest as they
were before the upgrade. 3.0 rewrites the published nodes file when the
configuration is changed through the API or direct methods, and restoring a
known-good file is faster and less error-prone than reconstructing one. This
guide does not claim a file written by 3.0 loads unchanged into 2.9 — that has
not been verified here, and a kept copy makes the question moot.
