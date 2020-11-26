// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.Service.Authentication
{
    using System;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Attribute that can be applied to controllers (or their methods) to enabled basic authentication.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BasicAuthenticationAttribute: TypeFilterAttribute
    {
        public BasicAuthenticationAttribute() : base(typeof(BasicAuthenticationFilter))
        {

        }
    }
}