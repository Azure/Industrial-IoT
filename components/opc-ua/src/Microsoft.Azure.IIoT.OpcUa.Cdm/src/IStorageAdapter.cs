//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cdm {
    using Microsoft.CommonDataModel.ObjectModel.Storage;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Storage adapter
    /// </summary>
    public interface IStorageAdapter {

        /// <summary>
        /// Storage adapter to mount
        /// </summary>
        StorageAdapterBase Adapter { get; }

        /// <summary>
        /// Lock file
        /// </summary>
        /// <param name="corpusPath"></param>
        /// <returns></returns>
        Task LockAsync(string corpusPath);

        /// <summary>
        /// write the data to the corpus
        /// </summary>
        /// <param name="corpusPath"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        Task WriteAsync(string corpusPath,
            Func<bool, byte[]> writer);

        /// <summary>
        /// Unlock file
        /// </summary>
        /// <param name="corpusPath"></param>
        /// <returns></returns>
        Task UnlockAsync(string corpusPath);
    }
}
