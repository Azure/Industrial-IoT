// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Filters
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    /// <summary>
    /// Triggers authentication if access token cannot be acquired
    /// silently, i.e. from cache.
    /// </summary>
    public class AdalTokenAcquisitionExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            //If ADAL failed to acquire access token
            if (context.Exception is AdalSilentTokenAcquisitionException)
            {
                //Send user to Azure AD to re-authenticate
                context.Result = new ChallengeResult();
            }
        }
    }
}
