// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Configuration
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Post configuration base helper class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PostConfigureOptionBase<T> : ConfigureOptionBase,
        IPostConfigureOptions<T> where T : class, new()
    {
        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        protected PostConfigureOptionBase(IConfiguration configuration) :
            base(configuration)
        {
        }

        /// <inheritdoc/>
        public abstract void PostConfigure(string? name, T options);

        /// <summary>
        /// Helper to get options.
        /// </summary>
        public IOptions<T> ToOptions()
        {
            var t = Bind();
            PostConfigure(Options.DefaultName, t);
            return Options.Create(t);
        }

        /// <summary>
        /// Binds an options instance. Concrete configurators override this with a
        /// concrete configuration binding source-generator call or a typed factory.
        /// </summary>
        protected virtual T Bind()
        {
            return new T();
        }
    }
}
