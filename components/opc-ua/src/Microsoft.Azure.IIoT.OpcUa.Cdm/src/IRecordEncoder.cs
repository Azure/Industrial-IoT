// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cdm {
    using System.Collections.Generic;

    /// <summary>
    /// Record encoder
    /// </summary>
    public interface IRecordEncoder {

        /// <summary>
        /// Encode each line in the table
        /// </summary>
        /// <param name="data"></param>
        /// <param name="separator"></param>
        /// <param name="addHeader"></param>
        /// <returns></returns>
        byte[] Encode<T>(List<T> data, string separator,
            bool addHeader = false);
    }
}
