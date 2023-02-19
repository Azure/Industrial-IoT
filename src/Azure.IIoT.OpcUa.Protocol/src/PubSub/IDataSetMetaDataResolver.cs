// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    /// <summary>
    /// Resolve metadata during encoding and decoding
    /// </summary>
    public interface IDataSetMetaDataResolver {

        /// <summary>
        /// Find data set metadata or return null if not found.
        /// </summary>
        /// <param name="writerId"></param>
        /// <param name="majorVersion"></param>
        /// <param name="minorVersion"></param>
        /// <returns></returns>
        DataSetMetaDataType Find(ushort writerId,
            uint majorVersion = 0, uint minorVersion = 0);
    }
}