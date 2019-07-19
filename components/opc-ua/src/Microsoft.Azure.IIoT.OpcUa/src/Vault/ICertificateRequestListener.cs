// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate request change listener
    /// </summary>
    public interface ICertificateRequestListener {

        /// <summary>
        /// Called when request is added
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task OnCertificateRequestSubmittedAsync(
            CertificateRequestModel request);

        /// <summary>
        /// Called when request is activated
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task OnCertificateRequestApprovedAsync(
            CertificateRequestModel request);

        /// <summary>
        /// Called when request is complete or failed
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task OnCertificateRequestCompletedAsync(
            CertificateRequestModel request);

        /// <summary>
        /// Called when request is accepted by requestor
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task OnCertificateRequestAcceptedAsync(
            CertificateRequestModel request);

        /// <summary>
        /// Called when request is deleted
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task OnCertificateRequestDeletedAsync(
            CertificateRequestModel request);
    }
}
