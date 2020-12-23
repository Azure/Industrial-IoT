// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services.SecureData {
    using Microsoft.AspNetCore.DataProtection;
    using Newtonsoft.Json;
    using System;

    public class SecureData {

        /// <summary>
        /// Create SecureData
        /// </summary>
        /// <param name="provider"></param>
        public SecureData(IDataProtectionProvider provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider)); ;
            _protector = _provider.CreateProtector(GetType().FullName);
        }

        /// <summary>
        /// Unprotect and Deserialize data
        /// </summary>
        /// <param name="data"></param>
        /// <returns>T</returns>
        public T UnprotectDeserialize<T>(string data) {
            var serializedData = string.Empty;
            if (data != null) {
                serializedData = _protector.Unprotect(data);
            }
            return JsonConvert.DeserializeObject<T>(serializedData);
        }

        /// <summary>
        /// Protect and Serialize data
        /// </summary>
        /// <param name="data"></param>
        /// <returns>string</returns>
        public string ProtectSerialize<T>(T data) {
            var serializedData = JsonConvert.SerializeObject(data);
            return _protector.Protect(serializedData);
        }

        private readonly IDataProtector _protector;
        private readonly IDataProtectionProvider _provider;
    }
}

