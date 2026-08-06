// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="PublisherConfig.PostConfigure"/> covering all
    /// configuration branches. Calls <see cref="PublisherConfig.ToOptions"/>
    /// which in turn calls <see cref="PublisherConfig.PostConfigure"/> so the
    /// source-generated binder runs first, then the post-configure logic.
    /// </summary>
    public sealed class PublisherConfigTests
    {
        // ─── helpers ────────────────────────────────────────────────────────────

        private static IConfigurationRoot Configuration(
                params (string key, string? value)[] pairs)
            => new ConfigurationBuilder()
                .AddInMemoryCollection(pairs
                    .Select(p => new KeyValuePair<string, string?>(p.key, p.value)))
                .Build();

        private static (string key, string? value) P(string key, string? value)
            => (key, value);

        // ─── PublisherId ──────────────────────────────────────────────────────

        [Fact]
        public void Config_PublisherId_WhenSet_PreservesValue()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.PublisherIdKey, "my-publisher")))
                .ToOptions().Value;

            Assert.Equal("my-publisher", options.PublisherId);
        }

        [Fact]
        public void Config_PublisherId_WhenAbsent_ReturnsNonEmpty()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            // Default is DNS hostname – must be a non-empty string
            Assert.NotNull(options.PublisherId);
            Assert.NotEmpty(options.PublisherId);
        }

        // ─── SiteId ───────────────────────────────────────────────────────────

        [Fact]
        public void Config_SiteId_WhenSet_PreservesValue()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.SiteIdKey, "site-1")))
                .ToOptions().Value;

            Assert.Equal("site-1", options.SiteId);
        }

        [Fact]
        public void Config_SiteId_WhenAbsent_IsNull()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.SiteId);
        }

        // ─── UseNativePubSub ──────────────────────────────────────────────────

        [Fact]
        public void Config_UseNativePubSub_DefaultsToTrue()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.True(options.UseNativePubSub);
        }

        [Fact]
        public void Config_UseNativePubSub_CanBeDisabled()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.UseNativePubSubKey, "false")))
                .ToOptions().Value;

            Assert.False(options.UseNativePubSub);
        }

        // ─── UseStandardsCompliantEncoding ────────────────────────────────────

        [Fact]
        public void Config_UseStandardsCompliantEncoding_DefaultsFalse()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.False(options.UseStandardsCompliantEncoding);
        }

        [Fact]
        public void Config_UseStandardsCompliantEncoding_CanBeEnabled()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.UseStandardsCompliantEncodingKey, "true")))
                .ToOptions().Value;

            Assert.True(options.UseStandardsCompliantEncoding);
        }

        // ─── MessagingProfile defaults ────────────────────────────────────────

        [Fact]
        public void Config_MessagingProfile_DefaultIsPubSubJson()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.NotNull(options.MessagingProfile);
            Assert.Equal(MessagingMode.PubSub, options.MessagingProfile!.MessagingMode);
            Assert.Equal(MessageEncoding.Json, options.MessagingProfile.MessageEncoding);
        }

        [Fact]
        public void Config_MessageEncoding_UadpProducesUadpProfile()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.MessageEncodingKey, "Uadp")))
                .ToOptions().Value;

            Assert.Equal(MessageEncoding.Uadp, options.MessagingProfile!.MessageEncoding);
        }

        [Fact]
        public void Config_FullFeaturedMessage_UpgradesPubSubToFullNetworkMessages()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.FullFeaturedMessageKey, "true")))
                .ToOptions().Value;

            Assert.Equal(MessagingMode.FullNetworkMessages,
                options.MessagingProfile!.MessagingMode);
        }

        [Fact]
        public void Config_FullFeaturedMessage_DoesNotUpgradeNonPubSubMode()
        {
            // FullFeaturedMessage only acts when MessagingMode == PubSub
            var options = new PublisherConfig(
                Configuration(
                    P(PublisherConfig.MessagingModeKey, "DataSetMessages"),
                    P(PublisherConfig.FullFeaturedMessageKey, "true")))
                .ToOptions().Value;

            Assert.Equal(MessagingMode.DataSetMessages,
                options.MessagingProfile!.MessagingMode);
        }

        // ─── BatchSize ────────────────────────────────────────────────────────

        [Fact]
        public void Config_BatchSize_DefaultsTo50_WhenNonStrictAndNoTransport()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.BatchSizeLegacyDefault, options.BatchSize);
        }

        [Fact]
        public void Config_BatchSize_DefaultsToZero_WhenStrictEncoding()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.UseStandardsCompliantEncodingKey, "true")))
                .ToOptions().Value;

            Assert.Equal(0, options.BatchSize);
        }

        [Fact]
        public void Config_BatchSize_ExplicitValueIsUsed()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.BatchSizeKey, "100")))
                .ToOptions().Value;

            Assert.Equal(100, options.BatchSize);
        }

        [Fact]
        public void Config_BatchSize_DefaultsToZero_WhenDefaultTransportSet()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultTransportKey, "Mqtt")))
                .ToOptions().Value;

            Assert.Equal(0, options.BatchSize);
        }

        // ─── BatchTriggerInterval ─────────────────────────────────────────────

        [Fact]
        public void Config_BatchTriggerInterval_DefaultsTo10Seconds_WhenNonStrictAndNoTransport()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(TimeSpan.FromMilliseconds(
                PublisherConfig.BatchTriggerIntervalLLegacyDefaultMillis),
                options.BatchTriggerInterval);
        }

        [Fact]
        public void Config_BatchTriggerInterval_DefaultsToZero_WhenStrictEncoding()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.UseStandardsCompliantEncodingKey, "true")))
                .ToOptions().Value;

            Assert.Equal(TimeSpan.Zero, options.BatchTriggerInterval);
        }

        [Fact]
        public void Config_BatchTriggerInterval_ExplicitDurationValue()
        {
            // TimeSpan format: "00:00:05" = 5 seconds
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.BatchTriggerIntervalKey, "00:00:05")))
                .ToOptions().Value;

            Assert.Equal(TimeSpan.FromSeconds(5), options.BatchTriggerInterval);
        }

        [Fact]
        public void Config_BatchTriggerInterval_DefaultsToZero_WhenDefaultTransportSet()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultTransportKey, "Mqtt")))
                .ToOptions().Value;

            Assert.Equal(TimeSpan.Zero, options.BatchTriggerInterval);
        }

        // ─── MaxNodesPerDataSet ───────────────────────────────────────────────

        [Fact]
        public void Config_MaxNodesPerDataSet_DefaultIs1000()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.MaxNodesPerDataSetDefault, options.MaxNodesPerDataSet);
        }

        [Fact]
        public void Config_MaxNodesPerDataSet_CanBeOverridden()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.MaxNodesPerDataSetKey, "500")))
                .ToOptions().Value;

            Assert.Equal(500, options.MaxNodesPerDataSet);
        }

        // ─── DiagnosticsInterval ─────────────────────────────────────────────

        [Fact]
        public void Config_DiagnosticsInterval_DefaultIs60Seconds()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(TimeSpan.FromMilliseconds(
                PublisherConfig.DiagnosticsIntervalDefaultMillis),
                options.DiagnosticsInterval);
        }

        [Fact]
        public void Config_DiagnosticsInterval_CanBeOverriddenWithDuration()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DiagnosticsIntervalKey, "00:02:00")))
                .ToOptions().Value;

            Assert.Equal(TimeSpan.FromMinutes(2), options.DiagnosticsInterval);
        }

        // ─── DiagnosticsTarget ────────────────────────────────────────────────

        [Fact]
        public void Config_DiagnosticsTarget_DefaultIsLogger()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherDiagnosticTargetType.Logger, options.DiagnosticsTarget);
        }

        [Theory]
        [InlineData("Logger", PublisherDiagnosticTargetType.Logger)]
        [InlineData("Events", PublisherDiagnosticTargetType.Events)]
        public void Config_DiagnosticsTarget_ParsesValidValues(
            string value, PublisherDiagnosticTargetType expected)
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DiagnosticsTargetKey, value)))
                .ToOptions().Value;

            Assert.Equal(expected, options.DiagnosticsTarget);
        }

        [Fact]
        public void Config_DiagnosticsTarget_InvalidValueDefaultsToLogger()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DiagnosticsTargetKey, "Unknown")))
                .ToOptions().Value;

            Assert.Equal(PublisherDiagnosticTargetType.Logger, options.DiagnosticsTarget);
        }

        // ─── EnableRuntimeStateReporting ─────────────────────────────────────

        [Fact]
        public void Config_EnableRuntimeStateReporting_DefaultIsFalse()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.EnableRuntimeStateReportingDefault,
                options.EnableRuntimeStateReporting);
        }

        [Fact]
        public void Config_EnableRuntimeStateReporting_CanBeEnabled()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.EnableRuntimeStateReportingKey, "true")))
                .ToOptions().Value;

            Assert.True(options.EnableRuntimeStateReporting);
        }

        // ─── RuntimeStateRoutingInfo ──────────────────────────────────────────

        [Fact]
        public void Config_RuntimeStateRoutingInfo_DefaultIsRuntimeInfo()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.RuntimeStateRoutingInfoDefault,
                options.RuntimeStateRoutingInfo);
        }

        [Fact]
        public void Config_RuntimeStateRoutingInfo_CanBeOverridden()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.RuntimeStateRoutingInfoKey, "custom-routing")))
                .ToOptions().Value;

            Assert.Equal("custom-routing", options.RuntimeStateRoutingInfo);
        }

        // ─── EnableDataSetRoutingInfo ─────────────────────────────────────────

        [Fact]
        public void Config_EnableDataSetRoutingInfo_DefaultIsFalse()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.EnableDataSetRoutingInfoDefault,
                options.EnableDataSetRoutingInfo);
        }

        [Fact]
        public void Config_EnableDataSetRoutingInfo_CanBeEnabled()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.EnableDataSetRoutingInfoKey, "true")))
                .ToOptions().Value;

            Assert.True(options.EnableDataSetRoutingInfo);
        }

        // ─── EnableCloudEvents ────────────────────────────────────────────────

        [Fact]
        public void Config_EnableCloudEvents_DefaultIsFalse()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.EnableCloudEventsDefault,
                options.EnableCloudEvents);
        }

        // ─── TopicTemplates ───────────────────────────────────────────────────

        [Fact]
        public void Config_TopicTemplates_Root_HasDefaultValue()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.RootTopicTemplateDefault,
                options.TopicTemplates.Root);
        }

        [Fact]
        public void Config_TopicTemplates_Telemetry_HasDefaultValue()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.TelemetryTopicTemplateDefault,
                options.TopicTemplates.Telemetry);
        }

        [Fact]
        public void Config_TopicTemplates_Method_HasDefaultValue()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.MethodTopicTemplateDefault,
                options.TopicTemplates.Method);
        }

        [Fact]
        public void Config_TopicTemplates_Events_HasDefaultValue()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.EventsTopicTemplateDefault,
                options.TopicTemplates.Events);
        }

        [Fact]
        public void Config_TopicTemplates_Diagnostics_HasDefaultValue()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.DiagnosticsTopicTemplateDefault,
                options.TopicTemplates.Diagnostics);
        }

        [Fact]
        public void Config_TopicTemplates_Schema_HasDefaultValue()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.SchemaTopicTemplateDefault,
                options.TopicTemplates.Schema);
        }

        [Fact]
        public void Config_TopicTemplates_DataSetMetaData_IsNullByDefault()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.TopicTemplates.DataSetMetaData);
        }

        [Fact]
        public void Config_TopicTemplates_Root_CanBeOverridden()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.RootTopicTemplateKey, "my-root")))
                .ToOptions().Value;

            Assert.Equal("my-root", options.TopicTemplates.Root);
        }

        [Fact]
        public void Config_TopicTemplates_AioConnector_UsesClusterRootTemplate()
        {
            // IsAzureIoTOperationsConnector = true triggers the cluster root topic template
            var options = new PublisherConfig(
                Configuration(
                    P(nameof(PublisherOptions.IsAzureIoTOperationsConnector), "true")))
                .ToOptions().Value;

            Assert.Equal(PublisherConfig.RootTopicTemplateCluster,
                options.TopicTemplates.Root);
        }

        // ─── SchemaOptions ────────────────────────────────────────────────────

        [Fact]
        public void Config_SchemaOptions_NullByDefault()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.SchemaOptions);
        }

        [Fact]
        public void Config_SchemaOptions_CreatedWhenPublishMessageSchemaTrue()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.PublishMessageSchemaKey, "true")))
                .ToOptions().Value;

            Assert.NotNull(options.SchemaOptions);
        }

        [Fact]
        public void Config_SchemaOptions_CreatedWhenSchemaNamespaceSet()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.SchemaNamespaceKey, "my.schema")))
                .ToOptions().Value;

            Assert.NotNull(options.SchemaOptions);
            Assert.Equal("my.schema", options.SchemaOptions!.Namespace);
        }

        [Fact]
        public void Config_SchemaOptions_PreferAvroOverJsonSchema_CanBeSet()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.PreferAvroOverJsonSchemaKey, "true")))
                .ToOptions().Value;

            Assert.NotNull(options.SchemaOptions);
            Assert.True(options.SchemaOptions!.PreferAvroOverJsonSchema);
        }

        // ─── DisableDataSetMetaData ───────────────────────────────────────────

        [Fact]
        public void Config_DisableDataSetMetaData_TrueByDefault_WhenNonStrictEncoding()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.True(options.DisableDataSetMetaData);
        }

        [Fact]
        public void Config_DisableDataSetMetaData_FalseByDefault_WhenStrictEncoding()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.UseStandardsCompliantEncodingKey, "true")))
                .ToOptions().Value;

            Assert.False(options.DisableDataSetMetaData);
        }

        [Fact]
        public void Config_DisableComplexTypeSystem_SetsDisableDataSetMetaData()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DisableComplexTypeSystemKey, "true")))
                .ToOptions().Value;

            // DisableComplexTypeSystem → DisableDataSetMetaData
            Assert.True(options.DisableDataSetMetaData);
            Assert.True(options.DisableComplexTypeSystem);
        }

        [Fact]
        public void Config_SchemaOptionsPresent_ForcesComplexTypeSystemEnabled()
        {
            // When schema options are present, DisableComplexTypeSystem is forced to false
            // even if DisableDataSetMetaData defaulted to true (non-strict mode)
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.PublishMessageSchemaKey, "true")))
                .ToOptions().Value;

            Assert.NotNull(options.SchemaOptions);
            Assert.False(options.DisableComplexTypeSystem);
        }

        // ─── DebugLogNotifications ────────────────────────────────────────────

        [Fact]
        public void Config_DebugLogNotifications_FalseByDefault()
        {
            // GetBoolOrDefault without a fallback returns false
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.False(options.DebugLogNotifications);
        }

        [Fact]
        public void Config_DebugLogNotificationsFilter_SetThroughConfig()
        {
            // When set via the config key, the filter value is preserved
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DebugLogNotificationsFilterKey, "my-filter")))
                .ToOptions().Value;

            Assert.Equal("my-filter", options.DebugLogNotificationsFilter);
        }

        [Fact]
        public void Config_DebugLogNotificationsFilter_PostConfigure_ImplicitlyEnablesDebugLog()
        {
            // The implicit enable only fires inside PostConfigure when the filter
            // was not already set by Bind() — exercise it via direct PostConfigure call.
            var config = new PublisherConfig(Configuration());
            var opts = new PublisherOptions();
            config.PostConfigure(null, opts);

            // No filter set — DebugLogNotifications stays at GetBoolOrDefault (false).
            Assert.False(opts.DebugLogNotifications);

            // Now verify the implicit-enable branch: call PostConfigure with a fresh
            // opts that has null filter but the config provides one.
            var config2 = new PublisherConfig(
                Configuration(P(PublisherConfig.DebugLogNotificationsFilterKey, "filter")));
            var opts2 = new PublisherOptions();  // filter is null → PostConfigure branch fires
            config2.PostConfigure(null, opts2);

            Assert.Equal("filter", opts2.DebugLogNotificationsFilter);
            Assert.True(opts2.DebugLogNotifications);
        }

        [Fact]
        public void Config_DebugLogNotifications_CanBeExplicitlyEnabled()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DebugLogNotificationsKey, "true")))
                .ToOptions().Value;

            Assert.True(options.DebugLogNotifications);
        }

        // ─── DefaultQualityOfService ──────────────────────────────────────────

        [Fact]
        public void Config_DefaultQualityOfService_DefaultIsAtLeastOnce()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(QoS.AtLeastOnce, options.DefaultQualityOfService);
        }

        [Theory]
        [InlineData("AtLeastOnce", QoS.AtLeastOnce)]
        [InlineData("AtMostOnce", QoS.AtMostOnce)]
        [InlineData("ExactlyOnce", QoS.ExactlyOnce)]
        public void Config_DefaultQualityOfService_CanBeOverridden(string value, QoS expected)
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultQualityOfServiceKey, value)))
                .ToOptions().Value;

            Assert.Equal(expected, options.DefaultQualityOfService);
        }

        [Fact]
        public void Config_DefaultQualityOfService_InvalidValueFallsBackToAtLeastOnce()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultQualityOfServiceKey, "UnknownQoS")))
                .ToOptions().Value;

            Assert.Equal(QoS.AtLeastOnce, options.DefaultQualityOfService);
        }

        // ─── MessageTimestamp ─────────────────────────────────────────────────

        [Fact]
        public void Config_MessageTimestamp_DefaultIsCurrentTimeUtc()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(MessageTimestamp.CurrentTimeUtc, options.MessageTimestamp);
        }

        [Theory]
        [InlineData("CurrentTimeUtc", MessageTimestamp.CurrentTimeUtc)]
        [InlineData("PublishTime", MessageTimestamp.PublishTime)]
        [InlineData("EncodingTimeUtc", MessageTimestamp.EncodingTimeUtc)]
        public void Config_MessageTimestamp_CanBeOverridden(
            string value, MessageTimestamp expected)
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.MessageTimestampKey, value)))
                .ToOptions().Value;

            Assert.Equal(expected, options.MessageTimestamp);
        }

        [Fact]
        public void Config_MessageTimestamp_InvalidValueFallsBackToCurrentTimeUtc()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.MessageTimestampKey, "InvalidTimestamp")))
                .ToOptions().Value;

            Assert.Equal(MessageTimestamp.CurrentTimeUtc, options.MessageTimestamp);
        }

        // ─── DefaultNamespaceFormat ───────────────────────────────────────────

        [Fact]
        public void Config_DefaultNamespaceFormat_DefaultsToUri_WhenNonStrictEncoding()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(NamespaceFormat.Uri, options.DefaultNamespaceFormat);
        }

        [Fact]
        public void Config_DefaultNamespaceFormat_DefaultsToExpanded_WhenStrictEncoding()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.UseStandardsCompliantEncodingKey, "true")))
                .ToOptions().Value;

            Assert.Equal(NamespaceFormat.Expanded, options.DefaultNamespaceFormat);
        }

        [Theory]
        [InlineData("Uri", NamespaceFormat.Uri)]
        [InlineData("Expanded", NamespaceFormat.Expanded)]
        [InlineData("Index", NamespaceFormat.Index)]
        public void Config_DefaultNamespaceFormat_CanBeOverridden(
            string value, NamespaceFormat expected)
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultNamespaceFormatKey, value)))
                .ToOptions().Value;

            Assert.Equal(expected, options.DefaultNamespaceFormat);
        }

        [Fact]
        public void Config_DefaultNamespaceFormat_InvalidValueFallsBackToDefault()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultNamespaceFormatKey, "BadFormat")))
                .ToOptions().Value;

            // Invalid → falls back to the UseStandardsCompliantEncoding-based default
            Assert.Equal(NamespaceFormat.Uri, options.DefaultNamespaceFormat);
        }

        // ─── DefaultTransport ─────────────────────────────────────────────────

        [Fact]
        public void Config_DefaultTransport_NullByDefault()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.DefaultTransport);
        }

        [Theory]
        [InlineData("Mqtt", WriterGroupTransport.Mqtt)]
        [InlineData("Dapr", WriterGroupTransport.Dapr)]
        [InlineData("Http", WriterGroupTransport.Http)]
        public void Config_DefaultTransport_ParsesValidValues(
            string value, WriterGroupTransport expected)
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultTransportKey, value)))
                .ToOptions().Value;

            Assert.Equal(expected, options.DefaultTransport);
        }

        [Fact]
        public void Config_DefaultTransport_InvalidValueKeepsNull()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultTransportKey, "NoSuchTransport")))
                .ToOptions().Value;

            Assert.Null(options.DefaultTransport);
        }

        // ─── ScaleTestCount ───────────────────────────────────────────────────

        [Fact]
        public void Config_ScaleTestCount_DefaultIs1()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.ScaleTestCountDefault, options.ScaleTestCount);
        }

        [Fact]
        public void Config_ScaleTestCount_CanBeOverridden()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.ScaleTestCountKey, "10")))
                .ToOptions().Value;

            Assert.Equal(10, options.ScaleTestCount);
        }

        // ─── MaxNetworkMessageSendQueueSize ───────────────────────────────────

        [Fact]
        public void Config_MaxNetworkMessageSendQueueSize_DefaultIs4096()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.MaxNetworkMessageSendQueueSizeDefault,
                options.MaxNetworkMessageSendQueueSize);
        }

        [Fact]
        public void Config_MaxNetworkMessageSendQueueSize_CanBeOverridden()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.MaxNetworkMessageSendQueueSizeKey, "8192")))
                .ToOptions().Value;

            Assert.Equal(8192, options.MaxNetworkMessageSendQueueSize);
        }

        // ─── DisableSessionPerWriterGroup ─────────────────────────────────────

        [Fact]
        public void Config_DisableSessionPerWriterGroup_DefaultIsFalse()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.DisableSessionPerWriterGroupDefault,
                options.DisableSessionPerWriterGroup);
        }

        [Fact]
        public void Config_DisableSessionPerWriterGroup_CanBeEnabled()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DisableSessionPerWriterGroupKey, "true")))
                .ToOptions().Value;

            Assert.True(options.DisableSessionPerWriterGroup);
        }

        // ─── IgnoreConfiguredPublishingIntervals ──────────────────────────────

        [Fact]
        public void Config_IgnoreConfiguredPublishingIntervals_DefaultIsFalse()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Equal(PublisherConfig.IgnoreConfiguredPublishingIntervalsDefault,
                options.IgnoreConfiguredPublishingIntervals);
        }

        // ─── EnableDataSetKeepAlives / DefaultKeyFrameCount ───────────────────

        [Fact]
        public void Config_EnableDataSetKeepAlives_FalseByDefault()
        {
            // GetBoolOrDefault without fallback returns false
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.False(options.EnableDataSetKeepAlives);
        }

        [Fact]
        public void Config_EnableDataSetKeepAlives_CanBeEnabled()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.EnableDataSetKeepAlivesKey, "true")))
                .ToOptions().Value;

            Assert.True(options.EnableDataSetKeepAlives);
        }

        [Fact]
        public void Config_DefaultKeyFrameCount_NullByDefault()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.DefaultKeyFrameCount);
        }

        [Fact]
        public void Config_DefaultKeyFrameCount_CanBeSet()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultKeyFrameCountKey, "5")))
                .ToOptions().Value;

            Assert.Equal(5u, options.DefaultKeyFrameCount);
        }

        // ─── DefaultDataSetRouting ────────────────────────────────────────────

        [Fact]
        public void Config_DefaultDataSetRouting_NullByDefault()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.DefaultDataSetRouting);
        }

        [Fact]
        public void Config_DefaultDataSetRouting_CanBeSet()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultDataSetRoutingKey, "UseBrowseNames")))
                .ToOptions().Value;

            Assert.Equal(DataSetRoutingMode.UseBrowseNames, options.DefaultDataSetRouting);
        }

        [Fact]
        public void Config_DefaultDataSetRouting_InvalidValueKeepsNull()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultDataSetRoutingKey, "NoSuchMode")))
                .ToOptions().Value;

            Assert.Null(options.DefaultDataSetRouting);
        }

        // ─── DefaultMessageTimeToLive ─────────────────────────────────────────

        [Fact]
        public void Config_DefaultMessageTimeToLive_NullByDefault()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.DefaultMessageTimeToLive);
        }

        [Fact]
        public void Config_DefaultMessageTimeToLive_CanBeSetAsDuration()
        {
            // Duration strings in TimeSpan format ("hh:mm:ss") parse correctly via Bind()
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultMessageTimeToLiveKey, "00:00:05")))
                .ToOptions().Value;

            Assert.Equal(TimeSpan.FromSeconds(5), options.DefaultMessageTimeToLive);
        }

        [Fact]
        public void Config_DefaultMessageTimeToLive_PostConfigure_IntAsMilliseconds()
        {
            // When options.DefaultMessageTimeToLive is null and the config key has an int,
            // PostConfigure converts it as milliseconds (branch not reachable via ToOptions()
            // because Bind() always calls GetDurationOrNull first).
            var config = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultMessageTimeToLiveKey, "5000")));
            var opts = new PublisherOptions(); // DefaultMessageTimeToLive is null
            config.PostConfigure(null, opts);

            Assert.Equal(TimeSpan.FromMilliseconds(5000), opts.DefaultMessageTimeToLive);
        }

        // ─── DefaultMaxDataSetMessagesPerPublish ──────────────────────────────

        [Fact]
        public void Config_DefaultMaxDataSetMessagesPerPublish_NullByDefault()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.DefaultMaxDataSetMessagesPerPublish);
        }

        [Fact]
        public void Config_DefaultMaxDataSetMessagesPerPublish_CanBeSet()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultMaxMessagesPerPublishKey, "10")))
                .ToOptions().Value;

            Assert.Equal(10u, options.DefaultMaxDataSetMessagesPerPublish);
        }

        // ─── Static properties ────────────────────────────────────────────────

        [Fact]
        public void Config_Version_IsNotNullOrEmpty()
        {
            Assert.NotNull(PublisherConfig.Version);
            Assert.NotEmpty(PublisherConfig.Version);
        }

        [Fact]
        public void Config_IsContainer_IsBoolValue()
        {
            // Just verify the property is accessible and returns a bool
            _ = PublisherConfig.IsContainer;
        }

        [Fact]
        public void Config_IsRunningAsRoot_IsBoolValue()
        {
            _ = PublisherConfig.IsRunningAsRoot;
        }

        // ─── ForceCredentialEncryption ────────────────────────────────────────

        [Fact]
        public void Config_ForceCredentialEncryption_FalseByDefault()
        {
            // GetBoolOrDefault without fallback returns false
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.False(options.ForceCredentialEncryption);
        }

        [Fact]
        public void Config_ForceCredentialEncryption_CanBeEnabled()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.ForceCredentialEncryptionKey, "true")))
                .ToOptions().Value;

            Assert.True(options.ForceCredentialEncryption);
        }

        // ─── ApiKeyOverride ───────────────────────────────────────────────────

        [Fact]
        public void Config_ApiKeyOverride_NullByDefault()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.ApiKeyOverride);
        }

        [Fact]
        public void Config_ApiKeyOverride_CanBeSet()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.ApiKeyOverrideKey, "secret-key")))
                .ToOptions().Value;

            Assert.Equal("secret-key", options.ApiKeyOverride);
        }

        // ─── DefaultMetaDataUpdateTime / AsyncMetaDataLoadTimeout ────────────

        [Fact]
        public void Config_DefaultMetaDataUpdateTime_NullByDefault_WhenNonStrict()
        {
            // Non-strict: metadata is disabled, so DefaultMetaDataUpdateTime stays null
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.DefaultMetaDataUpdateTime);
        }

        [Fact]
        public void Config_AsyncMetaDataLoadTimeout_DefaultWhenMetaDataEnabled()
        {
            // Strict encoding enables metadata
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.UseStandardsCompliantEncodingKey, "true")))
                .ToOptions().Value;

            Assert.Equal(TimeSpan.FromMilliseconds(
                PublisherConfig.AsyncMetaDataLoadTimeoutDefaultMillis),
                options.AsyncMetaDataLoadTimeout);
        }

        // ─── WriteValueWhenDataSetHasSingleEntry ──────────────────────────────

        [Fact]
        public void Config_WriteValueWhenDataSetHasSingleEntry_NullByDefault()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.WriteValueWhenDataSetHasSingleEntry);
        }

        [Fact]
        public void Config_WriteValueWhenDataSetHasSingleEntry_CanBeEnabled()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.WriteValueWhenDataSetHasSingleEntryKey, "true")))
                .ToOptions().Value;

            Assert.True(options.WriteValueWhenDataSetHasSingleEntry);
        }

        // ─── DefaultUseReverseConnect ─────────────────────────────────────────

        [Fact]
        public void Config_DefaultUseReverseConnect_NullByDefault()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.DefaultUseReverseConnect);
        }

        [Fact]
        public void Config_DefaultUseReverseConnect_CanBeEnabled()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DefaultUseReverseConnectKey, "true")))
                .ToOptions().Value;

            Assert.True(options.DefaultUseReverseConnect);
        }

        // ─── DisableSubscriptionTransfer ──────────────────────────────────────

        [Fact]
        public void Config_DisableSubscriptionTransfer_NullByDefault()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.DisableSubscriptionTransfer);
        }

        [Fact]
        public void Config_DisableSubscriptionTransfer_CanBeEnabled()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.DisableSubscriptionTransferKey, "true")))
                .ToOptions().Value;

            Assert.True(options.DisableSubscriptionTransfer);
        }

        // ─── PublishedNodesFile ────────────────────────────────────────────────

        [Fact]
        public void Config_PublishedNodesFile_NullByDefault()
        {
            var options = new PublisherConfig(Configuration()).ToOptions().Value;

            Assert.Null(options.PublishedNodesFile);
        }

        [Fact]
        public void Config_PublishedNodesFile_CanBeSet()
        {
            var options = new PublisherConfig(
                Configuration(P(PublisherConfig.PublishedNodesFileKey,
                    PublisherConfig.PublishedNodesFileDefault)))
                .ToOptions().Value;

            Assert.Equal(PublisherConfig.PublishedNodesFileDefault, options.PublishedNodesFile);
        }

        // ─── Interaction: strict encoding + metadata fields ────────────────────

        [Fact]
        public void Config_StrictEncoding_AllDefaultsAreConsistent()
        {
            var options = new PublisherConfig(
                Configuration(
                    P(PublisherConfig.PublisherIdKey, "publisher"),
                    P(PublisherConfig.UseStandardsCompliantEncodingKey, "true")))
                .ToOptions().Value;

            // From the existing contract test
            Assert.Equal("publisher", options.PublisherId);
            Assert.True(options.UseStandardsCompliantEncoding);
            Assert.Equal(MessagingMode.PubSub, options.MessagingProfile!.MessagingMode);
            Assert.Equal(MessageEncoding.Json, options.MessagingProfile.MessageEncoding);
            Assert.Equal(0, options.BatchSize);
            Assert.Equal(TimeSpan.Zero, options.BatchTriggerInterval);
            Assert.Equal(1000, options.MaxNodesPerDataSet);
            Assert.False(options.DisableDataSetMetaData);
        }
    }
}
