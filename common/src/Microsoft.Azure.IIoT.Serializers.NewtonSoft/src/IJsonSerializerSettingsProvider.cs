// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers {
    using Newtonsoft.Json;

    /// <summary>
    /// Json serializer settings provider
    /// </summary>
    public interface IJsonSerializerSettingsProvider {

        /// <summary>
        /// Serializer settings
        /// </summary>
        JsonSerializerSettings Settings { get; }
    }
}
