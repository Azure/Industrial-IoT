// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    public class PerDependencyClientAccessor : IClientAccessor {
        public IClient Client { get; }

        public PerDependencyClientAccessor(IClientFactory clientFactory) {
            Client = clientFactory.CreateAsync().Result;
        }

        public void Dispose() {
            Client.Dispose();
        }
    }
}
