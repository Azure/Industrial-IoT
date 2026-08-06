# OPC Publisher threat model <!-- omit in toc -->

[Home](../readme.md) · [Security](../security.md)

- [`OpcPublisher.tm7`](./OpcPublisher.tm7) — Microsoft Threat Modeling Tool model
- [`dataflow.md`](./dataflow.md) — the same model as reviewable Mermaid diagrams
- [`../../../tools/threatmodel/generate_tm7.py`](../../../tools/threatmodel/generate_tm7.py)
  — generator; **edit this, not the `.tm7`**, and re-run it

## Scope

The module as deployed: the `opcpublisher` container on an IoT Edge gateway,
its OPC UA client, its configuration surfaces (mounted file, REST API, IoT Hub
direct methods, Azure Device Registry), its PKI store, and the transports it
publishes to. The CI/E2E test infrastructure is included as a second diagram
because it runs against real Azure resources with real credentials.

Out of scope: the security of the IoT Edge host itself, Azure IoT Hub's own
threat model, and the OPC UA stack implementation (an external dependency).

## Trust boundaries

| # | Boundary | Crossed by | Why it matters |
| --- | --- | --- | --- |
| TB1 | OT / plant network | Flows 1, 2 | The server side is frequently unauthenticated and flat. Treat every byte from it as hostile. |
| TB2 | `opcpublisher` container | Flows 3, 4, 5, 6, 10 | Separates module code from the host and from the rest of the edge deployment. |
| TB3 | Mounted volume | Flows 7, 8 | Config and PKI are shared with the host; the module cannot assume exclusive control. |
| TB4 | Cloud | Flows 4, 5, 6, 9 | Authenticated and encrypted, but the far side has full reconfiguration power. |

## Threats

STRIDE per element/flow. "Status" is what the code does **today** — this is a
description of the system, not a to-do list; the open items are called out in
[Residual risk](#residual-risk).

| # | Flow / element | STRIDE | Threat | Status |
| --- | --- | --- | --- | --- |
| T1 | (1) OPC UA server → stack | **S** | Rogue server impersonates the real one and feeds fabricated process values. | Mitigated by certificate validation, **unless** `--aa` / `AutoAcceptUntrustedCertificates` is set. That switch is a deliberate convenience for commissioning and should not survive into production. |
| T2 | (1) | **T**, **I** | Values read or modified in flight. | Mitigated only when the endpoint uses `SecurityMode` `Sign` or `SignAndEncrypt`. `None` is supported and is plaintext by design. |
| T3 | (1) | **D** | Hostile or faulty server floods notifications, or returns huge values, exhausting memory on the gateway. | Partly mitigated: bounded publish queues drop on overflow (`ingressNotificationsDropped`), server queue size is configurable. Message size is bounded per transport. |
| T4 | (2) stack → OPC UA server | **T**, **E** | Publisher is used as a **write** path into the plant: `Write` and `Call` reach the PLC. Whoever controls the configuration surface controls this. | This is the highest-consequence flow in the model. It is gated by the same authn as T6/T7 — no additional authorisation layer exists. |
| T5 | (3) operator → REST API | **S**, **E** | Weak, default, or leaked API key grants full reconfiguration, hence T4. | Mitigated by API key auth; no rate limiting or lockout. Do not expose the API beyond the edge network. |
| T6 | (4) IoT Hub → module | **E** | Any principal with IoT Hub *service connect* can invoke direct methods and reconfigure publishing, hence T4. | Accepted by design: the module trusts IoT Hub to authenticate the caller. Control who holds the service policy. |
| T7 | (7) `published_nodes.json` | **I** | The file holds OPC UA credentials. From 2.6 onward `EncryptedAuthPassword` is **plaintext by default** (2.5 and earlier always encrypted). | Host filesystem permissions are the only control. Prefer certificate or anonymous auth where possible. |
| T8 | (7) | **T** | Anything that can write the mounted file changes what the module publishes and writes (T4). | Host filesystem permissions. The module re-reads the file on change by design. |
| T9 | (8) PKI store | **T** | Injecting a certificate into the trusted store defeats T1 entirely. | Host filesystem permissions; store is on the mounted volume. |
| T10 | (5), (6) telemetry egress | **I** | Process data is commercially sensitive. | TLS on every supported transport; IoT Hub path additionally uses the module identity. |
| T11 | (11) diagnostics / logs | **I** | Verbose logging (`--dln`, diagnostics dumps) can echo payloads, endpoint URLs and node ids into logs that leave the device. | Off by default; treat as sensitive when enabled. |
| T12 | Heartbeat watchdog | **D**, **T** | A watchdog that re-sends the last known value can, if it races the value it waits for, emit stale data that a consumer records as real — corrupting a historian. | **Fixed.** The watchdog now requires `heartbeatInterval + min(publishingInterval, heartbeatInterval)` of silence, and the `Heartbeat` indicator lets a consumer filter re-sends. See [readme](../readme.md#watchdog-grace-period). |
| T13 | (10) image pull | **T** | Compromised or substituted module image. | Registry auth; images are cosign-signed in CI. |

### Test and CI infrastructure

| # | Flow / element | STRIDE | Threat | Status |
| --- | --- | --- | --- | --- |
| T14 | (12) OIDC federation | **S**, **E** | A workflow change that runs untrusted code could use the federated identity against the test subscription. | Mitigated: federated OIDC with no stored client secret, and E2E runs on `push`/`schedule`/`workflow_dispatch`, not on `pull_request` from forks. |
| T15 | (13) Key Vault → `$GITHUB_ENV` | **I** | Test secrets (IoT Hub connection string, SSH keys, Event Hub SAS) reach the runner environment. | Mitigated: values are `::add-mask::`ed and written via heredoc. Anything that echoes them defeats the mask. |
| T16 | (17) build artifacts | **I** | `.trx` files and captured module logs are uploaded as artifacts and can contain endpoint URLs, node ids and payloads. | Retention is bounded (14–30 days). Do not enable payload logging in CI. |
| T17 | Workflow `run:` blocks | **T**, **E** | Script injection through `${{ }}` interpolation of an input into a shell. GitHub substitutes textually *before* bash parses, so a quote in the value breaks out and executes — in a job holding `id-token: write`, which can mint an Azure OIDC assertion for the E2E service principal. | **Found and fixed.** `e2e-standalone.yml`'s `init` step interpolated `inputs.soak_minutes` and `inputs.resource_group_name` directly. Both now arrive via `env:` and are validated against a strict pattern before use. Reachable only via `workflow_dispatch`, so exploitable by a principal that can dispatch but not push. |
| T18 | `ci.yml` `images_build` | **T** | `github.ref_name` is interpolated into a `run:` block; git allows shell metacharacters in branch names. | Low: the job carries `if: github.event_name != 'pull_request'`, so creating such a branch requires write access, and the value is passed as a PowerShell argument rather than into `sh -c`. Pre-existing; worth tightening opportunistically. |

## Residual risk

Accepted, with the reasoning:

1. **T4/T6 — no second authorisation layer.** Anyone who can reach the
   configuration surface can make the publisher write to the plant. The
   mitigation is operational: restrict the IoT Hub service policy and the API
   key, and use OPC UA server-side permissions so the publisher's identity is
   only authorised for the nodes it legitimately needs.
2. **T7 — plaintext credentials in `published_nodes.json`.** Changed in 2.6 for
   usability. Host filesystem permissions are the control.
3. **T1 — `--aa` disables server certificate validation.** Intentionally
   available for commissioning; it must not be left on.

## Maintaining this model

Re-run the generator after editing the model:

```bash
python tools/threatmodel/generate_tm7.py
```

Then open `OpcPublisher.tm7` in the
[Microsoft Threat Modeling Tool](https://aka.ms/threatmodelingtool) and use
*Analysis view* to regenerate the threat list. The `.tm7` here intentionally
ships with an empty `<ThreatInstances/>` so the tool generates them from the
current stencil set rather than carrying a stale, hand-written list.

> The `.tm7` is authored against the TMT 2016/7.x schema and validated for
> well-formedness and referential integrity (every data flow endpoint resolves
> to a real element) by CI-independent checks. It has **not** been round-tripped
> through the Threat Modeling Tool GUI in this repository, since the tool is
> Windows-desktop only. If the tool reports a schema problem, fix the generator
> and regenerate rather than editing the XML by hand.
