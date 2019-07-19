// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Signing request processor
    /// </summary>
    public interface ISigningRequestProcessor {

        /// <summary>
        /// Create a new certificate request with CSR.
        /// The CSR is validated and added to the database as new
        /// request.
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="context">The authority Id adding the
        /// request
        /// </param>
        /// <param name="ct"></param>
        /// <returns>The request</returns>
        Task<StartSigningRequestResultModel> StartSigningRequestAsync(
            StartSigningRequestModel request, VaultOperationContextModel context,
            CancellationToken ct = default);

        /// <summary>
        /// Fetch the data of a certificate requests.
        /// Can be used to query the request state and to read an
        /// issued certificate with a private key.
        /// </summary>
        /// <param name="requestId">The request Id</param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns>The request</returns>
        Task<FinishSigningRequestResultModel> FinishSigningRequestAsync(
            string requestId, VaultOperationContextModel context,
            CancellationToken ct = default);
    }
}
