// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Furly.Extensions.Configuration;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.IO;

    /// <summary>
    /// Client's application configuration implementation
    /// </summary>
    public sealed class TestClientConfig : ConfigureOptionBase<OpcUaClientOptions>,
        IDisposable
    {
        public TestClientConfig(IConfiguration configuration) : base(configuration)
        {
            _path = Path.Combine(Directory.GetCurrentDirectory(), "pki",
                    Guid.NewGuid().ToByteArray().ToBase16String());
        }

        /// <inheritdoc/>
        public override void Configure(string? name, OpcUaClientOptions options)
        {
            options.Security.PkiRootPath = _path;
            options.LingerTimeoutDuration = TimeSpan.FromSeconds(20);
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
