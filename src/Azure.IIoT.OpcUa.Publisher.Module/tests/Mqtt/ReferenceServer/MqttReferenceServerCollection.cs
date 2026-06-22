// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.ReferenceServer
{
    using Xunit;

    /// <summary>
    /// Groups the MQTT broker integration tests so they run serialized. The in-process
    /// MQTT broker/client is sensitive to CPU contention; at higher MaxParallelThreads the
    /// concurrent collections starve it and an RPC/telemetry wait can stall until the
    /// 10-minute blame-hang inactivity timeout aborts the whole run (observed on the Linux
    /// CI shard once parallelism was restored to 4). DisableParallelization runs these tests
    /// in xUnit's non-parallel phase - one at a time with no competing load - while the rest
    /// of the assembly keeps full parallelism.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class MqttReferenceServerCollection
    {
        public const string Name = "MqttReferenceServerIntegration";
    }
}
