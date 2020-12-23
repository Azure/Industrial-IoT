// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers {
    using System.Collections.Generic;

    /// <summary>
    /// Formats content using a mime type serializer
    /// </summary>
    public interface ISerializerResolver {

        /// <summary>
        /// Accepted / supported mime types
        /// </summary>
        IEnumerable<string> Accepted { get; }

        /// <summary>
        /// Get a serializer and filter optionally on
        /// mime type
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        ISerializer GetSerializer(string mimeType = null);
    }
}