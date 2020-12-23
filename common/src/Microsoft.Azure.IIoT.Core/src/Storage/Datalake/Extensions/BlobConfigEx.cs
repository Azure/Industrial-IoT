// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Datalake {
    using Microsoft.Azure.IIoT.Utils;

    /// <summary>
    /// Blob Storage configuration extension
    /// </summary>
    public static class BlobConfigEx {

        /// <summary>
        /// Get blob storage connection string
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GetStorageConnString(this IBlobConfig config) {
            var account = config.AccountName;
            var key = config.AccountKey;
            var suffix = config.EndpointSuffix;
            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(key)) {
                return null;
            }
            return ConnectionString.CreateStorageConnectionString(
                account, suffix, key, "https").ToString();
        }
    }
}
