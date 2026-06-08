We'll be glad to accept patches and contributions to the project. There are just few guidelines we ask to follow.

Contribution License Agreement
==============================

If you want/plan to contribute, we ask you to sign a [CLA](https://cla.microsoft.com/) (Contribution License Agreement).  A friendly bot will remind you about it when you submit a pull-request.

Submitting a contribution
=========================

It's generally best to start by [opening a new issue](https://help.github.com/articles/creating-an-issue) describing the work you intend to submit. Even for minor tasks, it's helpful to know what contributors are working on. Please mention in the initial issue that you are planning to work on it, so that it can be assigned to you.

Follow the usual GitHub flow process of [forking the project](https://help.github.com/articles/fork-a-repo), and setup a new branch to work in. Each group of changes should be done in separate branches, in order to ensure that a pull request only includes the changes related to one issue.

Any significant change should almost always be accompanied by tests. Look at the existing tests to see the testing approach and style used.  

Follow the project coding style, to ensure consistency and quick code reviews.  We heavily rely on dependency injection using *Autofac* and thus ask to follow the same paradigm when adding new code.

Do your best to have clear commit messages for each change, in order to keep consistency throughout the project. Reference the issue number (#num). A good commit message serves at least these purposes:
* Speed up the pull request review process
* Help future developers to understand the purpose of your code
* Help the maintainer write release notes

One-line messages are fine for small changes, but bigger changes should look like this:
```
$ git commit -m "A brief summary of the commit
>
> A paragraph describing what changed and its impact."
```

Finally, push the commits to your fork, submit a pull request, wait for all gates to pass and fix any issues found as part of the gate process.  The team might ask for some [changes](https://help.github.com/articles/committing-changes-to-a-pull-request-branch-created-from-a-fork) before merging the pull request.

GitHub Actions CI
=================

This repository builds and tests in GitHub Actions in addition to the internal Azure DevOps pipeline. The two pipelines are complementary -- the internal pipeline is authoritative for official releases, and the GitHub Actions workflows publish to GHCR / GitHub Packages so the public repo is self-contained.

Workflows under `.github/workflows/`:

| Workflow | Purpose | Triggers |
|---|---|---|
| `ci.yml` | Builds, unit tests with coverage, multi-arch container images (GHCR), NuGet packages (GitHub Packages), invokes E2E. | PR, push to `main`/`release/*`, nightly schedule, manual dispatch. |
| `e2e-standalone.yml` | Reusable end-to-end standalone test orchestrator (deploy Azure resources, run tests, cleanup). Called from `ci.yml`. | `workflow_call`, manual dispatch. |
| `e2e-run-tests.yml` | Reusable test runner used twice by `e2e-standalone.yml` (`PublisherMode=AE` then `PublisherMode=standalone`). | `workflow_call`. |
| `codeql.yml` | CodeQL static analysis (csharp). | PR, push, weekly schedule. |

### Container images (GHCR)

Multi-architecture (linux/amd64, linux/arm/v7, linux/arm64) images are pushed to:

* `ghcr.io/azure/iotedge/opc-publisher`
* `ghcr.io/azure/iot/opc-ua-test-server` (linux/amd64 only)

Tagged with the nbgv semantic version (e.g. `2.9.17`) and, when pushed from `main`, also `:latest`. Images are signed with [cosign](https://github.com/sigstore/cosign) keyless signatures via GitHub OIDC; verify with `cosign verify ghcr.io/azure/iotedge/opc-publisher:<tag>`.

For the E2E pipeline to pull these images from an IoT Edge VM at runtime, the GHCR packages must be **public**. If you keep them private, set repository secrets `GHCR_USERNAME` and `GHCR_TOKEN` (a classic PAT with `read:packages`) so the deployment scripts can write registry credentials to KeyVault.

### NuGet packages

Three packages are published to `https://nuget.pkg.github.com/Azure/index.json`:
`Azure.IIoT.OpcUa.Publisher.Models`, `Azure.IIoT.OpcUa.Publisher.Sdk`, `Azure.IIoT.OpcUa.Publisher.Testing.Servers`. Publish runs only on push to `main` or `release/*`.

### Azure authentication (federated OIDC, no client secret)

The E2E workflow uses [`azure/login@v2`](https://github.com/Azure/login) with workload identity federation. Configure these repository variables (not secrets):

| Repository variable | Description |
|---|---|
| `AZURE_CLIENT_ID` | Application (client) ID of the Entra ID App Registration used for E2E. |
| `AZURE_TENANT_ID` | Entra ID tenant ID. |
| `AZURE_SUBSCRIPTION_ID` | Subscription ID for E2E resources. |

On the Azure side, the App Registration must have federated credentials for these subjects (add one per branch you want to run E2E from):

* `repo:Azure/Industrial-IoT:ref:refs/heads/main`
* `repo:Azure/Industrial-IoT:ref:refs/heads/release/*` (one per release branch)

The federated identity needs:

* **Contributor** on the target subscription (creates resource groups, IoT Hub, KeyVault, VMs, ACI).
* **Key Vault Secrets Officer** on secrets in the deployment-created vaults (the deployment scripts create new KeyVaults in RBAC mode and migrate any pre-existing access-policy vaults; `SetKeyVaultPermissions.ps1` assigns the role automatically).

### Restoring NuGet packages from runners

The repo's `Nuget.Config` points at an internal Azure Artifacts feed that GitHub-hosted runners cannot reach. Every workflow `dotnet restore`/`pack`/tool install command pins to nuget.org explicitly (`-s https://api.nuget.org/v3/index.json`). When adding new workflow steps, follow the same pattern.

### Running E2E manually

Use the **Run workflow** button on `e2e-standalone.yml` and provide an `image_tag` already pushed to GHCR. Set `cleanup: false` to keep the resource group after the run for debugging.
