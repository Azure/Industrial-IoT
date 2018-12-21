/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.Security.Cryptography.X509Certificates;
using Opc.Ua.Gds.Server.Database;
using Opc.Ua.Server;

namespace Opc.Ua.Gds.Server
{
    /// <summary>
    /// Implements a sample Global Discovery Server.
    /// </summary>
    /// <remarks>
    /// Each server instance must have one instance of a StandardServer object which is
    /// responsible for reading the configuration file, creating the endpoints and dispatching
    /// incoming requests to the appropriate handler.
    /// 
    /// This sub-class specifies non-configurable metadata such as Product Name and initializes
    /// the ApplicationNodeManager which provides access to the data exposed by the Global Discovery Server.
    /// 
    /// </remarks>
    public class OpcVaultGlobalDiscoveryServer : StandardServer
    {
        public OpcVaultGlobalDiscoveryServer(
            IApplicationsDatabase database,
            ICertificateRequest request,
            ICertificateGroup certificateGroup,
            bool autoApprove = true
            )
        {
            _database = database;
            _request = request;
            _certificateGroup = certificateGroup;
            _autoApprove = autoApprove;
        }

        #region Overridden Methods
        /// <summary>
        /// Initializes the server before it starts up.
        /// </summary>
        /// <remarks>
        /// This method is called before any startup processing occurs. The sub-class may update the 
        /// configuration object or do any other application specific startup tasks.
        /// </remarks>
        protected override void OnServerStarting(ApplicationConfiguration configuration)
        {
            Utils.Trace("The server is starting.");

            base.OnServerStarting(configuration);

            // it is up to the application to decide how to validate user identity tokens.
            // this function creates a validator for X509 identity tokens.
            CreateUserIdentityValidators(configuration);
        }

        /// <summary>
        /// Called after the server has been started.
        /// </summary>
        protected override void OnServerStarted(IServerInternal server)
        {
            base.OnServerStarted(server);

            // request notifications when the user identity is changed. all valid users are accepted by default.
            server.SessionManager.ImpersonateUser += SessionManager_ImpersonateUser;
        }

        /// <summary>
        /// Creates the node managers for the server.
        /// </summary>
        /// <remarks>
        /// This method allows the sub-class create any additional node managers which it uses. The SDK
        /// always creates a CoreNodeManager which handles the built-in nodes defined by the specification.
        /// Any additional NodeManagers are expected to handle application specific nodes.
        /// </remarks>
        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Utils.Trace("Creating the Node Managers.");

            List<INodeManager> nodeManagers = new List<INodeManager>
            {
                // create the custom node managers.
                new ApplicationsNodeManager(server, configuration, _database, _request, _certificateGroup, _autoApprove)
            };

            // create master node manager.
            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }

        /// <summary>
        /// Loads the non-configurable properties for the application.
        /// </summary>
        /// <remarks>
        /// These properties are exposed by the server but cannot be changed by administrators.
        /// </remarks>
        protected override ServerProperties LoadServerProperties()
        {
            ServerProperties properties = new ServerProperties
            {
                ManufacturerName = "Microsoft Corp.",
                ProductName = "Azure Industrial IoT OPC Vault Global Discovery Server",
                ProductUri = "http://azure.microsoft.com/iiot-opcvault/GlobalDiscoveryServer",
                SoftwareVersion = Utils.GetAssemblySoftwareVersion(),
                BuildNumber = Utils.GetAssemblyBuildNumber(),
                BuildDate = Utils.GetAssemblyTimestamp()
            };

            return properties;
        }

        /// <summary>
        /// This method is called at the being of the thread that processes a request.
        /// </summary>
        protected override OperationContext ValidateRequest(RequestHeader requestHeader, RequestType requestType)
        {
            OperationContext context = base.ValidateRequest(requestHeader, requestType);

            if (requestType == RequestType.Write)
            {
                // reject all writes if no user provided.
                if (context.UserIdentity.TokenType == UserTokenType.Anonymous)
                {
                    // construct translation object with default text.
                    TranslationInfo info = new TranslationInfo(
                        "NoWriteAllowed",
                        "en-US",
                        "Must provide a valid user before calling write.");

                    // create an exception with a vendor defined sub-code.
                    throw new ServiceResultException(new ServiceResult(
                        StatusCodes.BadUserAccessDenied,
                        "NoWriteAllowed",
                        Opc.Ua.Gds.Namespaces.OpcUaGds,
                        new LocalizedText(info)));
                }

                UserIdentityToken securityToken = context.UserIdentity.GetIdentityToken();

                // check for a user name token.
                if (securityToken is UserNameIdentityToken userNameToken)
                {
                    lock (_lock)
                    {
                        _contexts.Add(context.RequestId, new ImpersonationContext());
                    }
                }
            }

            return context;
        }

        /// <summary>
        /// This method is called in a finally block at the end of request processing (i.e. called even on exception).
        /// </summary>
        protected override void OnRequestComplete(OperationContext context)
        {
            ImpersonationContext impersonationContext = null;

            lock (_lock)
            {
                if (_contexts.TryGetValue(context.RequestId, out impersonationContext))
                {
                    _contexts.Remove(context.RequestId);
                }
            }

            base.OnRequestComplete(context);
        }

        /// <summary>
        /// Creates the objects used to validate the user identity tokens supported by the server.
        /// </summary>
        private void CreateUserIdentityValidators(ApplicationConfiguration configuration)
        {
            for (int ii = 0; ii < configuration.ServerConfiguration.UserTokenPolicies.Count; ii++)
            {
                UserTokenPolicy policy = configuration.ServerConfiguration.UserTokenPolicies[ii];

                // create a validator for a certificate token policy.
                if (policy.TokenType == UserTokenType.Certificate)
                {
                    // check if user certificate trust lists are specified in configuration.
                    if (configuration.SecurityConfiguration.TrustedUserCertificates != null &&
                        configuration.SecurityConfiguration.UserIssuerCertificates != null)
                    {
                        CertificateValidator certificateValidator = new CertificateValidator();
                        certificateValidator.Update(configuration.SecurityConfiguration).Wait();
                        certificateValidator.Update(
                            configuration.SecurityConfiguration.UserIssuerCertificates,
                            configuration.SecurityConfiguration.TrustedUserCertificates,
                            configuration.SecurityConfiguration.RejectedCertificateStore);

                        // set custom validator for user certificates.
                        _userCertificateValidator = certificateValidator.GetChannelValidator();
                    }
                    break;
                }
            }
        }


        /// <summary>
        /// Called when a client tries to change its user identity.
        /// </summary>
        protected virtual void SessionManager_ImpersonateUser(Session session, ImpersonateEventArgs args)
        {
            // check for a user name token
            if (args.NewIdentity is AnonymousIdentityToken anonymousToken)
            {
                args.Identity = new RoleBasedIdentity(new UserIdentity(), GdsRole.ApplicationUser);
                return;
            }

            // check for a user name token
            if (args.NewIdentity is UserNameIdentityToken userNameToken)
            {
#if UNITTESTONLY
                if (VerifyPassword(userNameToken))
                {
                    switch (userNameToken.UserName)
                    {
                        // Server configuration administrator, manages the GDS server security
                        case "sysadmin":
                            {
                                args.Identity = new SystemConfigurationIdentity(new UserIdentity(userNameToken));
                                Utils.Trace("SystemConfigurationAdmin Token Accepted: {0}", args.Identity.DisplayName);
                                return;
                            }

                        // GDS administrator
                        case "appadmin":
                            {
                                args.Identity = new RoleBasedIdentity(new UserIdentity(userNameToken), GdsRole.ApplicationAdmin);
                                Utils.Trace("ApplicationAdmin Token Accepted: {0}", args.Identity.DisplayName);
                                return;
                            }

                        // GDS user
                        case "appuser":
                            {
                                args.Identity = new RoleBasedIdentity(new UserIdentity(userNameToken), GdsRole.ApplicationUser);
                                Utils.Trace("ApplicationUser Token Accepted: {0}", args.Identity.DisplayName);
                                return;
                            }
                    }
                }
#endif
            }

            // check for x509 user token.
            if (args.NewIdentity is X509IdentityToken x509Token)
            {
                GdsRole role = GdsRole.ApplicationAdmin;
                VerifyUserTokenCertificate(x509Token.Certificate);

                Utils.Trace("X509 Token Accepted: {0} as {1}", args.Identity.DisplayName, role.ToString());
                args.Identity = new RoleBasedIdentity(new UserIdentity(x509Token), role);
                return;
            }

            throw new ServiceResultException(new ServiceResult(StatusCodes.BadUserAccessDenied));
        }

        /// <summary>
        /// Verifies that a certificate user token is trusted.
        /// </summary>
        private void VerifyUserTokenCertificate(X509Certificate2 certificate)
        {
            try
            {
                _userCertificateValidator.Validate(certificate);
            }
            catch (Exception e)
            {
                TranslationInfo info;
                StatusCode result = StatusCodes.BadIdentityTokenRejected;
                if (e is ServiceResultException se && se.StatusCode == StatusCodes.BadCertificateUseNotAllowed)
                {
                    info = new TranslationInfo(
                        "InvalidCertificate",
                        "en-US",
                        "'{0}' is an invalid user certificate.",
                        certificate.Subject);

                    result = StatusCodes.BadIdentityTokenInvalid;
                }
                else
                {
                    // construct translation object with default text.
                    info = new TranslationInfo(
                        "UntrustedCertificate",
                        "en-US",
                        "'{0}' is not a trusted user certificate.",
                        certificate.Subject);
                }

                // create an exception with a vendor defined sub-code.
                throw new ServiceResultException(new ServiceResult(
                    result,
                    info.Key,
                    LoadServerProperties().ProductUri,
                    new LocalizedText(info)));
            }
        }

#if UNITTESTONLY
        private bool VerifyPassword(UserNameIdentityToken userNameToken)
        {
            // TODO: check username/password permissions
            return userNameToken.DecryptedPassword == "demo";
        }
#endif
#endregion

#region Private Fields
        private readonly object _lock = new object();
        private Dictionary<uint, ImpersonationContext> _contexts = new Dictionary<uint, ImpersonationContext>();
        private readonly IApplicationsDatabase _database = null;
        private readonly ICertificateRequest _request = null;
        private readonly ICertificateGroup _certificateGroup = null;
        private readonly bool _autoApprove;
        private X509CertificateValidator _userCertificateValidator;
#endregion
    }
}
