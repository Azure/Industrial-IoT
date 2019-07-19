// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Key pair generation services - can be turned off for secure operations.
    /// </summary>
    public interface IKeyPairRequestProcessor {

        /// <summary>
        /// Create a new certificate request with a public/private
        /// key pair.
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="context">The authority submitting the
        /// request</param>
        /// <param name="ct"></param>
        /// <returns>The request id</returns>
        Task<StartNewKeyPairRequestResultModel> StartNewKeyPairRequestAsync(
            StartNewKeyPairRequestModel request, VaultOperationContextModel context,
            CancellationToken ct = default);

        /// <summary>
        /// Fetch the data of a new key pair requests.
        /// Can be used to query the request state and to read an
        /// issued certificate with a private key.
        /// </summary>
        /// <param name="requestId">The request Id</param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns>The request</returns>
        Task<FinishNewKeyPairRequestResultModel> FinishNewKeyPairRequestAsync(
            string requestId, VaultOperationContextModel context,
            CancellationToken ct = default);
    }
}
