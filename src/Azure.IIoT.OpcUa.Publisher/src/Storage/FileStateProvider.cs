// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Storage
{
    using Azure.IIoT.OpcUa.Publisher;
    using Furly.Extensions.Serializers;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// File state provider
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class FileStateProvider<T> : IStateProvider<T>
    {
        /// <summary>
        /// Create file based state provider
        /// </summary>
        /// <param name="serializer"></param>
        public FileStateProvider(IJsonSerializer serializer)
        {
            _serializer = serializer;
            _fileName = string.Empty;
        }

        /// <inheritdoc/>
        public ValueTask StoreAsync(string key, T value, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public  ValueTask<T> LoadAsync(string key, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        private readonly IJsonSerializer _serializer;
        private readonly string _fileName;
    }
}
