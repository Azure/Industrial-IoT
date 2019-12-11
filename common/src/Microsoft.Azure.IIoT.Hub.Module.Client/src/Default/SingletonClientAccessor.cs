// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    public class SingletonClientAccessor : IClientAccessor {

        public SingletonClientAccessor(IClient client) {
            Client = client;
        }

        public IClient Client { get; }

        public void Dispose() {
            Client.Dispose();
        }
    }
}
