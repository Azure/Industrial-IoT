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
        public const string KeepAliveIntervalKey = "KeepAliveInterval";
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
        public const string ReconnectRetryDelayKey = "ReconnectRetryDelay";

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
        public const int DefaultSessionTimeoutDefaultSec = 60;
        public const int KeepAliveIntervalDefaultSec = 10;
        public const int ReconnectRetryDelayDefaultSec = 5;
        public const int MinimumCertificateKeySizeDefault = 1024;
        public const bool AutoAcceptUntrustedCertificatesDefault = false;
        public const bool RejectSha1SignedCertificatesDefault = false;
        public const bool AddAppCertToTrustedStoreDefault = true;
        public const bool RejectUnknownRevocationStatusDefault = true;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void PostConfigure(string name, ClientOptions options)
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

            if (options.ReconnectRetryDelay == null)
            {
                var reconnectRetryDelay = GetIntOrDefault(ReconnectRetryDelayKey,
                    ReconnectRetryDelayDefaultSec);
                if (reconnectRetryDelay > 0)
                {
                    options.ReconnectRetryDelay = TimeSpan.FromSeconds(reconnectRetryDelay);
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
        }

        /// <inheritdoc/>
        public ClientConfig(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
