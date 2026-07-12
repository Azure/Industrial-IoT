// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Core.Configuration;
    using Azure.IIoT.OpcUa.Core.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.IO;

    /// <summary>
    /// Client's application configuration implementation
    /// </summary>
    public sealed class TestClientConfig : ConfigureOptionBase<OpcUaClientOptions>,
        IDisposable
    {
        /// <summary>
        /// Configuration key used by server fixtures to provide an isolated PKI root.
        /// </summary>
        public const string PkiRootPathKey = "TestClient:PkiRootPath";

        public TestClientConfig(IConfiguration configuration) : base(configuration)
        {
            _path = configuration[PkiRootPathKey] ?? configuration["PkiRootPath"] ??
                Path.Combine(Path.GetTempPath(), "opcua-test-client-pki",
                    Guid.NewGuid().ToByteArray().ToBase16String());
        }

        /// <inheritdoc/>
        public override void Configure(string? name, OpcUaClientOptions options)
        {
            options.Security.PkiRootPath = _path;
            options.Security.AutoAcceptUntrustedCertificates = true;
            options.DefaultConnectTimeoutDuration = TimeSpan.FromMinutes(2);
            options.DefaultServiceCallTimeoutDuration = TimeSpan.FromMinutes(2);
            options.CreateSessionTimeoutDuration = TimeSpan.FromMinutes(1);
            options.LingerTimeoutDuration = TimeSpan.FromSeconds(5);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Directory.Exists(_path))
            {
                Try.Op(() => Directory.Delete(_path, true));
            }
        }

        private readonly string _path;
    }
}
