// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#if !DEBUG
using Xunit;
// MaxParallelThreads is intentionally capped low: each integration-test fixture
// spins up an OPC UA server, an in-process MQTT broker and a publisher host, so
// running many in parallel peaks native handle / memory usage and crashes the
// test host on the resource-constrained CI build containers. Keep this at 2.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, MaxParallelThreads = 2)]
#endif
