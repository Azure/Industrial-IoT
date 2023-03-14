// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Furly.Extensions.Configuration;
    using Furly.Tunnel.Router.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Configure the method router
    /// </summary>
    public sealed class MethodRouterConfig : PostConfigureOptionBase<RouterOptions>
    {
        /// <summary>
        /// Create configuration
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="options"></param>
        public MethodRouterConfig(IConfiguration configuration,
            IOptions<PublisherOptions> options) : base(configuration)
        {
            _options = options;
        }

        /// <inheritdoc/>
        public override void PostConfigure(string name, RouterOptions options)
        {
            if (options.MountPoint == null)
            {
                options.MountPoint = new TopicBuilder(_options).MethodTopic;
            }
        }

        private readonly IOptions<PublisherOptions> _options;
    }
}
