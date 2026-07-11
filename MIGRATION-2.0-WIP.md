# UA-.NETStandard 2.0 stack migration — work in progress

This repository is mid-way through migrating OPC Publisher 3.0 from the published
`OPCFoundation.NetStandard.Opc.Ua.*` 1.5.x NuGet packages to the **UA-.NETStandard
2.0** stack vendored as the git submodule at `external/UA-.NETStandard`
(v2.0.64-preview).

The migration is **staged**. Each stage lands as a durable, whole-solution-green
commit. To keep every commit green, projects that have not yet been migrated are
**temporarily removed from `Industrial-IoT.slnx`** and re-added + migrated in a
later stage. Nothing is deleted from disk — only the solution membership changes.

## Stage 1 (this commit) — `Azure.IIoT.OpcUa` (Core) on 2.0

**Done:**

- The four `OPCFoundation.NetStandard.Opc.Ua.*` PackageReferences (`Core`,
  `Client`, `Client.ComplexTypes`, `Server`) are replaced by ProjectReferences
  into the `external/UA-.NETStandard` submodule; the OPCFoundation group in
  `Directory.Packages.props` is emptied and `Apache.Avro` is removed.
- The 6 forked codecs (`JsonEncoderEx`, `JsonDecoderEx`, `BaseAvroEncoder`,
  `BaseAvroDecoder`, `AvroEncoder`, `AvroDecoder`) and **all Avro** (source,
  schemas, PubSub Avro message types, `AvroFileWriter`, and every Avro test) are
  deleted. Avro will be re-added upstream in the 2.0 stack later and consumed then.
- The value API (`JsonVariantEncoder`, `EncodeableEx.AsJson`) is rewired onto the
  2.0 stack's own `Opc.Ua.JsonEncoder`/`Opc.Ua.JsonDecoder` using
  `JsonEncoderOptions.Compact` (reversible) / `.Verbose` (non-reversible).
- The **UADP** PubSub encoders are fully rewired onto the 2.0
  `BinaryEncoder`/`BinaryDecoder`.
- The **JSON** PubSub encoders (`JsonNetworkMessage`, `JsonDataSetMessage`,
  `MonitoredItemMessage`, `JsonMetadataMessage`, and the Avro/JSON dispatch in
  `PubSubMessage`/`EncodeableDictionary.Decode`) are reduced to minimal compiling
  stubs that throw `NotSupportedException` and are tagged `TODO(Phase 5)`. They
  will be replaced by the 2.0 stack `Opc.Ua.PubSub` implementation in Phase 5.
- Struct-redesign API deltas across the encoders are adopted as-is (2.0 made many
  OPC UA types — `NodeId`, `ExpandedNodeId`, `LocalizedText`, `QualifiedName`,
  `DataValue`, `TypeInfo`, `ExtensionObject`, `ArrayOf<T>`, `DateTimeUtc`,
  `ByteString` — value types / immutable).

**In scope this stage (build green in `Industrial-IoT.slnx`):**

- `src/Azure.IIoT.OpcUa/src` (Core of the migration)
- `src/Azure.IIoT.OpcUa.Core/src` + `tests`
- `src/Azure.IIoT.OpcUa.Publisher.Models/src` + `tests`
- `src/Azure.IIoT.OpcUa.Publisher.Sdk/src`

## Stage 2 (this commit) — `Azure.IIoT.OpcUa.Publisher` + `Module.src` + `Sdk` on 2.0

Stage 2 migrates the Publisher engine, Module host, and SDK onto the 2.0 stack and
lands them **green** in `Industrial-IoT.slnx`. The 2.0 API deltas (2.0 turned
`NodeId`, `ExpandedNodeId`, `QualifiedName`, `LocalizedText`, `DataValue`,
`StatusCode`, `ByteString` into readonly value types, and replaced every generated
`XxxCollection` with the `ArrayOf<T>` readonly struct) are fully resolved for these
projects. `Azure.IIoT.OpcUa.Publisher/src`, `Azure.IIoT.OpcUa.Publisher.Module/src`,
and `Azure.IIoT.OpcUa.Publisher.Sdk` now build with **0 errors** in the solution.

**In scope this stage (build green in `Industrial-IoT.slnx`):**

- Everything from Stage 1, plus
- `src/Azure.IIoT.OpcUa.Publisher/src` (Publisher engine)
- `src/Azure.IIoT.OpcUa.Publisher.Module/src` (ASP.NET Core IoT Edge host)
- `src/Azure.IIoT.OpcUa.Publisher.Sdk/src`

**Deferred inside the migrated code:**

- **Phase 4b**: X509 challenge-signing for `UserIdentity` (2.0 uses the
  provider-based `UserIdentity.CreateAsync(CertificateIdentifier, ...)` lazy-signing
  path). Stage 2 presents the certificate via `new UserIdentity(new X509IdentityToken
  { CertificateData = ... })` (`StackModelsEx.cs`, tagged `TODO(4b)`) — compiles and
  presents the cert but does not perform the private-key challenge signature.
  Classic sessions are kept throughout (no `ManagedSession`).
- **Phase 5**: JSON PubSub telemetry call paths that route through the
  `TODO(Phase 5)` stub encoders remain stubbed (Avro command-line options
  `--daf/--asj` removed with the Avro drop).

**Landed on disk this stage (architectural deltas):**

- `Stack/OpcUaCollectionCompat.cs` (new) — a compat shim declaring the removed
  `XxxCollection : List<Xxx>` types in `namespace Opc.Ua` (exploiting the
  `List<T>` → `ArrayOf<T>` implicit conversion for the request direction) plus a
  `NodeIdCompat.IsNull(...)` helper (the static `NodeId.IsNull` was removed in
  favour of the instance `.IsNull` property).
- `Stack/LruNodeCacheCompat.cs` (new) — a Publisher-local `ILruNodeCache` /
  `LruNodeCache` wrapper over the 2.0 `INodeCache` / `NodeCache`, re-exposing the
  old surface (`IsTypeOf`, `GetNodeAsync`, `GetSuperTypeAsync`,
  `GetBuiltInTypeAsync`, `GetReferencesAsync`, `Clear`, `Inner`). Isolates the
  node-cache churn (the 2.0 `ILruNodeCache` was removed) to one file.
- `Stack/CertificateValidationCompat.cs` (new) — a
  `CertificateValidationEventHandler` / `CertificateValidationEventArgs` compat
  pair bridging the removed `CertificateValidator` event model onto the 2.0
  `ICertificateManager.AcceptError` hook.
- `Stack/Runtime/FlatCertificateStore.cs` — migrated the 7 members to the 2.0
  async `ICertificateStore` (`Certificate` / `CertificateCollection` in
  `Opc.Ua.Security.Certificates`).
- `Stack/Services/OpcUaApplication.cs` — `Validate` event rewired onto the 2.0
  `ICertificateManager.AcceptError` hook.
- `Stack/Services/OpcUaClient.cs` / `OpcUaSession.cs` — `Create` / `CreateAsync`
  overrides + session ctor migrated to the 2.0 `SessionFactory` signatures
  (`Certificate?` / `CertificateCollection?` / `ArrayOf<EndpointDescription>` /
  `ArrayOf<string>` / `IUserIdentity?` / non-nullable `ReverseConnectManager`).
  Classic sessions kept (no `ManagedSession` — that is Phase 4b).
- `Stack/Extensions/ServiceResponseEx.cs` — added `ArrayOf<T>` overloads to the
  `Validate` extension (response `.Results` / `.DiagnosticInfos` are now
  `ArrayOf<T>`, which is not `IEnumerable<T>`).
- Bulk mechanical: `NodeId.IsNull(x)` → `NodeIdCompat.IsNull(x)` (57 files);
  `.Item1?` → `.Item1` on now-non-null `DataValue` tuples in `SessionEx`.
- Body-wave resolved: value-type `?.` removal (CS0023), `StatusCode`↔`uint`
  (CS9135/CS0266), `ArrayOf<T>` mutation via `List<T>` rebuild (CS1061),
  `ByteString` casts, `DataValue` readonly construction, `PropertyState<T>`
  abstract construction (CS0144), and the `ComplexTypeSystem` wave, all adapted
  to the 2.0 API. `LangVersion` raised to 14.0 in `common.props`.
- `Module.src`: removed the deleted-Avro `AvroWriter` config class + its DI
  registration (`Runtime/Configuration.cs`) and the `--daf/disableavrofiles`
  command-line option (`Runtime/CommandLine.cs`).

## Stage 3 / Phase 9A (this commit) — `Azure.IIoT.OpcUa.Publisher.Testing` (Servers) on 2.0

Stage 3 migrates the ~18 test OPC UA servers (the `Testing.Servers` project) from the
published 1.5 `Opc.Ua.Server` API to the 2.0 `Opc.Ua.Server` NodeManager API, landing
them **green** in `Industrial-IoT.slnx` (whole-solution build = 0 errors). The servers
keep the classic `CustomNodeManager2`/`SampleNodeManager` base (no
`AsyncCustomNodeManager`) and remain functionally equivalent — they back the
integration tests. The submodule's already-2.0 Quickstart servers
(`external/UA-.NETStandard/Applications/Quickstarts.Servers/**`) and
`Libraries/Opc.Ua.Server/**` base classes were used as the exact idiom reference.

**Done — 2.0 idiom deltas adopted across the servers:**

- Immutable readonly-struct value types (`NodeId`, `ExpandedNodeId`, `QualifiedName`,
  `LocalizedText`, `DataValue`, `ByteString`, `NumericRange`, `StatusCode`, `Variant`,
  `DateTimeUtc`, `XmlElement`): `.IsNull` is now an instance property (was a static
  method); value-type `?.` removed; `DataValue` constructed immutably (ctor + `.With*`).
- `PropertyState<T>`/`BaseDataVariableState<T>` are abstract → constructed via the 2.0
  `PropertyState<T>.Implementation<TBuilder>` factory pattern. `PropertyState<DateTime>`
  becomes `PropertyState<DateTimeUtc>` (the `VariantBuilder` covers `DateTimeUtc`).
- `ArrayOf<T>` is an immutable readonly struct (no `.Add`/`.Clear`; does not implement
  `IEnumerable<T>`) — accumulate in `List<T>`/convert via `.ToArray()`.
- `NodeState.ReadAttribute(context, attributeId, indexRange, dataEncoding, ref value)`
  (5th arg is `ref DataValue`); `ReadAttributes` returns `ArrayOf<Variant>`.
- Delegate signature changes (`GenericMethodCalledEventHandler`,
  `NodeValueEventHandler`, `ConditionAddCommentEventHandler`, method-call handlers use
  `ArrayOf<Variant>`, value handlers use `ref Variant`).
- `NodeState.SetChildValue<T>` now requires `T : IEncodeable` — plain values set via
  `Variant` instead.
- Certificate/token/telemetry/`ServerBase` deltas in `ServerFactory.cs` /
  `ServerConsoleHost.cs` (UA0021 `CertificateManager.AcceptError`;
  `CertificateIdentifierResolver.ResolveAsync`; `StopAsync`;
  `ITelemetryContext`-requiring server ctors; wire-token accessors).
- The shared `Common/SampleNodeManager.cs` + `Common/DataChangeMonitoredItem.cs`
  (which are forks of the Quickstart `SampleNodeManager`) were hand-migrated to mirror
  the 2.0 Quickstart twins.
- `MonitoredNode2` now takes an `IAsyncNodeManager` — supplied via the base
  `this.ToAsyncNodeManager()` extension (classic node manager kept).

**Simplified / deferred inside Testing.Servers:**

- `ISA95Jobs` server **descoped** (`<Compile Remove="Isa95Jobs\**\*.cs"/>` +
  `Generated\Isa95Jobs\**`, unregistered in `ServerFactory`): it is a
  NodeSet2-generated server with no source-gen path and only stale generated errors.
  It is not needed by the migrated integration tests; to be restored in a later pass.
- `ServerFactory.VerifyCertificate(x509Token)` reduced to a `// TODO(Stage final)` stub
  (server-side X509 user-token validation moved to the async `IdentityRegistry` model
  in 2.0).
- Config `ParseExtension<T>()` call sites (now constrained to `T : IEncodeable`, which
  the DataContract config classes do not implement) replaced with the `new T()`
  fallback — behaviorally equivalent for the integration tests (which use defaults).

**In scope this stage (build green in `Industrial-IoT.slnx`):**

- Everything from Stages 1–2, plus
- `src/Azure.IIoT.OpcUa.Publisher.Testing/src` (Testing.Servers, minus `ISA95Jobs`)

## Temporarily descoped from `Industrial-IoT.slnx` (to be migrated in later stages)

| Project | Reason | Target stage |
| --- | --- | --- |
| `src/Azure.IIoT.OpcUa.Publisher/tests` | depends on Testing.Servers fixtures; also references deleted forked codecs / Phase-5 stubs | Final stage |
| `src/Azure.IIoT.OpcUa.Publisher.Module/tests` | depends on Testing.Servers + Module integration | Final stage |
| `src/Azure.IIoT.OpcUa.Publisher.Module/cli` | depends on Testing.Servers | Final stage |
| `src/Azure.IIoT.OpcUa.Publisher.Testing/tests` | depends on Testing.Servers | Final stage |
| `src/Azure.IIoT.OpcUa.Publisher.Testing/cli` | depends on Testing.Servers; has an unfixed cert-validation pattern in `TestServerFactory.cs` | Final stage |
| `src/Azure.IIoT.OpcUa/tests` | `EncodeableDictionaryTests` / `JsonDataSetTests` reference the deleted forked codecs; JSON PubSub tests exercise the `TODO(Phase 5)` stubs; expected values need rewiring to the 2.0 codec output | Final stage (test rewiring) |

Note: `Testing.Servers` is now **in scope and green** (was Stage 3 target), except the
descoped `ISA95Jobs` server (see Stage 3 above).

## Constraints (unchanged across stages)

- Never modify the `external/UA-.NETStandard` submodule.
- Do not adopt `ManagedSession` (that is Phase 4b, a later pass) — keep the classic
  `Session`/`Subscription`/`MonitoredItem` APIs.
- Accept the 2.0 stack codec output as-is (behavioral compat bar; do not re-fork to
  match the old byte-for-byte output).
