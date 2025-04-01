// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using FluentAssertions;
    using Isa95Jobs;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Simple Events server node tests
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Isa95JobsServerTests<T>
    {
        public Isa95JobsServerTests(Func<INodeServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        private readonly T _connection;
        private readonly Func<INodeServices<T>> _services;
    }
}
