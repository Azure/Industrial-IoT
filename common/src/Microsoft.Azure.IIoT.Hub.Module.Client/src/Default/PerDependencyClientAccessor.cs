// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {

    /// <summary>
    /// Class that creates a new client from the IClientFactory each time it is instantiated.
    /// </summary>
    public class PerDependencyClientAccessor : IClientAccessor {

        /// <inheritdoc/>
        public IClient Client { get; }

        /// <summary>
        /// Creates a new instance of the PerDependencyClientAccessor.
        /// In the constructor, a new instance of IClient is
        /// requested from the IClientFactory.
        /// </summary>
        /// <param name="clientFactory"></param>
        public PerDependencyClientAccessor(IClientFactory clientFactory) {
            Client = clientFactory.CreateAsync().Result;
        }

        /// <inheritdoc/>
        public void Dispose() {
            Client.Dispose();
        }
    }
}
