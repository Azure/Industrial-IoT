//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Cdm {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// CDM's ADLS g2 storage interface
    /// </summary>
    public interface IAdlsStorage : IDisposable {

        /// <summary>
        /// write the data in a csv partition format
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="partitionUrl"></param>
        /// <param name="data"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        Task WriteInCsvPartition<T>(string partitionUrl, List<T> data, string separator);

        /// <summary>
        /// creates the storage root folder if not already existing 
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="blobName"></param>
        /// <param name="rootFolder"></param>
        /// <returns></returns>
        Task CreateBlobRoot(string hostName, string blobName, string rootFolder);
    }
}
