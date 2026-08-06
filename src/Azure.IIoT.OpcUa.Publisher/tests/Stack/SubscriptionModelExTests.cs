// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="SubscriptionModelEx.CreateSubscriptionId"/>.
    /// </summary>
    public sealed class SubscriptionModelExTests
    {
        [Fact]
        public void CreateSubscriptionId_DefaultModel_ReturnsNonEmptyString()
        {
            var model = new SubscriptionModel();
            var id = model.CreateSubscriptionId();
            Assert.NotEmpty(id);
        }

        [Fact]
        public void CreateSubscriptionId_SameConfig_ReturnsSameId()
        {
            var model1 = new SubscriptionModel
            {
                Priority = 5,
                PublishingInterval = TimeSpan.FromMilliseconds(500)
            };
            var model2 = new SubscriptionModel
            {
                Priority = 5,
                PublishingInterval = TimeSpan.FromMilliseconds(500)
            };

            Assert.Equal(model1.CreateSubscriptionId(), model2.CreateSubscriptionId());
        }

        [Fact]
        public void CreateSubscriptionId_DifferentPriority_ReturnsDifferentId()
        {
            var model1 = new SubscriptionModel { Priority = 1 };
            var model2 = new SubscriptionModel { Priority = 2 };

            Assert.NotEqual(model1.CreateSubscriptionId(), model2.CreateSubscriptionId());
        }

        [Fact]
        public void CreateSubscriptionId_DifferentInterval_ReturnsDifferentId()
        {
            var model1 = new SubscriptionModel { PublishingInterval = TimeSpan.FromMilliseconds(100) };
            var model2 = new SubscriptionModel { PublishingInterval = TimeSpan.FromMilliseconds(200) };

            Assert.NotEqual(model1.CreateSubscriptionId(), model2.CreateSubscriptionId());
        }

        [Fact]
        public void CreateSubscriptionId_ContainsPriorityAndInterval()
        {
            var model = new SubscriptionModel
            {
                Priority = 7,
                PublishingInterval = TimeSpan.FromMilliseconds(1000)
            };

            var id = model.CreateSubscriptionId();

            // Format is "{hash}[P{priority}@{intervalMs}]"
            Assert.Contains("[P7@1000]", id, StringComparison.Ordinal);
        }
    }
}
