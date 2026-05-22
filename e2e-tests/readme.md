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
