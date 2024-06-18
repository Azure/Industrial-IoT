// ------------------------------------------------------------
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
    using System.Text;

    /// <summary>
    /// Default client configuration
    /// </summary>
    public sealed class OpcUaClientConfig : PostConfigureOptionBase<OpcUaClientOptions>
    {
        /// <summary>
        /// Configuration
        /// </summary>
        public const string PkiRootPathKey = "PkiRootPath";
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string ApplicationNameKey = "ApplicationName";
        public const string ApplicationUriKey = "ApplicationUri";
        public const string ProductUriKey = "ProductUri";
        public const string DefaultSessionTimeoutKey = "DefaultSessionTimeout";
        public const string DefaultServiceCallTimeoutKey = "DefaultServiceCallTimeout";
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
        public const string TrustedUserCertificatesTypeKey = "TrustedUserCertificatesType";
        public const string TrustedUserCertificatesPathKey = "TrustedUserCertificatesPath";
        public const string UserIssuerCertificatesTypeKey = "UserIssuerCertificatesType";
        public const string UserIssuerCertificatesPathKey = "UserIssuerCertificatesPath";
        public const string HttpsIssuerCertificatesTypeKey = "HttpsIssuerCertificatesType";
        public const string HttpsIssuerCertificatesPathKey = "HttpsIssuerCertificatesPath";
        public const string TrustedHttpsCertificatesTypeKey = "TrustedHttpsCertificatesType";
        public const string TrustedHttpsCertificatesPathKey = "TrustedHttpsCertificatesPath";
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
        public const string SubscriptionErrorRetryDelaySecondsKey = "SubscriptionErrorRetryDelaySeconds";
        public const string InvalidMonitoredItemRetryDelaySecondsKey = "InvalidMonitoredItemRetryDelaySeconds";
        public const string BadMonitoredItemRetryDelaySecondsKey = "BadMonitoredItemRetryDelaySeconds";
        public const string SubscriptionManagementIntervalSecondsKey = "SubscriptionManagementIntervalSeconds";
        public const string LingerTimeoutSecondsKey = "LingerTimeoutSeconds";
        public const string ApplicationCertificatePasswordKey = "ApplicationCertificatePassword";
        public const string TryConfigureFromExistingAppCertKey = "TryConfigureFromExistingAppCert";
        public const string ReverseConnectPortKey = "ReverseConnectPort";
        public const string DisableComplexTypePreloadingKey = "DisableComplexTypePreloading";
        public const string PublishRequestsPerSubscriptionPercentKey = "PublishRequestsPerSubscriptionPercent";
        public const string ActivePublishErrorHandlingKey = "ActivePublishErrorHandling";
        public const string MinPublishRequestsKey = "MinPublishRequests";
        public const string MaxNodesPerBrowseOverrideKey = "MaxNodesPerBrowseOverride";
        public const string MaxNodesPerReadOverrideKey = "MaxNodesPerReadOverride";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Default values for transport quotas.
        /// </summary>
        public const string ApplicationNameDefault = "Microsoft.Azure.IIoT";
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static readonly CompositeFormat ApplicationUriDefault =
            CompositeFormat.Parse("urn:localhost:{0}:microsoft:");
        public const string ProductUriDefault = "https://www.github.com/Azure/Industrial-IoT";
        public const string PkiRootPathDefault = "pki";
        public const int SecurityTokenLifetimeDefault = 60 * 60 * 1000;
        public const int ChannelLifetimeDefault = 300 * 1000;
        public const int MaxBufferSizeDefault = 64 * 1024;
        public const int MaxMessageSizeDefault = 8 * 1024 * 1024;
        public const int MaxArrayLengthDefault = (64 * 1024) - 1;
        public const int MaxByteStringLengthDefault = 1024 * 1024;
        public const int MaxStringLengthDefault = (128 * 1024) - 256;
        public const int OperationTimeoutDefault = 120 * 1000;
        public const int SubscriptionErrorRetryDelayDefaultSec = 2;
        public const int InvalidMonitoredItemRetryDelayDefaultSec = 5 * 60;
        public const int BadMonitoredItemRetryDelayDefaultSec = 30 * 60;
        public const int DefaultSessionTimeoutDefaultSec = 60;
        public const int DefaultServiceCallTimeoutDefaultSec = 3 * 60;
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
        public const bool ActivePublishErrorHandlingDefault = true;
        public const int MinPublishRequestsDefault = 3;
        public const int PublishRequestsPerSubscriptionPercentDefault = 100;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void PostConfigure(string? name, OpcUaClientOptions options)
        {
            if (string.IsNullOrEmpty(options.ApplicationName))
            {
                options.ApplicationName = GetStringOrDefault(ApplicationNameKey);
                if (string.IsNullOrEmpty(options.ApplicationName) ||
                    options.ApplicationName == "Azure.IIoT.OpcUa.Publisher.Module")
                {
                    options.ApplicationName = ApplicationNameDefault;
                }
            }

            if (string.IsNullOrEmpty(options.ApplicationUri))
            {
                options.ApplicationUri = GetStringOrDefault(ApplicationUriKey,
                    string.Format(CultureInfo.InvariantCulture,
                        ApplicationUriDefault, options.ApplicationName));
            }

            if (string.IsNullOrEmpty(options.ProductUri))
            {
                options.ProductUri = GetStringOrDefault(ProductUriKey,
                    ProductUriDefault);
            }

            if (options.DefaultSessionTimeoutDuration == null)
            {
                var sessionTimeout = GetIntOrDefault(DefaultSessionTimeoutKey,
                    DefaultSessionTimeoutDefaultSec);
                if (sessionTimeout > 0)
                {
                    options.DefaultSessionTimeoutDuration = TimeSpan.FromSeconds(sessionTimeout);
                }
            }

            if (options.DefaultServiceCallTimeoutDuration == null)
            {
                var serviceCallTimeout = GetIntOrDefault(DefaultServiceCallTimeoutKey,
                    DefaultServiceCallTimeoutDefaultSec);
                if (serviceCallTimeout > 0)
                {
                    options.DefaultServiceCallTimeoutDuration = TimeSpan.FromSeconds(serviceCallTimeout);
                }
            }

            if (options.KeepAliveIntervalDuration == null)
            {
                var keepAliveInterval = GetIntOrDefault(KeepAliveIntervalKey,
                    KeepAliveIntervalDefaultSec);
                if (keepAliveInterval > 0)
                {
                    options.KeepAliveIntervalDuration = TimeSpan.FromSeconds(keepAliveInterval);
                }
            }

            if (options.CreateSessionTimeoutDuration == null)
            {
                var createSessionTimeout = GetIntOrDefault(CreateSessionTimeoutKey,
                    CreateSessionTimeoutDefaultSec);
                if (createSessionTimeout > 0)
                {
                    options.CreateSessionTimeoutDuration = TimeSpan.FromSeconds(createSessionTimeout);
                }
            }

            options.ReverseConnectPort ??= GetIntOrDefault(ReverseConnectPortKey,
                    ReverseConnectPortDefault);

            if (options.MinReconnectDelayDuration == null)
            {
                var reconnectDelay = GetIntOrDefault(MinReconnectDelayKey,
                    MinReconnectDelayDefault);
                if (reconnectDelay > 0)
                {
                    options.MinReconnectDelayDuration = TimeSpan.FromMilliseconds(reconnectDelay);
                }
            }

            if (options.MaxReconnectDelayDuration == null)
            {
                var reconnectDelay = GetIntOrDefault(MaxReconnectDelayKey,
                    MaxReconnectDelayDefault);
                if (reconnectDelay > 0)
                {
                    options.MaxReconnectDelayDuration = TimeSpan.FromMilliseconds(reconnectDelay);
                }
            }

            if (options.LingerTimeoutDuration == null)
            {
                var lingerTimeout = GetIntOrDefault(LingerTimeoutSecondsKey);
                if (lingerTimeout > 0)
                {
                    options.LingerTimeoutDuration = TimeSpan.FromSeconds(lingerTimeout);
                }
            }

            options.DisableComplexTypePreloading ??= GetBoolOrDefault(DisableComplexTypePreloadingKey);

            if (options.SubscriptionErrorRetryDelay == null)
            {
                var retryTimeout = GetIntOrNull(SubscriptionErrorRetryDelaySecondsKey);
                if (retryTimeout.HasValue)
                {
                    options.SubscriptionErrorRetryDelay =
                        TimeSpan.FromSeconds(retryTimeout.Value);
                }
            }

            if (options.BadMonitoredItemRetryDelayDuration == null)
            {
                var retryTimeout = GetIntOrNull(BadMonitoredItemRetryDelaySecondsKey);
                if (retryTimeout.HasValue)
                {
                    options.BadMonitoredItemRetryDelayDuration =
                        TimeSpan.FromSeconds(retryTimeout.Value);
                }
            }

            if (options.InvalidMonitoredItemRetryDelayDuration == null)
            {
                var retryTimeout = GetIntOrNull(InvalidMonitoredItemRetryDelaySecondsKey);
                if (retryTimeout.HasValue)
                {
                    options.InvalidMonitoredItemRetryDelayDuration =
                        TimeSpan.FromSeconds(retryTimeout.Value);
                }
            }

            if (options.SubscriptionManagementIntervalDuration == null)
            {
                var managementInterval = GetIntOrNull(SubscriptionManagementIntervalSecondsKey);
                if (managementInterval.HasValue)
                {
                    options.SubscriptionManagementIntervalDuration =
                        TimeSpan.FromSeconds(managementInterval.Value);
                }
            }

            options.MinPublishRequests ??= GetIntOrDefault(MinPublishRequestsKey,
                    MinPublishRequestsDefault);

            options.PublishRequestsPerSubscriptionPercent ??= GetIntOrNull(
                    PublishRequestsPerSubscriptionPercentKey,
                    PublishRequestsPerSubscriptionPercentDefault);

            options.ActivePublishErrorHandling ??= GetBoolOrDefault(
                    ActivePublishErrorHandlingKey, ActivePublishErrorHandlingDefault);

            options.MaxNodesPerReadOverride ??= GetIntOrNull(MaxNodesPerReadOverrideKey);

            options.MaxNodesPerBrowseOverride ??= GetIntOrNull(MaxNodesPerBrowseOverrideKey);

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

            options.EnableOpcUaStackLogging ??= GetBoolOrNull(EnableOpcUaStackLoggingKey);

            if (options.Security.TrustedUserCertificates == null)
            {
                options.Security.TrustedUserCertificates = new()
                {
                    StorePath = GetStringOrDefault(TrustedUserCertificatesPathKey,
                        $"{options.Security.PkiRootPath}/user"),
                    StoreType = GetStringOrDefault(TrustedUserCertificatesTypeKey,
                        CertificateStoreType.Directory)
                };
            }

            if (options.Security.TrustedHttpsCertificates == null)
            {
                options.Security.TrustedHttpsCertificates = new()
                {
                    StorePath = GetStringOrDefault(TrustedHttpsCertificatesPathKey,
                        $"{options.Security.PkiRootPath}/https"),
                    StoreType = GetStringOrDefault(TrustedHttpsCertificatesTypeKey,
                        CertificateStoreType.Directory)
                };
            }

            if (options.Security.HttpsIssuerCertificates == null)
            {
                options.Security.HttpsIssuerCertificates = new()
                {
                    StorePath = GetStringOrDefault(HttpsIssuerCertificatesPathKey,
                        $"{options.Security.PkiRootPath}/https/issuer"),
                    StoreType = GetStringOrDefault(HttpsIssuerCertificatesTypeKey,
                        CertificateStoreType.Directory)
                };
            }

            if (options.Security.UserIssuerCertificates == null)
            {
                options.Security.UserIssuerCertificates = new()
                {
                    StorePath = GetStringOrDefault(UserIssuerCertificatesPathKey,
                        $"{options.Security.PkiRootPath}/user/issuer"),
                    StoreType = GetStringOrDefault(UserIssuerCertificatesTypeKey,
                        CertificateStoreType.Directory)
                };
            }

            if (options.Security.ApplicationCertificatePassword == null)
            {
                options.Security.ApplicationCertificatePassword =
                    GetStringOrDefault(ApplicationCertificatePasswordKey);
            }
            if (options.Security.TryUseConfigurationFromExistingAppCert == null)
            {
                options.Security.TryUseConfigurationFromExistingAppCert =
                    GetBoolOrNull(TryConfigureFromExistingAppCertKey);
            }
        }

        /// <inheritdoc/>
        public OpcUaClientConfig(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
