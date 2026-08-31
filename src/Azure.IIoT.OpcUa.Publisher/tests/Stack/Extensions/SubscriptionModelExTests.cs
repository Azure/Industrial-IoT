// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Extensions
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
        public void CreateSubscriptionId_DefaultModel_ReturnsNonEmpty()
        {
            var model = new SubscriptionModel();
            var id = model.CreateSubscriptionId();

            Assert.NotNull(id);
            Assert.NotEmpty(id);
        }

        [Fact]
        public void CreateSubscriptionId_SameModels_ReturnsSameId()
        {
            var a = new SubscriptionModel
            {
                PublishingInterval = TimeSpan.FromMilliseconds(500),
                Priority = 10,
                KeepAliveCount = 5u
            };
            var b = new SubscriptionModel
            {
                PublishingInterval = TimeSpan.FromMilliseconds(500),
                Priority = 10,
                KeepAliveCount = 5u
            };

            Assert.Equal(a.CreateSubscriptionId(), b.CreateSubscriptionId());
        }

        [Fact]
        public void CreateSubscriptionId_DifferentPublishingInterval_ReturnsDifferentId()
        {
            var a = new SubscriptionModel { PublishingInterval = TimeSpan.FromMilliseconds(500) };
            var b = new SubscriptionModel { PublishingInterval = TimeSpan.FromMilliseconds(1000) };

            Assert.NotEqual(a.CreateSubscriptionId(), b.CreateSubscriptionId());
        }

        [Fact]
        public void CreateSubscriptionId_DifferentPriority_ReturnsDifferentId()
        {
            var a = new SubscriptionModel { Priority = 1 };
            var b = new SubscriptionModel { Priority = 100 };

            Assert.NotEqual(a.CreateSubscriptionId(), b.CreateSubscriptionId());
        }

        [Fact]
        public void CreateSubscriptionId_ContainsPriorityAndIntervalMarkers()
        {
            var model = new SubscriptionModel
            {
                Priority = 5,
                PublishingInterval = TimeSpan.FromMilliseconds(250)
            };

            var id = model.CreateSubscriptionId();

            // The id format includes "P<priority>@<milliseconds>"
            Assert.Contains("P5", id, StringComparison.Ordinal);
            Assert.Contains("@250", id, StringComparison.Ordinal);
        }

        [Fact]
        public void CreateSubscriptionId_NullPublishingInterval_UsesZero()
        {
            var model = new SubscriptionModel { PublishingInterval = null, Priority = 0 };

            var id = model.CreateSubscriptionId();

            Assert.Contains("@0", id, StringComparison.Ordinal);
        }
    }
}
