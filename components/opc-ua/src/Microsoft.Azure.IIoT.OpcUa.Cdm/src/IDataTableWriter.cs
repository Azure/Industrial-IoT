//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Cdm {
    using Microsoft.Azure.IIoT.Storage;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Datatable writer
    /// </summary>
    public interface IDataTableWriter {

        /// <summary>
        /// Access to underlying storage
        /// </summary>
        IFileStorage Storage { get; }

        /// <summary>
        /// write the data to the partition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="drive"></param>
        /// <param name="folder"></param>
        /// <param name="partition"></param>
        /// <param name="data"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        Task<bool> WriteAsync<T>(string drive,
            string folder, string partition, List<T> data,
            string separator);
    }
}
