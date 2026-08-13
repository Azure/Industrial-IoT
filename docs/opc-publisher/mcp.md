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
the api key in the `Authorization` header, exactly like the REST API. The header
carries the `ApiKey` scheme followed by the key — a bare key is rejected with
`401`:

```json
{
  "servers": {
    "opc-publisher": {
      "type": "http",
      "url": "https://localhost:9072/mcp",
      "headers": {
        "Authorization": "ApiKey <api key>"
      }
    }
  }
}
```

The api key is generated on first start and can be read from the module's
`ApiKey` twin property, or set explicitly with `--api-key`. See
[Command line](./commandline.md).

## Running the container

The published image is distroless and runs as a **non-root user** (`UID 1654`).
That is the shape to prefer, and everything except capturing from a network
interface works in it.

```bash
docker run -d --name opc-publisher \
  -p 9072:9072 \
  -v /srv/publisher:/mount \
  ghcr.io/azure/iotedge/opc-publisher \
    --pf=/mount/published_nodes.json \
    --api-key=<api key> \
    --mcp
```

Two things about the ports. Because OPC Publisher only defaults to the
privileged port when it runs as root in a container, the non-root image listens
on **9072**, not 443. And `--mcp` needs an HTTP listener: if none is configured
it enables one on the default HTTPS port rather than starting an endpoint
nothing can reach.

The plaintext listener is **off unless you ask for it** with `--unsecurehttp`.
If you do turn it on, the api key travels on it in the clear, and because the
MCP tools can extract secure channel keys and read capture artifacts, **`/mcp`
is refused on that listener** and answers `403` there. Use the TLS port.

### Where captures are written

Capture artifacts (`capture.pcap`, `keys.uakeys.json`, `keys.uakeys.txt`) are
written under the process's local application data, which is
`/home/app/OPCFoundation/opcua-pcap/...` for the non-root image. That is inside
the container, so **captures are lost when the container is replaced**, and they
are not reachable from the host.

Captures contain plant traffic and the key log decrypts it, so treat that
directory exactly like the PKI folder. If you want to keep captures, or hand a
pcap to someone, mount a volume for them and copy the files out deliberately.

### On IoT Edge

The same settings expressed as `createOptions`:

```json
{
  "HostConfig": {
    "Binds": [ "/srv/publisher:/mount" ],
    "PortBindings": {
      "9072/tcp": [ { "HostPort": "9072" } ]
    }
  }
}
```

with `--mcp` added to the module's command line arguments.

### If you need to capture from a network interface

NIC capture needs two things the default deployment does not have: the
`CAP_NET_RAW` capability, and a user that actually holds it. Docker grants
`NET_RAW` to the container by default, but a non-root process does not inherit
it, so the container has to run as root as well:

```bash
docker run -d --name opc-publisher \
  --user 0 --cap-add NET_RAW \
  -p 443:443 \
  -v /srv/publisher:/mount \
  ghcr.io/azure/iotedge/opc-publisher \
    --pf=/mount/published_nodes.json --api-key=<api key> --mcp
```

Note the ports change to **80/443** in this mode, for the reason given above,
so the TLS port to publish is 443 and the plaintext one to leave unpublished
is 80.

On IoT Edge the equivalent is `"CapAdd": [ "NET_RAW" ]` and `"User": "0"` in
`HostConfig`; on Kubernetes it is a `securityContext` with `runAsUser: 0` and
`capabilities.add: ["NET_RAW"]`.

Weigh that against what it costs. Running as root undoes the non-root hardening
for every other part of the process, and in-process capture — which needs
neither — already sees everything OPC Publisher itself sends and receives.
Reach for NIC capture only when you need traffic OPC Publisher is *not* a party
to.

> [!WARNING]
> Without the capability, NIC capture **does not fail loudly**. `start_capture`
> returns a running session and `stop_capture` completes with `frameCount: 0`,
> while the module log records
> `SharpPcap.PcapException: Unable to activate the adapter (eth0). (Error Code: PermissionDenied)`.
> An empty capture is the symptom to recognise.


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

## Troubleshooting interop problems

This is what the diagnostics tools are for. OPC UA interop failures are usually
invisible from logs alone — the interesting detail is on the wire, encrypted.
In-process capture records OPC Publisher's own traffic **together with the key
material**, so the traffic can be decrypted and read back as service calls.

> [!IMPORTANT]
> Use the enum values below exactly as written. The tool descriptions show
> hyphenated forms such as `inproc-client` and `service-timeline`, but the
> parameters deserialize as .NET enums, so only the PascalCase spellings
> (`InProcessClient`, `ServiceTimeline`) are accepted. A hyphenated `source`
> silently falls back to `Nic` and then fails complaining about libpcap, which
> is a confusing way to learn this.

### A server rejects the connection

1. `list_active_channels` — see whether a channel exists at all.
2. `start_capture` with `{ "source": "InProcessClient" }` **before** retrying
   the connection. The tap records every channel created afterwards, so the
   failing handshake is captured in full.
3. Reconnect (`Connect`, or let the publisher retry).
4. `stop_capture`, then `get_capture` with `format: "ServiceTimeline"`.

The timeline names the service that failed. `OpenSecureChannel` failures point
at certificates or security policy; `CreateSession` and `ActivateSession`
failures point at user identity or the application URI.

### Values are missing, stale, or wrong

1. `start_capture` `InProcessClient`, let it run through a few publishing
   cycles, `stop_capture`.
2. `summarize_service_calls` — call counts, average latency and error rate per
   service. A `Read` or `Publish` error rate above zero, or a latency spike,
   localises the problem quickly.
3. `get_capture` with `format: "ServiceTimeline"` to read the offending calls.

### A suspected encoding or interop bug

1. `get_capture` with `format: "Pcap"` (or `"PcapNg"`) — returned as an
   embedded resource.
2. `dump_keys` for the same session.
3. Open the pcap in Wireshark with the key log, or stay in the tools and use
   `decode_pcap_with_keys`, which takes a pcap and a key log from disk and does
   not require them to come from the same capture session.

### Reproducing without the plant

`replay_pcap` with `mode: "mock-server"` opens a listener that replays the
captured server side to a connecting client, so a server-specific bug can be
reproduced on a desk. `mode: "mock-client"` re-issues the captured requests
against a live endpoint. `stop_replay` ends it.

### What works as root and as non-root

Established by running both, not inferred from capability lists:

| | non-root (as shipped) | root + `CAP_NET_RAW` |
| --- | --- | --- |
| `start_capture` `InProcessClient` | yes | yes |
| decode, `dump_keys`, `summarize_service_calls`, replay | yes | yes |
| `start_capture` `Nic` | **no** — session runs, captures 0 frames | yes |
| `list_interfaces` | no | no |

`list_interfaces` currently fails in both modes: the enumerator reads a device's
link type without opening the device first, and the resulting
`DeviceNotReadyException` is reported as `Unable to enumerate devices — is
libpcap / Npcap installed?` even though libpcap is present in the image. Pass
the interface name to `start_capture` directly instead; inside a container it is
almost always `eth0`.

**Everything except NIC capture works in the default non-root deployment.** That
covers essentially every interop question about traffic OPC Publisher itself
exchanges, which is the common case.

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

### The plaintext listener

`--unsecurehttp` opens a plaintext HTTP listener on all interfaces (9071
non-root, 80 as root). It is **off unless you ask for it**.

That was not always true. Through 2.9 and the 3.0 previews the listener was
always on, even though the option documented itself as `Default: disabled` and
warned in the same sentence that it exposes the api key on the network — an
absent, empty or unparseable setting all resolved to the default port, so the
documented state could not be reached by any configuration input. As of 3.0 the
behaviour matches the documentation.

If you do enable it, the api key is sent on it in the clear. Because the MCP
tools can extract secure channel keys and read capture artifacts, `/mcp` is
**refused on that listener** and answers `403` regardless.

### libpcap in the container image

**libpcap ships in the image.** It is staged in from an Azure Linux builder
during the image build, so `libpcap.so` → `libpcap.so.1` is present under
`/usr/lib` on both `linux/amd64` and `linux/arm64`. The image itself stays
distroless: no package manager and no shell come with it.

The library is necessary but not sufficient — see
[what works as root and as non-root](#what-works-as-root-and-as-non-root) for
what it does and does not buy you, and
[if you need to capture from a network interface](#if-you-need-to-capture-from-a-network-interface)
for how to grant the missing privilege.

On Windows and for local (non-container) runs, install
[Npcap](https://npcap.com/) or libpcap yourself.

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
