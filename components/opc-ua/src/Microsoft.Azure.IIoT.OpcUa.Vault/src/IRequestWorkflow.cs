// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate Request workflow context
    /// </summary>
    public interface IRequestWorkflow {

        /// <summary>
        /// Fail the request
        /// </summary>
        /// <param name="requestId">The request Id</param>
        /// <param name="errorInfo"></param>
        /// <param name="ct"></param>
        Task FailRequestAsync<T>(string requestId, T errorInfo,
            CancellationToken ct = default);

        /// <summary>
        /// Complete the request
        /// </summary>
        /// <param name="requestId">The request Id</param>
        /// <param name="predicate"></param>
        /// <param name="ct"></param>
        Task CompleteRequestAsync(string requestId,
            Action<CertificateRequestModel> predicate,
            CancellationToken ct = default);
    }
}
