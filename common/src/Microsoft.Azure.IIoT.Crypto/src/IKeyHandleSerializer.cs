// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Can serialize and deserialize a key handle for 
    /// storage.
    /// </summary>
    public interface IKeyHandleSerializer {

        /// <summary>
        /// Serialize a handle to json
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        JToken SerializeHandle(KeyHandle handle);

        /// <summary>
        /// Deserialize a handle from json
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        KeyHandle DeserializeHandle(JToken token);
    }
}