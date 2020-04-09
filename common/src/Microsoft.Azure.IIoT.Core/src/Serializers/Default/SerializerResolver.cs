// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers {
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Default resolver
    /// </summary>
    public class SerializerResolver : ISerializerResolver {

        /// <inheritdoc/>
        public IEnumerable<string> Accepted => _cache.Keys;

        /// <summary>
        /// Create resolver
        /// </summary>
        /// <param name="serializers"></param>
        public SerializerResolver(IEnumerable<ISerializer> serializers) {
            _cache = serializers.ToDictionary(s => s.MimeType, s => s);
        }

        /// <inheritdoc/>
        public ISerializer GetSerializer(string mimeType = null) {
            if (mimeType == null) {
                return _cache.FirstOrDefault().Value;
            }
            if (_cache.TryGetValue(mimeType, out var serializer)) {
                return serializer;
            }
            return null;
        }

        private readonly Dictionary<string, ISerializer> _cache;
    }
}