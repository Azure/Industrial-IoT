﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Runtime
{
    using Furly.Extensions.Configuration;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;
    using System;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// Default client configuration
    /// </summary>
    public sealed class OpcUaClientConfig : PostConfigureOptionBase<OpcUaClientOptions>
    {
        /// <summary>
        /// Configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string PkiRootPathKey = "PkiRootPath";
        public const string ApplicationNameKey = "ApplicationName";
        public const string ApplicationUriKey = "ApplicationUri";
        public const string ProductUriKey = "ProductUri";
        public const string DefaultSessionTimeoutKey = "DefaultSessionTimeout";
        public const string KeepAliveIntervalKey = "KeepAliveInterval";
        public const string ApplicationCertificateStorePathKey = "ApplicationCertificateStorePath";
        public const string ApplicationCertificateStoreTypeKey = "ApplicationCertificateStoreType";
        public const string ApplicationCertificateSubjectNameKey = "ApplicationCertificateSubjectName";
        public const string TrustedIssuerCertificatesPathKey = "TrustedIssuerCertificatesPath";
        public const string TrustedIssuerCertificatesTypeKey = "TrustedIssuerCertificatesType";
        public const string TrustedPeerCertificatesPathKey = "TrustedPeerCertificatesPath";
        public const string TrustedPeerCertificatesTypeKey = "TrustedPeerCertificatesType";
        public const string RejectedCertificateStorePathKey = "RejectedCertificateStorePath";
        public const string RejectedCertificateStoreTypeKey = "RejectedCertificateStoreType";
        public const string AutoAcceptUntrustedCertificatesKey = "AutoAcceptUntrustedCertificates";
        public const string RejectSha1SignedCertificatesKey = "RejectSha1SignedCertificates";
        public const string MinimumCertificateKeySizeKey = "MinimumCertificateKeySize";
        public const string AddAppCertToTrustedStoreKey = "AddAppCertToTrustedStore";
        public const string RejectUnknownRevocationStatusKey = "RejectUnknownRevocationStatus";
        public const string SecurityTokenLifetimeKey = "SecurityTokenLifetime";
        public const string EnableOpcUaStackLoggingKey = "EnableOpcUaStackLogging";
        public const string ChannelLifetimeKey = "ChannelLifetime";
        public const string MaxBufferSizeKey = "MaxBufferSize";
        public const string MaxMessageSizeKey = "MaxMessageSize";
        public const string MaxArrayLengthKey = "MaxArrayLength";
        public const string MaxByteStringLengthKey = "MaxByteStringLength";
        public const string MaxStringLengthKey = "MaxStringLength";
        public const string OperationTimeoutKey = "OperationTimeout";
        public const string CreateSessionTimeoutKey = "CreateSessionTimeout";
        public const string MaxReconnectDelayKey = "MaxReconnectDelay";
        public const string MinReconnectDelayKey = "MinReconnectDelay";
        public const string SubscriptionErrorRetryDelayKey = "SubscriptionErrorRetryDelay";
        public const string InvalidMonitoredItemRetryDelayKey = "InvalidMonitoredItemRetryDelay";
        public const string BadMonitoredItemRetryDelayKey = "BadMonitoredItemRetryDelay";
        public const string SubscriptionManagementIntervalKey = "SubscriptionManagementInterval";
        public const string LingerTimeoutKey = "LingerTimeout";
        public const string ReverseConnectPortKey = "ReverseConnectPort";

        /// <summary>
        /// Default values for transport quotas.
        /// </summary>
        public const string ApplicationNameDefault = "Microsoft.Azure.IIoT";
        public const string ApplicationUriDefault = "urn:localhost:{0}:microsoft:";
        public const string ProductUriDefault = "https://www.github.com/Azure/Industrial-IoT";
        public const string PkiRootPathDefault = "pki";
        public const int SecurityTokenLifetimeDefault = 60 * 60 * 1000;
        public const int ChannelLifetimeDefault = 300 * 1000;
        public const int MaxBufferSizeDefault = (64 * 1024) - 1;
        public const int MaxMessageSizeDefault = 4 * 1024 * 1024;
        public const int MaxArrayLengthDefault = (64 * 1024) - 1;
        public const int MaxByteStringLengthDefault = 1024 * 1024;
        public const int MaxStringLengthDefault = (128 * 1024) - 256;
        public const int OperationTimeoutDefault = 120 * 1000;
        public const int SubscriptionErrorRetryDelayDefaultSec = 2;
        public const int InvalidMonitoredItemRetryDelayDefaultSec = 5 * 60;
        public const int BadMonitoredItemRetryDelayDefaultSec = 30 * 60;
        public const int DefaultSessionTimeoutDefaultSec = 60;
        public const int KeepAliveIntervalDefaultSec = 10;
        public const int CreateSessionTimeoutDefaultSec = 5;
        public const int MaxReconnectDelayDefault = 60 * 1000;
        public const int MinReconnectDelayDefault = 1000;
        public const int ReverseConnectPortDefault = 4840;
        public const int MinimumCertificateKeySizeDefault = 1024;
        public const bool AutoAcceptUntrustedCertificatesDefault = false;
        public const bool RejectSha1SignedCertificatesDefault = false;
        public const bool AddAppCertToTrustedStoreDefault = true;
        public const bool RejectUnknownRevocationStatusDefault = true;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void PostConfigure(string? name, OpcUaClientOptions options)
        {
            if (options.ApplicationName == null)
            {
                options.ApplicationName =
                    GetStringOrDefault(ApplicationNameKey, ApplicationNameDefault);
            }

            if (options.ApplicationUri == null)
            {
                options.ApplicationUri = GetStringOrDefault(ApplicationUriKey,
                    string.Format(CultureInfo.InvariantCulture,
                        ApplicationUriDefault, options.ApplicationName));
            }

            if (options.ProductUri == null)
            {
                options.ProductUri = GetStringOrDefault(ProductUriKey,
                    ProductUriDefault);
            }

            if (options.DefaultSessionTimeout == null)
            {
                var sessionTimeout = GetIntOrDefault(DefaultSessionTimeoutKey,
                    DefaultSessionTimeoutDefaultSec);
                if (sessionTimeout > 0)
                {
                    options.DefaultSessionTimeout = TimeSpan.FromSeconds(sessionTimeout);
                }
            }

            if (options.KeepAliveInterval == null)
            {
                var keepAliveInterval = GetIntOrDefault(KeepAliveIntervalKey,
                    KeepAliveIntervalDefaultSec);
                if (keepAliveInterval > 0)
                {
                    options.KeepAliveInterval = TimeSpan.FromSeconds(keepAliveInterval);
                }
            }

            if (options.CreateSessionTimeout == null)
            {
                var createSessionTimeout = GetIntOrDefault(CreateSessionTimeoutKey,
                    CreateSessionTimeoutDefaultSec);
                if (createSessionTimeout > 0)
                {
                    options.CreateSessionTimeout = TimeSpan.FromSeconds(createSessionTimeout);
                }
            }

            if (options.ReverseConnectPort == null)
            {
                options.ReverseConnectPort = GetIntOrDefault(ReverseConnectPortKey,
                    ReverseConnectPortDefault);
            }

            if (options.MinReconnectDelay == null)
            {
                var reconnectDelay = GetIntOrDefault(MinReconnectDelayKey,
                    MinReconnectDelayDefault);
                if (reconnectDelay > 0)
                {
                    options.MinReconnectDelay = TimeSpan.FromMilliseconds(reconnectDelay);
                }
            }

            if (options.MaxReconnectDelay == null)
            {
                var reconnectDelay = GetIntOrDefault(MaxReconnectDelayKey,
                    MaxReconnectDelayDefault);
                if (reconnectDelay > 0)
                {
                    options.MaxReconnectDelay = TimeSpan.FromMilliseconds(reconnectDelay);
                }
            }

            if (options.LingerTimeout == null)
            {
                var lingerTimeout = GetIntOrDefault(LingerTimeoutKey);
                if (lingerTimeout > 0)
                {
                    options.LingerTimeout = TimeSpan.FromSeconds(lingerTimeout);
                }
            }

            if (options.SubscriptionErrorRetryDelay == null)
            {
                var retryTimeout = GetIntOrDefault(SubscriptionErrorRetryDelayKey);
                if (retryTimeout > 0)
                {
                    options.SubscriptionErrorRetryDelay = TimeSpan.FromSeconds(retryTimeout);
                }
            }

            if (options.BadMonitoredItemRetryDelay == null)
            {
                var retryTimeout = GetIntOrDefault(BadMonitoredItemRetryDelayKey);
                if (retryTimeout > 0)
                {
                    options.BadMonitoredItemRetryDelay = TimeSpan.FromSeconds(retryTimeout);
                }
            }

            if (options.InvalidMonitoredItemRetryDelay == null)
            {
                var retryTimeout = GetIntOrDefault(InvalidMonitoredItemRetryDelayKey);
                if (retryTimeout > 0)
                {
                    options.InvalidMonitoredItemRetryDelay = TimeSpan.FromSeconds(retryTimeout);
                }
            }

            if (options.SubscriptionManagementInterval == null)
            {
                var managementInterval = GetIntOrDefault(SubscriptionManagementIntervalKey);
                if (managementInterval > 0)
                {
                    options.SubscriptionManagementInterval = TimeSpan.FromSeconds(managementInterval);
                }
            }

            if (options.Security.MinimumCertificateKeySize == 0)
            {
                options.Security.MinimumCertificateKeySize = (ushort)GetIntOrDefault(
                    MinimumCertificateKeySizeKey, MinimumCertificateKeySizeDefault);
            }

            if (options.Security.AutoAcceptUntrustedCertificates == null)
            {
                options.Security.AutoAcceptUntrustedCertificates = GetBoolOrDefault(
                    AutoAcceptUntrustedCertificatesKey, AutoAcceptUntrustedCertificatesDefault);
            }

            if (options.Security.RejectSha1SignedCertificates == null)
            {
                options.Security.RejectSha1SignedCertificates = GetBoolOrDefault(
                    RejectSha1SignedCertificatesKey, RejectSha1SignedCertificatesDefault);
            }

            if (options.Security.AddAppCertToTrustedStore == null)
            {
                options.Security.AddAppCertToTrustedStore = GetBoolOrDefault(
                    AddAppCertToTrustedStoreKey, AddAppCertToTrustedStoreDefault);
            }

            if (options.Security.RejectUnknownRevocationStatus == null)
            {
                options.Security.RejectUnknownRevocationStatus = GetBoolOrDefault(
                    RejectUnknownRevocationStatusKey, RejectUnknownRevocationStatusDefault);
            }

            // https://github.com/OPCFoundation/UA-.NETStandard/blob/master/Docs/Certificates.md

            if (options.Security.PkiRootPath == null)
            {
                options.Security.PkiRootPath = GetStringOrDefault(PkiRootPathKey,
                    PkiRootPathDefault);
            }

            if (options.Security.ApplicationCertificate == null)
            {
                options.Security.ApplicationCertificate = new()
                {
                    StorePath = GetStringOrDefault(ApplicationCertificateStorePathKey,
                        $"{options.Security.PkiRootPath}/own"),
                    StoreType = GetStringOrDefault(ApplicationCertificateStoreTypeKey,
                        CertificateStoreType.Directory),
                    SubjectName = GetStringOrDefault(ApplicationCertificateSubjectNameKey,
                        $"CN={options.ApplicationName}, C=DE, S=Bav, O=Microsoft, DC=localhost")
                };
            }

            if (options.Security.RejectedCertificateStore == null)
            {
                options.Security.RejectedCertificateStore = new()
                {
                    StorePath = GetStringOrDefault(RejectedCertificateStorePathKey,
                        $"{options.Security.PkiRootPath}/rejected"),
                    StoreType = GetStringOrDefault(RejectedCertificateStoreTypeKey,
                        CertificateStoreType.Directory)
                };
            }

            if (options.Security.TrustedIssuerCertificates == null)
            {
                //
                // Returns the legacy 'issuers' if folder already exists or per
                // specification.
                //
                var legacyPath = $"{options.Security.PkiRootPath}/issuers";
                var path = Directory.Exists(legacyPath) ? legacyPath :
                    $"{options.Security.PkiRootPath}/issuer";

                options.Security.TrustedIssuerCertificates = new()
                {
                    StorePath = GetStringOrDefault(TrustedIssuerCertificatesPathKey,
                        path),
                    StoreType = GetStringOrDefault(TrustedIssuerCertificatesTypeKey,
                        CertificateStoreType.Directory)
                };
            }

            if (options.Security.TrustedPeerCertificates == null)
            {
                options.Security.TrustedPeerCertificates = new()
                {
                    StorePath = GetStringOrDefault(TrustedPeerCertificatesPathKey,
                        $"{options.Security.PkiRootPath}/trusted"),
                    StoreType = GetStringOrDefault(TrustedPeerCertificatesTypeKey,
                        CertificateStoreType.Directory)
                };
            }

            if (options.Quotas.ChannelLifetime == 0)
            {
                options.Quotas.ChannelLifetime = GetIntOrDefault(ChannelLifetimeKey,
                    ChannelLifetimeDefault);
            }

            if (options.Quotas.MaxArrayLength == 0)
            {
                options.Quotas.MaxArrayLength = GetIntOrDefault(MaxArrayLengthKey,
                    MaxArrayLengthDefault);
            }

            if (options.Quotas.MaxBufferSize == 0)
            {
                options.Quotas.MaxBufferSize = GetIntOrDefault(MaxBufferSizeKey,
                    MaxBufferSizeDefault);
            }

            if (options.Quotas.MaxByteStringLength == 0)
            {
                options.Quotas.MaxByteStringLength = GetIntOrDefault(MaxByteStringLengthKey,
                    MaxByteStringLengthDefault);
            }

            if (options.Quotas.MaxMessageSize == 0)
            {
                options.Quotas.MaxMessageSize = GetIntOrDefault(MaxMessageSizeKey,
                    MaxMessageSizeDefault);
            }

            if (options.Quotas.MaxStringLength == 0)
            {
                options.Quotas.MaxStringLength = GetIntOrDefault(MaxStringLengthKey,
                    MaxStringLengthDefault);
            }

            if (options.Quotas.OperationTimeout == 0)
            {
                options.Quotas.OperationTimeout = GetIntOrDefault(OperationTimeoutKey,
                    OperationTimeoutDefault);
            }

            if (options.Quotas.SecurityTokenLifetime == 0)
            {
                options.Quotas.SecurityTokenLifetime = GetIntOrDefault(SecurityTokenLifetimeKey,
                    SecurityTokenLifetimeDefault);
            }

            if (options.EnableOpcUaStackLogging == null)
            {
                options.EnableOpcUaStackLogging = GetBoolOrNull(EnableOpcUaStackLoggingKey);
            }
        }

        /// <inheritdoc/>
        public OpcUaClientConfig(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
