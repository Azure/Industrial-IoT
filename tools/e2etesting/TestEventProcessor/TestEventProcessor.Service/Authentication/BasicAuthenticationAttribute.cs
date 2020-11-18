using System;
using Microsoft.AspNetCore.Mvc;

namespace TestEventProcessor.Service.Authentication
{
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