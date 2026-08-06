// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="OpcUaClientConfig.PostConfigure"/>.
    /// </summary>
    public sealed class OpcUaClientConfigTests
    {
        private static OpcUaClientOptions Configure(
            params KeyValuePair<string, string?>[] pairs)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(pairs)
                .Build();
            return new OpcUaClientConfig(configuration).ToOptions().Value;
        }

        private static KeyValuePair<string, string?> P(string k, string? v)
            => new(k, v);

        // ── ApplicationName ───────────────────────────────────────────────────

        [Fact]
        public void Defaults_ApplicationName_IsMicrosoftAzureIIoT()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.ApplicationNameDefault, opts.ApplicationName);
        }

        [Fact]
        public void Config_ApplicationName_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.ApplicationNameKey, "MyApp"));
            Assert.Equal("MyApp", opts.ApplicationName);
        }

        [Fact]
        public void Config_ApplicationName_ModuleNameNotReplacedWhenSetViaConfig()
        {
            // When the module name is provided explicitly via configuration, the source-generated
            // binder sets it directly, bypassing the PostConfigure replacement guard.
            var opts = Configure(P(OpcUaClientConfig.ApplicationNameKey,
                "Azure.IIoT.OpcUa.Publisher.Module"));
            Assert.Equal("Azure.IIoT.OpcUa.Publisher.Module", opts.ApplicationName);
        }

        [Fact]
        public void PostConfigure_ApplicationName_ModuleNameReplacedWhenPassedAsNull()
        {
            // When ApplicationName is null in options and the config has the module placeholder,
            // PostConfigure replaces it with the default name.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([
                    new(OpcUaClientConfig.ApplicationNameKey, "Azure.IIoT.OpcUa.Publisher.Module")])
                .Build();
            var config = new OpcUaClientConfig(configuration);
            var opts = new OpcUaClientOptions(); // ApplicationName is null here
            config.PostConfigure(null, opts);
            Assert.Equal(OpcUaClientConfig.ApplicationNameDefault, opts.ApplicationName);
        }

        // ── ApplicationUri ────────────────────────────────────────────────────

        [Fact]
        public void Defaults_ApplicationUri_ContainsApplicationName()
        {
            var opts = Configure();
            // URI includes the application name
            Assert.Contains(opts.ApplicationName, opts.ApplicationUri,
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Config_ApplicationUri_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.ApplicationUriKey, "urn:custom-uri"));
            Assert.Equal("urn:custom-uri", opts.ApplicationUri);
        }

        // ── ProductUri ────────────────────────────────────────────────────────

        [Fact]
        public void Defaults_ProductUri_IsGitHubUrl()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.ProductUriDefault, opts.ProductUri);
        }

        [Fact]
        public void Config_ProductUri_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.ProductUriKey, "https://my.product/"));
            Assert.Equal("https://my.product/", opts.ProductUri);
        }

        // ── Session timeouts ──────────────────────────────────────────────────

        [Fact]
        public void Defaults_DefaultSessionTimeoutDuration_IsDefaultSeconds()
        {
            var opts = Configure();
            Assert.Equal(TimeSpan.FromSeconds(OpcUaClientConfig.DefaultSessionTimeoutDefaultSec),
                opts.DefaultSessionTimeoutDuration);
        }

        [Fact]
        public void Config_DefaultSessionTimeout_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.DefaultSessionTimeoutKey, "120"));
            Assert.Equal(TimeSpan.FromSeconds(120), opts.DefaultSessionTimeoutDuration);
        }

        [Fact]
        public void Config_DefaultSessionTimeout_Zero_LeavesNull()
        {
            // When provided as 0, the condition `sessionTimeout > 0` is false, so value stays null
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([new(OpcUaClientConfig.DefaultSessionTimeoutKey, "0")])
                .Build();
            var config = new OpcUaClientConfig(configuration);
            var opts = new OpcUaClientOptions();
            config.PostConfigure(null, opts);
            Assert.Null(opts.DefaultSessionTimeoutDuration);
        }

        [Fact]
        public void Defaults_DefaultServiceCallTimeoutDuration_IsDefaultSeconds()
        {
            var opts = Configure();
            Assert.Equal(TimeSpan.FromSeconds(OpcUaClientConfig.DefaultServiceCallTimeoutDefaultSec),
                opts.DefaultServiceCallTimeoutDuration);
        }

        [Fact]
        public void Config_DefaultServiceCallTimeout_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.DefaultServiceCallTimeoutKey, "90"));
            Assert.Equal(TimeSpan.FromSeconds(90), opts.DefaultServiceCallTimeoutDuration);
        }

        [Fact]
        public void Defaults_DefaultConnectTimeout_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.DefaultConnectTimeoutDuration);
        }

        [Fact]
        public void Config_DefaultConnectTimeout_SetsValue()
        {
            var opts = Configure(P(OpcUaClientConfig.DefaultConnectTimeoutKey, "45"));
            Assert.Equal(TimeSpan.FromSeconds(45), opts.DefaultConnectTimeoutDuration);
        }

        [Fact]
        public void Config_DefaultConnectTimeout_Zero_LeavesNull()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([new(OpcUaClientConfig.DefaultConnectTimeoutKey, "0")])
                .Build();
            var config = new OpcUaClientConfig(configuration);
            var opts = new OpcUaClientOptions();
            config.PostConfigure(null, opts);
            Assert.Null(opts.DefaultConnectTimeoutDuration);
        }

        // ── Keep-alive / create-session / reconnect ───────────────────────────

        [Fact]
        public void Defaults_KeepAliveIntervalDuration_IsDefaultSeconds()
        {
            var opts = Configure();
            Assert.Equal(TimeSpan.FromSeconds(OpcUaClientConfig.KeepAliveIntervalDefaultSec),
                opts.KeepAliveIntervalDuration);
        }

        [Fact]
        public void Config_KeepAliveInterval_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.KeepAliveIntervalKey, "30"));
            Assert.Equal(TimeSpan.FromSeconds(30), opts.KeepAliveIntervalDuration);
        }

        [Fact]
        public void Defaults_CreateSessionTimeoutDuration_IsDefaultSeconds()
        {
            var opts = Configure();
            Assert.Equal(TimeSpan.FromSeconds(OpcUaClientConfig.CreateSessionTimeoutDefaultSec),
                opts.CreateSessionTimeoutDuration);
        }

        [Fact]
        public void Config_CreateSessionTimeout_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.CreateSessionTimeoutKey, "15"));
            Assert.Equal(TimeSpan.FromSeconds(15), opts.CreateSessionTimeoutDuration);
        }

        [Fact]
        public void Defaults_MinReconnectDelayDuration_IsDefaultMs()
        {
            var opts = Configure();
            Assert.Equal(TimeSpan.FromMilliseconds(OpcUaClientConfig.MinReconnectDelayDefault),
                opts.MinReconnectDelayDuration);
        }

        [Fact]
        public void Config_MinReconnectDelay_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.MinReconnectDelayKey, "2000"));
            Assert.Equal(TimeSpan.FromMilliseconds(2000), opts.MinReconnectDelayDuration);
        }

        [Fact]
        public void Defaults_MaxReconnectDelayDuration_IsDefaultMs()
        {
            var opts = Configure();
            Assert.Equal(TimeSpan.FromMilliseconds(OpcUaClientConfig.MaxReconnectDelayDefault),
                opts.MaxReconnectDelayDuration);
        }

        [Fact]
        public void Config_MaxReconnectDelay_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.MaxReconnectDelayKey, "30000"));
            Assert.Equal(TimeSpan.FromMilliseconds(30000), opts.MaxReconnectDelayDuration);
        }

        // ── Linger timeout ────────────────────────────────────────────────────

        [Fact]
        public void Defaults_LingerTimeoutDuration_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.LingerTimeoutDuration);
        }

        [Fact]
        public void Config_LingerTimeout_SetsValue()
        {
            var opts = Configure(P(OpcUaClientConfig.LingerTimeoutSecondsKey, "120"));
            Assert.Equal(TimeSpan.FromSeconds(120), opts.LingerTimeoutDuration);
        }

        [Fact]
        public void Config_LingerTimeout_Zero_LeavesNull()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([new(OpcUaClientConfig.LingerTimeoutSecondsKey, "0")])
                .Build();
            var config = new OpcUaClientConfig(configuration);
            var opts = new OpcUaClientOptions();
            config.PostConfigure(null, opts);
            Assert.Null(opts.LingerTimeoutDuration);
        }

        // ── ReverseConnectPort ────────────────────────────────────────────────

        [Fact]
        public void Defaults_ReverseConnectPort_IsDefault()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.ReverseConnectPortDefault, opts.ReverseConnectPort);
        }

        [Fact]
        public void Config_ReverseConnectPort_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.ReverseConnectPortKey, "4841"));
            Assert.Equal(4841, opts.ReverseConnectPort);
        }

        // ── Publish requests ──────────────────────────────────────────────────

        [Fact]
        public void Defaults_MinPublishRequests_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.MinPublishRequests);
        }

        [Fact]
        public void Config_MinPublishRequests_SetsValue()
        {
            var opts = Configure(P(OpcUaClientConfig.MinPublishRequestsKey, "3"));
            Assert.Equal(3, opts.MinPublishRequests);
        }

        [Fact]
        public void Defaults_MaxPublishRequests_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.MaxPublishRequests);
        }

        [Fact]
        public void Config_MaxPublishRequests_SetsValue()
        {
            var opts = Configure(P(OpcUaClientConfig.MaxPublishRequestsKey, "8"));
            Assert.Equal(8, opts.MaxPublishRequests);
        }

        [Fact]
        public void Defaults_PublishRequestsPerSubscriptionPercent_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.PublishRequestsPerSubscriptionPercent);
        }

        [Fact]
        public void Config_PublishRequestsPerSubscriptionPercent_SetsValue()
        {
            var opts = Configure(P(OpcUaClientConfig.PublishRequestsPerSubscriptionPercentKey, "150"));
            Assert.Equal(150, opts.PublishRequestsPerSubscriptionPercent);
        }

        // ── Node cache ────────────────────────────────────────────────────────

        [Fact]
        public void Defaults_NodeCacheCapacity_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.NodeCacheCapacity);
        }

        [Fact]
        public void Config_NodeCacheCapacity_SetsValue()
        {
            var opts = Configure(P(OpcUaClientConfig.NodeCacheCapacityKey, "1000"));
            Assert.Equal(1000, opts.NodeCacheCapacity);
        }

        [Fact]
        public void Defaults_NodeCacheTimeout_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.NodeCacheTimeout);
        }

        [Fact]
        public void Config_NodeCacheTimeout_SetsValue()
        {
            var opts = Configure(P(OpcUaClientConfig.NodeCacheTimeoutKey, "01:00:00"));
            Assert.Equal(TimeSpan.FromHours(1), opts.NodeCacheTimeout);
        }

        // ── MaxNodesPerReadOverride / MaxNodesPerBrowseOverride ───────────────

        [Fact]
        public void Defaults_MaxNodesPerReadOverride_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.MaxNodesPerReadOverride);
        }

        [Fact]
        public void Config_MaxNodesPerReadOverride_SetsValue()
        {
            var opts = Configure(P(OpcUaClientConfig.MaxNodesPerReadOverrideKey, "100"));
            Assert.Equal(100, opts.MaxNodesPerReadOverride);
        }

        [Fact]
        public void Defaults_MaxNodesPerBrowseOverride_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.MaxNodesPerBrowseOverride);
        }

        [Fact]
        public void Config_MaxNodesPerBrowseOverride_SetsValue()
        {
            var opts = Configure(P(OpcUaClientConfig.MaxNodesPerBrowseOverrideKey, "200"));
            Assert.Equal(200, opts.MaxNodesPerBrowseOverride);
        }

        // ── Security defaults ─────────────────────────────────────────────────

        [Fact]
        public void Defaults_Security_PkiRootPath_IsDefault()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.PkiRootPathDefault, opts.Security.PkiRootPath);
        }

        [Fact]
        public void Config_PkiRootPath_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.PkiRootPathKey, "custom/pki"));
            Assert.Equal("custom/pki", opts.Security.PkiRootPath);
        }

        [Fact]
        public void Defaults_Security_MinimumCertificateKeySize_IsDefault()
        {
            var opts = Configure();
            Assert.Equal((ushort)OpcUaClientConfig.MinimumCertificateKeySizeDefault,
                opts.Security.MinimumCertificateKeySize);
        }

        [Fact]
        public void Config_MinimumCertificateKeySize_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.MinimumCertificateKeySizeKey, "2048"));
            Assert.Equal((ushort)2048, opts.Security.MinimumCertificateKeySize);
        }

        [Fact]
        public void Defaults_Security_AutoAcceptUntrustedCertificates_IsFalse()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.AutoAcceptUntrustedCertificatesDefault,
                opts.Security.AutoAcceptUntrustedCertificates);
        }

        [Fact]
        public void Defaults_Security_RejectSha1SignedCertificates_IsFalse()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.RejectSha1SignedCertificatesDefault,
                opts.Security.RejectSha1SignedCertificates);
        }

        [Fact]
        public void Defaults_Security_AddAppCertToTrustedStore_IsTrue()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.AddAppCertToTrustedStoreDefault,
                opts.Security.AddAppCertToTrustedStore);
        }

        [Fact]
        public void Defaults_Security_RejectUnknownRevocationStatus_IsTrue()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.RejectUnknownRevocationStatusDefault,
                opts.Security.RejectUnknownRevocationStatus);
        }

        // ── Security cert store paths ─────────────────────────────────────────

        [Fact]
        public void Defaults_Security_ApplicationCertificates_StorePathContainsPkiRoot()
        {
            var opts = Configure();
            Assert.NotNull(opts.Security.ApplicationCertificates);
            Assert.Contains(opts.Security.PkiRootPath!,
                opts.Security.ApplicationCertificates!.StorePath!,
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Defaults_Security_TrustedPeerCertificates_StorePathContainsPkiRoot()
        {
            var opts = Configure();
            Assert.NotNull(opts.Security.TrustedPeerCertificates);
            Assert.Contains(opts.Security.PkiRootPath!,
                opts.Security.TrustedPeerCertificates!.StorePath!,
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Defaults_Security_RejectedCertificateStore_StorePathContainsRejected()
        {
            var opts = Configure();
            Assert.NotNull(opts.Security.RejectedCertificateStore);
            Assert.Contains("rejected",
                opts.Security.RejectedCertificateStore!.StorePath!,
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Config_ApplicationCertificateSubjectName_IncludesApplicationName()
        {
            var opts = Configure();
            // The default subject name includes the application name
            Assert.NotNull(opts.Security.ApplicationCertificates!.SubjectName);
            Assert.Contains(opts.ApplicationName!,
                opts.Security.ApplicationCertificates!.SubjectName!,
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Config_ApplicationCertificateStorePath_CanBeOverridden()
        {
            var opts = Configure(P(OpcUaClientConfig.ApplicationCertificateStorePathKey,
                "custom/certs/own"));
            Assert.Equal("custom/certs/own", opts.Security.ApplicationCertificates!.StorePath);
        }

        // ── Quotas defaults ───────────────────────────────────────────────────

        [Fact]
        public void Defaults_Quotas_ChannelLifetime_IsDefault()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.ChannelLifetimeDefault, opts.Quotas.ChannelLifetime);
        }

        [Fact]
        public void Config_ChannelLifetime_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.ChannelLifetimeKey, "600000"));
            Assert.Equal(600000, opts.Quotas.ChannelLifetime);
        }

        [Fact]
        public void Defaults_Quotas_MaxArrayLength_IsDefault()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.MaxArrayLengthDefault, opts.Quotas.MaxArrayLength);
        }

        [Fact]
        public void Config_MaxArrayLength_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.MaxArrayLengthKey, "131071"));
            Assert.Equal(131071, opts.Quotas.MaxArrayLength);
        }

        [Fact]
        public void Defaults_Quotas_MaxBufferSize_IsDefault()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.MaxBufferSizeDefault, opts.Quotas.MaxBufferSize);
        }

        [Fact]
        public void Defaults_Quotas_MaxMessageSize_IsDefault()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.MaxMessageSizeDefault, opts.Quotas.MaxMessageSize);
        }

        [Fact]
        public void Defaults_Quotas_MaxByteStringLength_IsDefault()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.MaxByteStringLengthDefault, opts.Quotas.MaxByteStringLength);
        }

        [Fact]
        public void Defaults_Quotas_MaxStringLength_IsDefault()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.MaxStringLengthDefault, opts.Quotas.MaxStringLength);
        }

        [Fact]
        public void Defaults_Quotas_OperationTimeout_IsDefault()
        {
            var opts = Configure();
            Assert.Equal(OpcUaClientConfig.OperationTimeoutDefault, opts.Quotas.OperationTimeout);
        }

        [Fact]
        public void Config_OperationTimeout_OverridesDefault()
        {
            var opts = Configure(P(OpcUaClientConfig.OperationTimeoutKey, "60000"));
            Assert.Equal(60000, opts.Quotas.OperationTimeout);
        }

        // ── OpcUaKeySetLogFolderName ──────────────────────────────────────────

        [Fact]
        public void Defaults_OpcUaKeySetLogFolderName_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.OpcUaKeySetLogFolderName);
        }

        [Fact]
        public void Config_OpcUaKeySetLogFolderName_SetsValue()
        {
            var opts = Configure(P(OpcUaClientConfig.OpcUaKeySetLogFolderNameKey, "keysets"));
            Assert.Equal("keysets", opts.OpcUaKeySetLogFolderName);
        }

        // ── Pre-configured values are preserved ───────────────────────────────

        [Fact]
        public void PreConfigured_ApplicationName_IsPreserved()
        {
            var configuration = new ConfigurationBuilder().Build();
            var config = new OpcUaClientConfig(configuration);
            var opts = new OpcUaClientOptions { ApplicationName = "AlreadySet" };
            config.PostConfigure(null, opts);
            Assert.Equal("AlreadySet", opts.ApplicationName);
        }

        [Fact]
        public void PreConfigured_ApplicationUri_IsPreserved()
        {
            var configuration = new ConfigurationBuilder().Build();
            var config = new OpcUaClientConfig(configuration);
            var opts = new OpcUaClientOptions
            {
                ApplicationName = "App",
                ApplicationUri = "urn:already-set"
            };
            config.PostConfigure(null, opts);
            Assert.Equal("urn:already-set", opts.ApplicationUri);
        }

        [Fact]
        public void PreConfigured_Security_ApplicationCertificates_IsPreserved()
        {
            var configuration = new ConfigurationBuilder().Build();
            var config = new OpcUaClientConfig(configuration);
            var existingStore = new CertificateInfo { StorePath = "already/set" };
            var opts = new OpcUaClientOptions();
            opts.Security.ApplicationCertificates = existingStore;
            config.PostConfigure(null, opts);
            Assert.Same(existingStore, opts.Security.ApplicationCertificates);
        }
    }
}
