// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Xunit;

    /// <summary>
    /// Tests for <see cref="OpcUaSubscription"/> helpers. The static helper
    /// <see cref="OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription"/>
    /// encodes the contract documented on
    /// <c>OpcUaSubscriptionOptions.MaxMonitoredItemPerSubscription</c> and
    /// is the root-cause fix for issue #2445 (server reporting
    /// <c>uint.MaxValue</c> caused a "Cannot create 0 or negative size
    /// batches" failure inside partitioning).
    /// </summary>
    public class OpcUaSubscriptionTests
    {
        private const int kDefault = 65536;

        [Fact]
        public void ServerNullAndNoOverrideReturnsDefault()
        {
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: null, configuredLimit: null, defaultLimit: kDefault);
            Assert.Equal(kDefault, result);
        }

        [Fact]
        public void ServerZeroAndNoOverrideReturnsDefault()
        {
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: 0u, configuredLimit: null, defaultLimit: kDefault);
            Assert.Equal(kDefault, result);
        }

        [Fact]
        public void ServerZeroAndOverrideReturnsOverride()
        {
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: 0u, configuredLimit: 1000u, defaultLimit: kDefault);
            Assert.Equal(1000, result);
        }

        [Fact]
        public void ServerLimitAndNoOverrideReturnsServerLimit()
        {
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: 500u, configuredLimit: null, defaultLimit: kDefault);
            Assert.Equal(500, result);
        }

        [Fact]
        public void OverrideLargerThanServerLimitDoesNotApply()
        {
            // Per option docstring: "If the server supports less, this value takes no effect."
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: 100u, configuredLimit: 1000u, defaultLimit: kDefault);
            Assert.Equal(100, result);
        }

        [Fact]
        public void OverrideSmallerThanServerLimitCapsServerValue()
        {
            // The override is a user-imposed cap; when smaller, it wins.
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: 1000u, configuredLimit: 50u, defaultLimit: kDefault);
            Assert.Equal(50, result);
        }

        [Fact]
        public void ConfiguredZeroIsTreatedAsUnset()
        {
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: 200u, configuredLimit: 0u, defaultLimit: kDefault);
            Assert.Equal(200, result);
        }

        [Fact]
        public void ServerZeroAndConfiguredZeroReturnsDefault()
        {
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: 0u, configuredLimit: 0u, defaultLimit: kDefault);
            Assert.Equal(kDefault, result);
        }

        [Fact]
        public void ServerUintMaxValueAndNoOverrideFallsBackToDefault()
        {
            // Regression test for issue #2445. Previously the uint.MaxValue
            // server value propagated to (int) cast as -1, throwing
            // "Cannot create 0 or negative size batches" downstream.
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: uint.MaxValue, configuredLimit: null, defaultLimit: kDefault);
            Assert.Equal(kDefault, result);
        }

        [Fact]
        public void ServerUintMaxValueAndOverrideAppliesOverride()
        {
            // Regression test for issue #2445. Customer-reported scenario:
            // server returns uint.MaxValue, user-configured override must apply.
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: uint.MaxValue, configuredLimit: 1000u, defaultLimit: kDefault);
            Assert.Equal(1000, result);
        }

        [Fact]
        public void ServerUintMaxValueAndOverrideUintMaxValueFallsBackToDefault()
        {
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: uint.MaxValue, configuredLimit: uint.MaxValue,
                defaultLimit: kDefault);
            Assert.Equal(kDefault, result);
        }

        [Fact]
        public void ServerValueAtIntMaxValueIsAccepted()
        {
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: int.MaxValue, configuredLimit: null, defaultLimit: kDefault);
            Assert.Equal(int.MaxValue, result);
        }

        [Fact]
        public void ServerValueAboveIntMaxValueIsTreatedAsUnset()
        {
            // Values that cannot be safely cast to int are treated as
            // "no usable server limit"; the configured override or default applies.
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                serverLimit: (uint)int.MaxValue + 1u, configuredLimit: 1000u,
                defaultLimit: kDefault);
            Assert.Equal(1000, result);
        }

        [Theory]
        [InlineData(null, null, kDefault)]
        [InlineData(0u, null, kDefault)]
        [InlineData(uint.MaxValue, null, kDefault)]
        [InlineData(uint.MaxValue, uint.MaxValue, kDefault)]
        [InlineData(uint.MaxValue, 1000u, 1000)]
        [InlineData(1000u, 50u, 50)]
        [InlineData(50u, 1000u, 50)]
        public void ResultIsAlwaysPositiveAndInIntRange(uint? server, uint? configured, int expected)
        {
            var result = OpcUaSubscription.ComputeMaxMonitoredItemsPerSubscription(
                server, configured, kDefault);
            Assert.InRange(result, 1, int.MaxValue);
            Assert.Equal(expected, result);
        }
    }
}
