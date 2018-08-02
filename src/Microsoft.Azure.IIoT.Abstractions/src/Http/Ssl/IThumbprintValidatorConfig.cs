// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IIoT.Http.Ssl {

    /// <summary>
    /// Configuration interface
    /// </summary>
    public interface IThumbprintValidatorConfig {

        // The remote endpoint ssl certificate thumbprint the proxy communicates with
        string CertThumbprint { get; }

    }
}
