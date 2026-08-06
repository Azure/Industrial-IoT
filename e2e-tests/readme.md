# E2E test

## Background

Properties of E2E tests:

* black-box
* end-to-end
* high cost tests (long preparation / execution times)

Goal of E2E tests:

* check the customer point of view (validate)
* cover the most important scenarios

## How to write E2E tests

### Preparation

Use the `TestCaseOrderer` attribute with `TestCaseOrderer.FullName` parameter on your
test class, and `PriorityOrder` on your test methods to order them.

Use the `Collection` attribute on your test class to share the context between test
methods.

Use the `Trait` attribute on your test class so the runner can filter to e2e tests:

```csharp
[Trait("PublisherMode", "AE")]
```

The test class gets the context as a parameter of its constructor. Use one of the
following context types:

* `IIoTPlatformTestContext` — general-purpose context with shared state
* `IIoTStandaloneTestContext` — for standalone (publisher-only) mode

In order for the context to log information, the `OutputHelper` of the context needs to
be set to the `IOutputHelper` the test class gets as a constructor parameter.

If necessary you can clean the context by calling its `Reset` method, otherwise its state
is shared between test methods, and even between test classes of the same `Collection`.
It is recommended to call `Reset` on the context in the first test method of a test
class.

Use and extend the `TestHelper` class.

## Long running telemetry quality tests

Two soak tests validate the quality of the telemetry stream produced by an OPC Publisher
module that is actually deployed to IoT Edge, complementing the in-process soak in
`src/Azure.IIoT.OpcUa.Publisher.Module/tests` (which is run by `.github/workflows/soak.yml`).
They use OPC PLC counter nodes, which increment by exactly one per cycle, so the value is
its own sequence number: a lost value is a gap, a reordered value is a decrease, and the
expected source timestamp distance is exactly one update interval.

| Test | Trait | Scenario | Asserts |
| --- | --- | --- | --- |
| `FTelemetryQualityCountersTestTheory` | `PublisherMode=soakcounters` | 100 nodes counting up every **2 s**, published with 2 s publishing/sampling interval, queue size 10 and a **2 s heartbeat** | Nothing is lost, reordered, repeated or unevenly spaced, and **no heartbeat fires** — a value arrives on every publish cycle, so the watchdog must stay silent |
| `FTelemetryQualityHeartbeatTestTheory` | `PublisherMode=soakheartbeat` | 20 nodes counting up every **2 min**, with a **10 s heartbeat** | Heartbeats **do** fire on every node at the configured cadence and never before the watchdog grace period; every repeat carries the `Heartbeat` indicator; heartbeats never alter the source timestamp; and with heartbeats excluded the value stream is complete, ordered and exactly 2 minutes apart |

### Infrastructure

Both tests **reuse the deployed resource group, IoT Hub and IoT Edge VM**. Standing up a
second environment would roughly double the Azure spend and the resource-leak surface
without adding coverage — the code under test is the publisher module, not the hub.
Everything that could cause interference is isolated per scenario instead:

* its own publisher **module identity** (`publisher_soak_fast` / `publisher_soak_slow`)
  and layered deployment, with its own `--pf` published nodes file and `--pki` folder,
* its own **OPC PLC simulation container** (distinct name, DNS label and sizing — pass
  `nameDiscriminator` to `TestHelper.CreateSimulationContainerAsync`),
* its own `DataSetWriterGroup` / `DataSetWriterId`, and
* its own Event Hub **consumer group** (`SoakCounters` / `SoakHeartbeat`), because the
  IoT Hub built-in endpoint allows only five concurrent readers per partition and group.

Telemetry is attributed by the `iothub-connection-module-id` system property, so each
scenario only ever sees its own publisher's messages.

Because each trait value is run by its own `dotnet test` process, the two soaks run **in
parallel with each other and with the A&E job** — the xUnit runner is configured with
`parallelizeTestCollections: false`, so putting them in the same process would serialize
them instead.

### Configuration

| Environment variable | Default | Meaning |
| --- | --- | --- |
| `IIOT_E2E_SOAK_MINUTES` | `30` | How long telemetry is observed after the warm up |
| `IIOT_E2E_SOAK_NODES` | `100` | Number of 2-second counter nodes |

Both are surfaced as the `soak_minutes` and `soak_nodes` inputs of
`.github/workflows/e2e-standalone.yml`.

> The in-process soak (`src/Azure.IIoT.OpcUa.Publisher.Module/tests`) is gated separately and
> skips unless `IIOT_TELEMETRY_SOAK=1` (or a node count / duration / full-scale override) is
> set. It runs for minutes and is sensitive to machine load, so it must not run as part of an
> ordinary solution-wide test pass — in particular the internal build, which runs the whole
> solution and cannot be filtered from this repository.

> The node count is bounded by the **IoT Hub S1 daily message quota**, which is shared
> with the A&E job, and by the two vCPU IoT Edge VM. At the default of 100 nodes the soak
> produces roughly one network message per second. Raising it much above 250 needs an IoT
> Hub SKU bump. Scale itself is covered by the in-process soak, which runs 3000 nodes.

## Authentication

The tests use federated identity (workload identity federation) end to end:

* In Azure DevOps, the service connection is configured as **Workload identity federation**
  — no client secret. The pipeline tasks (`AzurePowerShell@5`, `AzureCLI@2`) authenticate
  via the federated SP automatically.
* In test code, `TestHelper` builds a credential chain that prefers `AzurePipelinesCredential`
  (federated, uses `SYSTEM_ACCESSTOKEN` + `AZURE_CLIENT_ID` + `AZURE_TENANT_ID`), then falls
  back to `DefaultAzureCredential` and `AzureCliCredential` for local-dev runs.
* No long-lived secrets are stored in Key Vault for ACR pulls, storage access, or SSH
  to the Edge VM. Where credentials are unavoidable (IoT Edge device symmetric key, SSH
  keypair for the Edge VM), they are generated just-in-time per deployment and held only
  in pipeline-secret variables — never persisted at rest.

## Executing tests locally (Visual Studio / `dotnet test`)

You can reuse a deployed test environment to speed up test development.

1. Run the e2e pipeline with the `Cleanup` parameter set to `false` (so the resource
   group survives after the pipeline completes).
2. Find the `ResourceGroupName` in the pipeline run summary.
3. Tag the resource group so the daily garbage-collector doesn't delete it:
   * `owner=<your-alias>`
   * `DoNotDelete=true`
4. Grant yourself the "Key Vault Secrets User" role on the test Key Vault. The KV uses
   Azure RBAC, not access policies:

   ```powershell
   $kvId = (Get-AzKeyVault -ResourceGroupName <rg>).ResourceId
   $myId = (Get-AzADUser -SignedIn).Id   # or -Mail / -UserPrincipalName / -ObjectId
   New-AzRoleAssignment -ObjectId $myId -RoleDefinitionName "Key Vault Secrets User" -Scope $kvId
   ```

   The helper script `tools/e2etesting/SetKeyVaultPermissions.ps1` does the same thing
   if you pass `-ResourceGroupName` and optionally `-ServicePrincipalName`.
5. Sign in to the Azure CLI as the principal you just granted:

   ```powershell
   az login --tenant <test-tenant-id>
   ```
6. Set the test-environment pointers as env vars (these are non-secret resource IDs;
   the credentials come from `az login`):

   ```powershell
   $env:PCS_SUBSCRIPTION_ID = "<subscription-id>"
   $env:PCS_RESOURCE_GROUP  = "<rg-name>"
   $env:PCS_AUTH_TENANT     = "<tenant-id>"
   $env:IOTHUB_HOSTNAME     = "<iot-hub-fqdn>"
   # plus whatever else the test you're running expects (PLC_SIMULATION_URLS, etc.)
   ```
7. Run the tests from Visual Studio or `dotnet test`. The test process picks up your
   `az login` session via `DefaultAzureCredential` for ARM operations.

When you're done, re-run the pipeline with `Cleanup=true` (and the recorded
`ResourceGroupName`) to tear down the environment, or let the daily garbage collector
do it (if your RG doesn't have the `DoNotDelete=true` tag).
