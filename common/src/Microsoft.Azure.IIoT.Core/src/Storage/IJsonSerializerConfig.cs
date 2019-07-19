// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using Newtonsoft.Json;

    /// <summary>
    /// Json serializer configuration
    /// </summary>
    public interface IJsonSerializerConfig {

        /// <summary>
        /// Serializer settings
        /// </summary>
        JsonSerializerSettings Serializer { get; }
    }
}
