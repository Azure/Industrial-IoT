# MCP tool server (`--mcp`)

[Home](./readme.md)

OPC Publisher can expose the OPC UA [Model Context Protocol](https://modelcontextprotocol.io)
tool server so that an AI agent can drive it directly. It is **disabled by
default** and must be turned on with `--mcp`.

> [!WARNING]
> The MCP tools are not read-only and they are not a diagnostic sideshow. Read
> [Security](#security) before enabling this anywhere that matters.

## Enabling it

```bash
./publisher --mcp
```

`--mcp` takes no port. The tool server is mapped at the `/mcp` path of the HTTP
server OPC Publisher already runs, which means it:

- listens on the **already configured port** (`--httpserverport`, default `443`
  in a container running as root, otherwise `9072`), and
- is protected by the **same api key authentication** as the REST API.

There is deliberately no separate listener. A dedicated port would have to
rebuild the authentication pipeline by hand and would be one more thing to
firewall.

If no HTTP port is configured at all, `--mcp` enables one on the default HTTPS
port rather than starting an endpoint nothing can reach.

## Pointing an agent at it

The endpoint speaks streamable HTTP at `https://<host>:<port>/mcp` and requires
the api key in the `Authorization` header, exactly like the REST API:

```json
{
  "servers": {
    "opc-publisher": {
      "type": "http",
      "url": "https://localhost:9072/mcp",
      "headers": {
        "Authorization": "<api key>"
      }
    }
  }
}
```

The api key is generated on first start and can be read from the module's
`ApiKey` twin property, or set explicitly with `--api-key`. See
[Command line](./commandline.md).

## What the tools can do

Enabling `--mcp` exposes the full OPC UA tool set from
`OPCFoundation.NetStandard.Opc.Ua.Mcp.Core` and the protocol diagnostics tools
from `OPCFoundation.NetStandard.Opc.Ua.Mcp.Diagnostics`:

| Area | Examples |
|---|---|
| Connections | discover endpoints, connect, disconnect, list sessions |
| Address space | browse, read and **write** attributes and values |
| Methods | **call** methods on connected servers |
| Subscriptions | create, modify and delete subscriptions and monitored items |
| Node management | add and delete nodes and references |
| Configuration and PKI | read and update configuration, manage certificates and trust lists |
| NodeSet | export the address space |
| Diagnostics | capture, decode and replay OPC UA traffic |

## Security

`--mcp` widens what a single credential protects. Anything that can reach the
HTTP port and present the api key can:

- **write to and call into the plant** through the Write and Call tools, and
- **capture OPC UA traffic, decode it — which discloses symmetric channel keys —
  and replay it** through the diagnostics tools.

Two consequences are worth stating plainly:

1. OPC Publisher has **no second authorisation layer**. Authentication is the
   only gate; there are no per-tool permissions. See the
   [threat model](./security.md).
2. Upstream ships the key-disclosing diagnostics tools **off** by default behind
   `Pcap:EnableDiagnosticsTools`. OPC Publisher deliberately enables them
   together with `--mcp`, so turning on `--mcp` turns those on too.

Only enable `--mcp` on a trusted network, and treat the api key as equivalent to
write access to every server the publisher is connected to.

### Diagnostics tools in a container

The capture tools have two modes and they behave differently in the shipped
container image:

- `source='inproc-client'` captures OPC Publisher's own OPC UA traffic and needs
  neither libpcap nor extra privileges. This is the mode that works out of the
  box.
- `source='nic'` and `list_interfaces` capture from a network interface through
  libpcap (Linux/macOS) or Npcap (Windows).

**libpcap ships in the container image.** It is staged in from an Azure Linux
builder during the image build, so `libpcap.so` → `libpcap.so.1` is present
under `/usr/lib` on both `linux/amd64` and `linux/arm64`. The image itself stays
distroless: no package manager and no shell come with it.

**The library is necessary but not sufficient.** Capturing from a network
interface also needs the `CAP_NET_RAW` capability, and the image **runs as a
non-root user** (`UID 1654`, inherited from the distroless base). Docker grants
`NET_RAW` to the container by default, but a non-root process does not hold it
without ambient or file capabilities, so `source='nic'` will fail with a
permission error under a default `docker run`.

To use NIC capture you have to give the container the privilege explicitly, for
example:

```bash
docker run --user 0 --cap-add NET_RAW ... ghcr.io/azure/iotedge/opc-publisher --mcp
```

Weigh that against what it costs: running as root undoes the non-root hardening
of the image for every other part of the process. `source='inproc-client'` is
the better answer whenever the traffic you want is OPC Publisher's own, which is
the common case.

On Windows and for local (non-container) runs, install
[Npcap](https://npcap.com/) or libpcap yourself as before.

## Native AOT is not supported with `--mcp`

OPC Publisher has an opt-in Native AOT publish (`-p:IIoTPublishAot=true`). The
MCP tool server does **not** work in that configuration and should not be
enabled there:

- The MCP SDK builds each tool's input schema reflectively, which is unavailable
  in an ahead-of-time published application.
- The diagnostics tools additionally depend on SharpPcap and on reflective
  packet dissection, neither of which is trim safe.

The default (non-AOT) publish is unaffected. Upstream tracks the AOT gap in
`Opc.Ua.Aot.Tests.McpAotTests`.
