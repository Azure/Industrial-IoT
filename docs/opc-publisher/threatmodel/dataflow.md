# OPC Publisher data flow diagram <!-- omit in toc -->

[Home](../readme.md) · [Security](../security.md) · [Threat model](./readme.md)

This is the data flow diagram (DFD) that backs
[`OpcPublisher.tm7`](./OpcPublisher.tm7). It is kept in Mermaid so that it is
reviewable in a pull request; the `.tm7` is the machine-readable form for the
Microsoft Threat Modeling Tool.

Numbers on the flows (`(1)`, `(2)`, …) match the `Flow #` column of the threat
table in [readme.md](./readme.md).

## Level 0 — context

```mermaid
flowchart LR
    OPCUA["OPC UA Server<br/>(field device / PLC)"]
    OPER["Operator / Engineer"]
    PUB["OPC Publisher<br/>(IoT Edge module)"]
    CLOUD["Azure IoT Hub /<br/>MQTT broker /<br/>HTTP endpoint"]

    OPCUA -- "(1) telemetry, opc.tcp" --> PUB
    PUB -- "(2) browse / read / write / call" --> OPCUA
    OPER -- "(3) REST API, direct methods" --> PUB
    PUB -- "(5) telemetry, runtime state" --> CLOUD
    CLOUD -- "(4) direct methods, twin desired props" --> PUB
```

## Level 1 — trust boundaries and stores

Trust boundaries are the dashed containers. Anything crossing one is a place
where authentication, authorisation, and input validation have to be argued
explicitly.

```mermaid
flowchart TB
    subgraph OT["OT / plant network — untrusted, flat, often unauthenticated"]
        OPCUA["OPC UA Server<br/>(external entity)"]
    end

    subgraph EDGE["IoT Edge gateway host"]
        subgraph MOD["opcpublisher container — trust boundary: process + container"]
            STACK["OPC UA client stack<br/>(session, subscriptions,<br/>monitored items)"]
            ENGINE["Publisher engine<br/>(encoders, writer groups,<br/>batching, heartbeat watchdog)"]
            API["REST API host<br/>(ASP.NET Core)"]
            CFG["Configuration service<br/>(PublishedNodesJsonServices)"]
        end
        EHUB["edgeHub<br/>(IoT Edge runtime)"]
        subgraph MNT["mounted volume — trust boundary: filesystem"]
            PNJSON[("published_nodes.json")]
            PKI[("PKI stores:<br/>own / trusted / issuer /<br/>rejected")]
        end
    end

    subgraph AZ["Azure / cloud"]
        IOTHUB["IoT Hub"]
        BROKER["MQTT broker /<br/>HTTP / Dapr / Event Hubs"]
        ADR["Azure Device Registry"]
        ACR["Container registry"]
    end

    OPER["Operator / Engineer<br/>(external entity)"]

    OPCUA -- "(1) DataChange / Event notifications" --> STACK
    STACK -- "(2) Browse, Read, Write, Call" --> OPCUA
    STACK <-. "(8) app + peer certs, CRLs,<br/>rejected certs" .-> PKI

    STACK --> ENGINE
    ENGINE -- "(5) network messages" --> EHUB
    EHUB -- "(5) D2C telemetry" --> IOTHUB
    ENGINE -- "(6) telemetry (direct transport)" --> BROKER

    OPER -- "(3) HTTPS + API key" --> API
    IOTHUB -- "(4) direct methods /<br/>twin desired properties" --> EHUB
    EHUB --> API
    API --> CFG
    CFG <-. "(7) read / write config" .-> PNJSON
    CFG --> STACK

    ADR -. "(9) asset / device definitions" .-> CFG
    ACR -. "(10) module image" .-> MOD
    ENGINE -- "(11) diagnostics, runtime state, logs" --> EHUB

    classDef ext fill:#f5f5f5,stroke:#666,stroke-dasharray:3 3
    classDef store fill:#eef,stroke:#446
    class OPCUA,OPER,ACR,ADR ext
    class PNJSON,PKI store
```

## Test and CI data flows

The end-to-end and soak test infrastructure creates its own, separate flows.
They do not exist in a customer deployment, but they do run against real Azure
resources with real credentials, so they are in scope for the threat model.

```mermaid
flowchart LR
    subgraph GH["GitHub Actions runner — untrusted for pull requests"]
        CI["ci.yml / soak.yml /<br/>e2e-standalone.yml"]
        TESTS["xUnit test host"]
    end

    subgraph AZT["Azure test subscription"]
        KV[("Key Vault<br/>(test secrets)")]
        HUB["Test IoT Hub"]
        VM["IoT Edge VM"]
        ACI["OPC PLC containers"]
    end

    CI -- "(12) OIDC federation<br/>(no client secret)" --> AZT
    KV -- "(13) secrets to \$GITHUB_ENV,<br/>masked" --> CI
    CI --> TESTS
    TESTS -- "(14) direct methods" --> HUB
    HUB --> VM
    TESTS -- "(15) read telemetry<br/>(Event Hub SAS)" --> HUB
    TESTS -- "(16) create / delete<br/>container groups" --> ACI
    TESTS -- "(17) trx + logs" --> ART[("Build artifacts")]

    classDef store fill:#eef,stroke:#446
    class KV,ART store
```

## Assumptions

These are the load-bearing assumptions. If one of them is false in a given
deployment, the corresponding threats in [readme.md](./readme.md) change
severity.

1. The OT network is **not** trusted. An OPC UA server may be malicious,
   compromised, or simply faulty, and can return hostile or malformed data.
2. The IoT Edge gateway host is trusted. An attacker with root on the host has
   already won: they can read the module's mounted volume, its PKI store and its
   module identity.
3. `published_nodes.json` and the PKI directory are on a mounted volume shared
   with the host. Their confidentiality and integrity are the host's
   responsibility, not the module's.
4. IoT Hub is trusted for authentication of direct methods; the module does not
   independently authenticate the caller of a direct method.
5. The REST API is reachable only from the edge network and is protected by the
   configured API key. It is not intended for exposure to the internet.
