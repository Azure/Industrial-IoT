// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to bulk document items
    /// </summary>
    public interface IDocumentLoader : IDisposable {

        /// <summary>
        /// Add document
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="doc"></param>
        /// <returns></returns>
        Task AddAsync<T>(T doc);

        /// <summary>
        /// Complete or abort processing
        /// </summary>
        /// <param name="abort"></param>
        /// <returns></returns>
        Task CompleteAsync(bool abort = false);
    }
}
