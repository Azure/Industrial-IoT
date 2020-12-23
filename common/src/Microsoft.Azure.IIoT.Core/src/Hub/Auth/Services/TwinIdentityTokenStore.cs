// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Auth {
    using Microsoft.Azure.IIoT.Hub.Auth.Models;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Twin as identity token store
    /// </summary>
    public class TwinIdentityTokenStore : IIdentityTokenStore {

        /// <summary>
        /// Create token store
        /// </summary>
        /// <param name="iotHubTwinServices"></param>
        public TwinIdentityTokenStore(IIoTHubTwinServices iotHubTwinServices) {
            _iotHubTwinServices = iotHubTwinServices;
        }

        /// <inheritdoc/>
        public async Task<IdentityTokenModel> GetIdentityTokenAsync(string identity) {
            var deviceId = GetDeviceId(identity, out var moduleId);
            var deviceTwin = await _iotHubTwinServices.GetAsync(deviceId, moduleId);
            if (deviceTwin == null) {
                throw new ResourceNotFoundException(identity);
            }
            if (!deviceTwin.Properties.Desired.ContainsKey(Constants.IdentityTokenPropertyName)) {
                throw new IdentityTokenNotFoundException(identity);
            }
            var property = deviceTwin.Properties.Desired[Constants.IdentityTokenPropertyName];
            var identityToken = ConvertFromVariantValue(property, identity);
            if (string.IsNullOrWhiteSpace(identityToken?.Key)) {
                throw new IdentityTokenInvalidException(identity);
            }
            return identityToken;
        }

        /// <summary>
        /// Split idenitity into device and module id
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        private string GetDeviceId(string identity, out string moduleId) {
            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }
            var values = identity.Split('/');
            moduleId = values.Length == 2 ? values[1] : null;
            return values[0];
        }

        /// <summary>
        /// Convert json token to identity token
        /// </summary>
        /// <param name="json"></param>
        /// <param name="identity"></param>
        /// <returns></returns>
        private static IdentityTokenModel ConvertFromVariantValue(VariantValue json,
            string identity) {
            try {
                var identityToken = json.ConvertTo<IdentityTokenTwinModel>();
                return identityToken?.ToServiceModel();
            }
            catch (Exception ex) {
                throw new IdentityTokenInvalidException(identity, ex);
            }
        }

        private readonly IIoTHubTwinServices _iotHubTwinServices;
    }
}