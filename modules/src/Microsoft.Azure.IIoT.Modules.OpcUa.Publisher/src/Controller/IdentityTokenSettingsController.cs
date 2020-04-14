// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Controller {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
    using System.Threading.Tasks;

    /// <summary>
    /// Controller
    /// </summary>
    [Version(1)]
    [Version(2)]
    public class IdentityTokenSettingsController : ISettingsController,
        IIdentityTokenProvider {

        /// <summary>
        /// Get identity token
        /// </summary>
        public IdentityTokenApiModel IdentityToken {
            get => _token ?? new IdentityTokenApiModel();
            set => _token = value;
        }

        /// <inheritdoc/>
        [Ignore]
        IdentityTokenModel IIdentityTokenProvider.IdentityToken =>
            _token?.ToServiceModel() ?? new IdentityTokenModel();

        /// <inheritdoc/>
        public Task ForceUpdate() {
            // TODO
            return Task.CompletedTask;
        }

        private IdentityTokenApiModel _token;
    }
}