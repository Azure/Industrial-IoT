# Native AOT publishing and container images

OPC Publisher publishes as a [Native AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
binary. This directory holds the accepted AOT warning baseline
(`publisher-module-nativeaot-baseline.md` / `.json`) and this note, which
records how a release candidate image is produced and — more importantly — how
to prove the image really is native.

## Table of contents

- [The trap: a green publish that is not AOT](#the-trap-a-green-publish-that-is-not-aot)
- [Building in CI](#building-in-ci)
- [Building locally on a non-Linux host](#building-locally-on-a-non-linux-host)
- [Proving the binary is native](#proving-the-binary-is-native)
- [Proving the image runs](#proving-the-image-runs)
- [Expected timings](#expected-timings)
- [Accepted warnings](#accepted-warnings)

## The trap: a green publish that is not AOT

`dotnet publish -p:IIoTPublishAot=true` **silently degrades to a managed
publish** when the SDK in use is older than the one the repo targets. There is
no error and no warning. The publish succeeds, the container builds, the image
loads — and then `docker run` fails at once because the entrypoint is not an
executable.

This is exactly what `mcr.microsoft.com/dotnet/sdk:10.0-noble-aot` does today:
the image ships SDK `10.0.302`, which is older than the repo's SDK, so the AOT
toolchain is skipped and managed DLLs are emitted instead.

**Never infer that a publish was AOT from its exit code.** Always run the
[native binary check](#proving-the-binary-is-native) before packaging.

## Building in CI

CI is the supported path and needs none of the workarounds below. Two jobs in
`.github/workflows/ci.yml` gate it, both on `ubuntu-latest` with
`actions/setup-dotnet` resolving `10.0.x` to the repo's SDK:

- **`aot_publish`** — `linux-x64`, and the job that enforces the warning
  baseline.
- **`aot_publish_arm64`** — `linux-arm64` cross-compile. The x64 gate cannot
  catch host/target architecture faults because there host and target agree;
  cross-compiling surfaces problems such as a source generator being emitted
  for the target architecture and then failing to load.

Both restore against nuget.org explicitly, because the repo's `Nuget.Config`
points only at the internal Azure Artifacts feed while the ILC runtime packages
live on nuget.org:

```bash
dotnet restore src/Azure.IIoT.OpcUa.Publisher.Module/src \
  -r linux-x64 -p:IIoTPublishAot=true -s https://api.nuget.org/v3/index.json
dotnet publish src/Azure.IIoT.OpcUa.Publisher.Module/src \
  -c Release -r linux-x64 --self-contained --no-restore -p:IIoTPublishAot=true
```

`-p:IIoTPublishAot=true` rather than `-p:PublishAot=true` because the switch
selects the AOT *configuration*, not just the toolchain: it also compiles the MCP
tool server out of the image, whose schema generation is reflective and cannot
work under AOT.

## Building locally on a non-Linux host

Native AOT links with the host platform's C/C++ toolchain, so a Linux image
cannot be produced from a Windows host directly — "Cross-OS native compilation
is not supported" — and a Windows-host AOT build needs the Windows SDK
(`advapi32.lib`), which a dev box may not have. Build inside a Linux container
instead, installing the repo's SDK over the one the base image ships:

```powershell
$env:TEMP='D:\buildtemp'; $env:TMP='D:\buildtemp'
New-Item -ItemType Directory -Force -Path 'D:\buildtemp\aot-e2e' | Out-Null
```

```powershell
docker run --rm `
  -e TMPDIR=/aot-e2e/tmp `
  -v D:\git\azure\Industrial-IoT3:/src `
  -v D:\buildtemp\aot-e2e:/aot-e2e `
  mcr.microsoft.com/dotnet/sdk:10.0-noble-aot `
  bash -lc @'
set -euo pipefail
mkdir -p /aot-e2e/dotnet /aot-e2e/nuget /aot-e2e/home /aot-e2e/tmp /aot-e2e/logs
export TMPDIR=/aot-e2e/tmp
export HOME=/aot-e2e/home
export DOTNET_CLI_HOME=/aot-e2e/home
export NUGET_PACKAGES=/aot-e2e/nuget
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=true

# The base image SDK is too old and would emit managed DLLs. Install the
# version the repo builds with; keep this in step with global SDK rollouts.
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /aot-e2e/dotnet-install.sh
bash /aot-e2e/dotnet-install.sh \
  --version 10.0.400-preview.0.26322.102 \
  --install-dir /aot-e2e/dotnet
export PATH=/aot-e2e/dotnet:$PATH

cd /src
dotnet restore src/Azure.IIoT.OpcUa.Publisher.Module/src \
  -r linux-x64 -p:IIoTPublishAot=true \
  -s https://api.nuget.org/v3/index.json \
  --artifacts-path /aot-e2e/artifacts

dotnet publish src/Azure.IIoT.OpcUa.Publisher.Module/src \
  -c Release -r linux-x64 --self-contained --no-restore \
  -p:IIoTPublishAot=true --artifacts-path /aot-e2e/artifacts
'@
```

Then package the already-published output. **`-t:PublishContainer` replaces the
publish chain rather than extending it**, so it must be a second invocation
carrying `--no-build --no-restore`; running it in one step re-publishes without
AOT and quietly discards the native binary:

```bash
dotnet publish src/Azure.IIoT.OpcUa.Publisher.Module/src \
  -c Release -r linux-x64 --self-contained --no-build --no-restore \
  -p:IIoTPublishAot=true --artifacts-path /aot-e2e/artifacts \
  -t:PublishContainer \
  -p:ContainerArchiveOutputPath=/aot-e2e/opc-publisher-candidate.tar.gz \
  -p:ContainerImageTag=aot-e2e-candidate
```

## Proving the binary is native

Check the produced entrypoint before packaging. A native build is a ~80 MB ELF
executable; a degraded managed build leaves a small `.dll` beside a launcher:

```bash
binary=$(find /aot-e2e/artifacts/publish /aot-e2e/artifacts/bin \
  -type f -name 'Azure.IIoT.OpcUa.Publisher.Module' | head -1)
ls -lh "$binary"
readelf -h "$binary"
```

Expected:

```text
-rwxr-xr-x 1 root root 83M  Azure.IIoT.OpcUa.Publisher.Module
ELF Header:
  Class:   ELF64
  Type:    DYN (Position-Independent Executable file)
  Machine: Advanced Micro Devices X86-64
```

If `find` returns nothing — only a `.dll` of the same name — the publish
degraded to managed. The usual cause is stale incremental state, not the SDK
version and not the project files: when `bin/Release` and `obj/Release` already
hold the output of an earlier non-AOT publish, MSBuild considers the publish up
to date and simply copies it. That path exits 0 and still emits the ILC
trim/AOT roll-up warnings, so it looks green. A degraded publish directory has
~220 files including `coreclr.dll` and no native executable; a real one has
about a dozen and no managed assemblies. Delete `bin/Release` and `obj/Release`
for the Module and publish again before looking anywhere else.

## Proving the image runs

Loading an image proves nothing; a degraded image loads happily and fails on
`docker run`. Start it and read the health endpoint:

```powershell
docker load -i 'D:\buildtemp\aot-e2e\opc-publisher-candidate.tar.gz'

docker run -d --name iiot-aot-e2e-candidate `
  -p 18080:8080 `
  -e UnsecureHttpServerPort=8080 `
  -e HttpServerPort= `
  iotedge/opc-publisher:aot-e2e-candidate

Start-Sleep -Seconds 15
Invoke-WebRequest -UseBasicParsing -Uri 'http://localhost:18080/healthz'
```

Expected: `StatusCode: 200`, body `Healthy`. Then clean up:

```powershell
docker rm -f iiot-aot-e2e-candidate
docker rmi iotedge/opc-publisher:aot-e2e-candidate
Remove-Item -Recurse -Force 'D:\buildtemp\aot-e2e'
```

## Expected timings

Measured on a developer workstation, container build:

| Step | Duration |
| --- | ---: |
| `dotnet restore` | ~1.5 min |
| AOT `dotnet publish` | ~14 min |
| `-t:PublishContainer` packaging | ~10 min |

Native AOT is inherently far slower than a JIT build — it compiles and links
the whole closed world. A publish that finishes in seconds did not run AOT.

## Accepted warnings

The application itself publishes with no IL trim or AOT warnings. Three
third-party roll-ups are accepted and expected:

- `Azure.Data.SchemaRegistry`
- `Dapr.Client`
- `Irony`

Anything beyond these three is a regression. The full inventory, with owners
and expiry dates, is in
[`publisher-module-nativeaot-baseline.md`](publisher-module-nativeaot-baseline.md).
