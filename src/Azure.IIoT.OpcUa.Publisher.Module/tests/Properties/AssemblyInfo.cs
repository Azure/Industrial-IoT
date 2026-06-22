// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#if !DEBUG
using Xunit;
// Test execution is fully serialized (MaxParallelThreads = 1): each integration-test
// fixture spins up an in-process OPC UA server, an MQTT broker and a publisher host.
// Running two collections concurrently keeps two in-process OPC UA servers active at
// once, which races on process-global OPC UA SDK state and intermittently crashes the
// native test host on newer Windows CI images (Server 2022 / 2025; Server 2019 masks
// the race). Serializing collections leaves only one server active at a time and
// removes the race. Do not raise above 1 without re-validating on a 2022/2025 image.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, MaxParallelThreads = 1)]
#endif
