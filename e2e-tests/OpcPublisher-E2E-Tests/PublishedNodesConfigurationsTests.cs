// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests
{
    using Newtonsoft.Json.Linq;
    using Xunit;

    public sealed class PublishedNodesConfigurationsTests
    {
        [Fact]
        public void SimpleEventFilterUsesRequestedQueueSize()
        {
            var filter = TestConstants.PublishedNodesConfigurations
                .SimpleEventFilter(queueSize: 1);

            var node = Assert.IsType<JObject>(Assert.Single(filter));
            Assert.Equal(1u, (uint)node["QueueSize"]);
        }
    }
}
