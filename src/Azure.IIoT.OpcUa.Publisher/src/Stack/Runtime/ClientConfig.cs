// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Runtime
{
    using Furly.Extensions.Configuration;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;
    using System.IO;

    /// <summary>
    /// Default client configuration
    /// </summary>
    public sealed class ClientConfig : PostConfigureOptionBase<ClientOptions>
    {
        /// <summary>
        /// Configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string ApplicationNameKey = "ApplicationName";
        public const string ApplicationUriKey = "ApplicationUri";
        public const string ProductUriKey = "ProductUri";
        public const string DefaultSessionTimeoutKey = "DefaultSessionTimeout";
        public const string MinSubscriptionLifetimeKey = "MinSubscriptionLifetime";
        public const string KeepAliveIntervalKey = "KeepAliveInterval";
        public const string MaxKeepAliveCountKey = "MaxKeepAliveCount";
        public const string PkiRootPathKey = "PkiRootPath";
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
        public const string ChannelLifetimeKey = "ChannelLifetime";
        public const string MaxBufferSizeKey = "MaxBufferSize";
        public const string MaxMessageSizeKey = "MaxMessageSize";
        public const string MaxArrayLengthKey = "MaxArrayLength";
        public const string MaxByteStringLengthKey = "MaxByteStringLength";
        public const string MaxStringLengthKey = "MaxStringLength";
        public const string OperationTimeoutKey = "OperationTimeout";

        /// <summary>
        /// Default values for transport quotas.
        /// </summary>
        public const int DefaultSecurityTokenLifetime = 60 * 60 * 1000;
        public const int DefaultChannelLifetime = 300 * 1000;
        public const int DefaultMaxBufferSize = (64 * 1024) - 1;
        public const int DefaultMaxMessageSize = 4 * 1024 * 1024;
        public const int DefaultMaxArrayLength = (64 * 1024) - 1;
        public const int DefaultMaxByteStringLength = 1024 * 1024;
        public const int DefaultMaxStringLength = (128 * 1024) - 256;
        public const int DefaultOperationTimeout = 120 * 1000;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void PostConfigure(string name, ClientOptions options)
        {
            if (options.ApplicationName == null)
            {
                options.ApplicationName =
                    GetStringOrDefault(ApplicationNameKey, "Microsoft.Azure.IIoT");
            }

            if (options.ApplicationUri == null)
            {
                options.ApplicationUri = GetStringOrDefault(ApplicationUriKey,
                    $"urn:localhost:{options.ApplicationName}:microsoft:");
            }

            if (options.ProductUri == null)
            {
                options.ProductUri = GetStringOrDefault(ProductUriKey,
                    "https://www.github.com/Azure/Industrial-IoT");
            }

            if (options.DefaultSessionTimeout == 0)
            {
                options.DefaultSessionTimeout = (uint)GetIntOrDefault(DefaultSessionTimeoutKey,
                    60) * 1000;
            }

            if (options.MinSubscriptionLifetime == 0)
            {
                options.MinSubscriptionLifetime = GetIntOrDefault(MinSubscriptionLifetimeKey,
                    10) * 1000;
            }

            if (options.KeepAliveInterval == 0)
            {
                options.KeepAliveInterval = GetIntOrDefault(KeepAliveIntervalKey,
                    10) * 1000;
            }

            if (options.MaxKeepAliveCount == 0)
            {
                options.MaxKeepAliveCount = (uint)GetIntOrDefault(MaxKeepAliveCountKey,
                    10);
            }

            if (options.Security.MinimumCertificateKeySize == 0)
            {
                options.Security.MinimumCertificateKeySize = (ushort)GetIntOrDefault(
                    MinimumCertificateKeySizeKey, 1024);
            }

            if (options.Security.AutoAcceptUntrustedCertificates == null)
            {
                options.Security.AutoAcceptUntrustedCertificates = GetBoolOrDefault(
                    AutoAcceptUntrustedCertificatesKey, false);
            }

            if (options.Security.RejectSha1SignedCertificates == null)
            {
                options.Security.RejectSha1SignedCertificates = GetBoolOrDefault(
                    RejectSha1SignedCertificatesKey, false);
            }

            if (options.Security.AddAppCertToTrustedStore == null)
            {
                options.Security.AddAppCertToTrustedStore = GetBoolOrDefault(
                    AddAppCertToTrustedStoreKey, true);
            }

            if (options.Security.RejectUnknownRevocationStatus == null)
            {
                options.Security.RejectUnknownRevocationStatus = GetBoolOrDefault(
                    RejectUnknownRevocationStatusKey, true);
            }

            // https://github.com/OPCFoundation/UA-.NETStandard/blob/master/Docs/Certificates.md

            if (options.Security.PkiRootPath == null)
            {
                options.Security.PkiRootPath = GetStringOrDefault(PkiRootPathKey,
                    "pki");
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
                    DefaultChannelLifetime);
            }

            if (options.Quotas.MaxArrayLength == 0)
            {
                options.Quotas.MaxArrayLength = GetIntOrDefault(MaxArrayLengthKey,
                    DefaultMaxArrayLength);
            }

            if (options.Quotas.MaxBufferSize == 0)
            {
                options.Quotas.MaxBufferSize = GetIntOrDefault(MaxBufferSizeKey,
                    DefaultMaxBufferSize);
            }

            if (options.Quotas.MaxByteStringLength == 0)
            {
                options.Quotas.MaxByteStringLength = GetIntOrDefault(MaxByteStringLengthKey,
                    DefaultMaxByteStringLength);
            }

            if (options.Quotas.MaxMessageSize == 0)
            {
                options.Quotas.MaxMessageSize = GetIntOrDefault(MaxMessageSizeKey,
                    DefaultMaxMessageSize);
            }

            if (options.Quotas.MaxStringLength == 0)
            {
                options.Quotas.MaxStringLength = GetIntOrDefault(MaxStringLengthKey,
                    DefaultMaxStringLength);
            }

            if (options.Quotas.OperationTimeout == 0)
            {
                options.Quotas.OperationTimeout = GetIntOrDefault(OperationTimeoutKey,
                    DefaultOperationTimeout);
            }

            if (options.Quotas.SecurityTokenLifetime == 0)
            {
                options.Quotas.SecurityTokenLifetime = GetIntOrDefault(SecurityTokenLifetimeKey,
                    DefaultSecurityTokenLifetime);
            }
        }

        /// <inheritdoc/>
        public ClientConfig(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
