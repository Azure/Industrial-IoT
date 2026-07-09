// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Configuration
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Post configuration base helper class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PostConfigureOptionBase<T> : ConfigureOptionBase,
        IPostConfigureOptions<T> where T : class
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
        /// <remarks>
        /// Binds the whole configuration to <typeparamref name="T"/> via the
        /// reflection-based binder. Because <typeparamref name="T"/> is a generic
        /// type parameter the System.Text.Json/configuration source generators
        /// cannot intercept the bind, so this path is not Native-AOT/trim safe and
        /// is annotated accordingly. In the AOT host, options are instead bound
        /// through the DI options pipeline (IPostConfigure) against concrete types.
        /// </remarks>
        /// <exception cref="InvalidOperationException"></exception>
        [RequiresUnreferencedCode("Binds configuration to T using reflection.")]
        [RequiresDynamicCode("Binds configuration to T using reflection.")]
        public IOptions<T> ToOptions()
        {
            // The configuration binding source generator cannot handle a generic
            // Get<T>() (SYSLIB1104, suppressed project-wide); this reflection
            // fallback is intentional and flagged AOT-unsafe via the Requires*
            // attributes above.
            var t = Configuration.Get<T?>() ?? Activator.CreateInstance<T>();
            if (t is null)
            {
                throw new InvalidOperationException(
                    $"Failed to create option of type {typeof(T)}");
            }
            PostConfigure(Options.DefaultName, t);
            return Options.Create(t);
        }
    }
}
