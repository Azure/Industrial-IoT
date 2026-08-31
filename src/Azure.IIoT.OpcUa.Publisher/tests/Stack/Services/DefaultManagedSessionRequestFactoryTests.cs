// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License. See LICENSE in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using System;
    using Xunit;

    public sealed class DefaultManagedSessionRequestFactoryTests
    {
        [Fact]
        public void GetConnectTimeoutUsesExplicitRequestTimeoutFirst()
        {
            var options = new OpcUaClientOptions
            {
                DefaultConnectTimeoutDuration = TimeSpan.FromSeconds(2),
                DefaultServiceCallTimeoutDuration = TimeSpan.FromSeconds(3)
            };

            var actual = DefaultManagedSessionRequestFactory.GetConnectTimeout(1234,
                options);

            Assert.Equal(TimeSpan.FromMilliseconds(1234), actual);
        }

        [Fact]
        public void GetConnectTimeoutIgnoresNonPositiveExplicitTimeout()
        {
            var options = new OpcUaClientOptions
            {
                DefaultConnectTimeoutDuration = TimeSpan.FromSeconds(2)
            };

            var zero = DefaultManagedSessionRequestFactory.GetConnectTimeout(0, options);
            var negative = DefaultManagedSessionRequestFactory.GetConnectTimeout(-1,
                options);

            Assert.Equal(TimeSpan.FromSeconds(2), zero);
            Assert.Equal(TimeSpan.FromSeconds(2), negative);
        }

        [Fact]
        public void GetConnectTimeoutUsesPositiveConfiguredConnectTimeout()
        {
            var options = new OpcUaClientOptions
            {
                DefaultConnectTimeoutDuration = TimeSpan.FromSeconds(4),
                DefaultServiceCallTimeoutDuration = TimeSpan.FromSeconds(5)
            };

            var actual = DefaultManagedSessionRequestFactory.GetConnectTimeout(null,
                options);

            Assert.Equal(TimeSpan.FromSeconds(4), actual);
        }

        [Fact]
        public void GetConnectTimeoutFallsBackToServiceCallTimeout()
        {
            var options = new OpcUaClientOptions
            {
                DefaultConnectTimeoutDuration = TimeSpan.Zero,
                DefaultServiceCallTimeoutDuration = TimeSpan.FromSeconds(6)
            };

            var actual = DefaultManagedSessionRequestFactory.GetConnectTimeout(null,
                options);

            Assert.Equal(TimeSpan.FromSeconds(6), actual);
        }

        [Fact]
        public void GetConnectTimeoutUsesOneMinuteDefaultLast()
        {
            var options = new OpcUaClientOptions
            {
                DefaultConnectTimeoutDuration = TimeSpan.FromSeconds(-1),
                DefaultServiceCallTimeoutDuration = TimeSpan.Zero
            };

            var actual = DefaultManagedSessionRequestFactory.GetConnectTimeout(null,
                options);

            Assert.Equal(TimeSpan.FromMinutes(1), actual);
        }
    }
}
