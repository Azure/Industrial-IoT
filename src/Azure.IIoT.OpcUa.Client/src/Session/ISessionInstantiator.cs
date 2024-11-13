/* ========================================================================
 * Copyright (c) 2005-2023 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Object that creates an instance of a Session object.
    /// It can be used to subclass enhanced Session
    /// classes which survive reconnect handling etc.
    /// </summary>
    public interface ISessionInstantiator
    {
        /// <summary>
        /// Constructs a new instance of the <see cref="Session"/> class.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="configuration"></param>
        /// <param name="endpoint"></param>
        /// <param name="clientCertificate"></param>
        /// <param name="sessionName"></param>
        /// <param name="sessionTimeout"></param>
        /// <param name="identity"></param>
        /// <param name="preferredLocales"></param>
        /// <param name="checkDomain"></param>
        /// <param name="availableEndpoints"></param>
        /// <param name="discoveryProfileUris"></param>
        /// <returns></returns>
        Session Create(ITransportChannel? channel,
            ApplicationConfiguration configuration, ConfiguredEndpoint endpoint,
            X509Certificate2? clientCertificate, string sessionName,
            TimeSpan sessionTimeout, IUserIdentity? identity,
            IReadOnlyList<string>? preferredLocales, bool checkDomain,
            EndpointDescriptionCollection? availableEndpoints = null,
            StringCollection? discoveryProfileUris = null);
    }
}
