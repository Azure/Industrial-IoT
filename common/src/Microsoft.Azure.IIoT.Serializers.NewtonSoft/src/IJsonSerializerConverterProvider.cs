// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Converter provider
    /// </summary>
    public interface IJsonSerializerConverterProvider {

        /// <summary>
        /// Gets a streaming context to use
        /// </summary>
        StreamingContext Context { get; }

        /// <summary>
        /// Get converters
        /// </summary>
        /// <returns></returns>
        IEnumerable<JsonConverter> GetConverters();
    }
}
