// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Autofac {

    /// <summary>
    /// Inject extra components
    /// </summary>
    public interface IInjector {

        /// <summary>
        /// Injects extra components into the container
        /// </summary>
        /// <param name="builder"></param>
        void Inject(ContainerBuilder builder);
    }
}
