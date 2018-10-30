// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
using Mono.Options;
using Opc.Ua.Configuration;
using Opc.Ua.Gds.Server.Database.OpcVault;
using Opc.Ua.Gds.Server.OpcVault;
using Opc.Ua.Server;

namespace Opc.Ua.Gds.Server
{
    public class ApplicationMessageDlg : IApplicationMessageDlg
    {
        private string message = string.Empty;
        private bool ask = false;

        public override void Message(string text, bool ask)
        {
            this.message = text;
            this.ask = ask;
        }

        public override async Task<bool> ShowAsync()
        {
            if (ask)
            {
                message += " (y/n, default y): ";
                Console.Write(message);
            }
            else
            {
                Console.WriteLine(message);
            }
            if (ask)
            {
                try
                {
                    ConsoleKeyInfo result = Console.ReadKey();
                    Console.WriteLine();
                    return await Task.FromResult((result.KeyChar == 'y') || (result.KeyChar == 'Y') || (result.KeyChar == '\r'));
                }
                catch
                {
                    // intentionally fall through
                }
            }
            return await Task.FromResult(true);
        }
    }

    public enum ExitCode : int
    {
        Ok = 0,
        ErrorServerNotStarted = 0x80,
        ErrorServerRunning = 0x81,
        ErrorServerException = 0x82,
        ErrorInvalidCommandLine = 0x100
    };

    public class Program
    {

        public static string Name = "Azure Industrial IoT OPC UA Global Discovery Server";

        public static int Main(string[] args)
        {
            Console.WriteLine(Name);

            // command line options
            bool showHelp = false;
            var opcVaultOptions = new OpcVaultApiOptions();
            var azureADOptions = new OpcVaultAzureADOptions();

            Mono.Options.OptionSet options = new Mono.Options.OptionSet {
                { "v|vault=", "OpcVault Url", g => opcVaultOptions.BaseAddress = g },
                { "r|resource=", "OpcVault Resource Id", r => opcVaultOptions.ResourceId = r },
                { "c|clientid=", "AD Client Id", c => azureADOptions.ClientId = c },
                { "s|secret=", "AD Client Secret", s => azureADOptions.ClientSecret = s },
                { "a|authority", "Authority", a => azureADOptions.Authority = a },
                { "t|tenantid", "Tenant Id", t => azureADOptions.TenantId = t },
                { "h|help", "show this message and exit", h => showHelp = h != null },
            };

            try
            {
                IList<string> extraArgs = options.Parse(args);
                foreach (string extraArg in extraArgs)
                {
                    Console.WriteLine("Error: Unknown option: {0}", extraArg);
                    showHelp = true;
                }
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                showHelp = true;
            }

            if (showHelp)
            {
                Console.WriteLine("Usage: dotnet Microsoft.Azure.IIoT.OpcUa.Modules.Vault.dll [OPTIONS]");
                Console.WriteLine();

                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return (int)ExitCode.ErrorInvalidCommandLine;
            }

            var server = new VaultGlobalDiscoveryServer();
            server.Run(opcVaultOptions, azureADOptions);

            return (int)VaultGlobalDiscoveryServer.ExitCode;
        }
    }

    public class VaultGlobalDiscoveryServer
    {
        OpcVaultGlobalDiscoveryServer server;
        Task status;
        DateTime lastEventTime;
        static ExitCode exitCode;

        public VaultGlobalDiscoveryServer()
        {
        }

        public void Run(
            OpcVaultApiOptions opcVaultOptions,
            OpcVaultAzureADOptions azureADOptions)
        {

            try
            {
                exitCode = ExitCode.ErrorServerNotStarted;
                ConsoleGlobalDiscoveryServer(opcVaultOptions, azureADOptions).Wait();
                Console.WriteLine("Server started. Press Ctrl-C to exit...");
                exitCode = ExitCode.ErrorServerRunning;
            }
            catch (Exception ex)
            {
                Utils.Trace("ServiceResultException:" + ex.Message);
                Console.WriteLine("Exception: {0}", ex.Message);
                exitCode = ExitCode.ErrorServerException;
                return;
            }

            ManualResetEvent quitEvent = new ManualResetEvent(false);
            try
            {
                Console.CancelKeyPress += (sender, eArgs) =>
                {
                    quitEvent.Set();
                    eArgs.Cancel = true;
                };
            }
            catch
            {
            }

            // wait for timeout or Ctrl-C
            quitEvent.WaitOne();

            if (server != null)
            {
                Console.WriteLine("Server stopped. Waiting for exit...");

                using (OpcVaultGlobalDiscoveryServer _server = server)
                {
                    // Stop status thread
                    server = null;
                    status.Wait();
                    // Stop server and dispose
                    _server.Stop();
                }
            }

            exitCode = ExitCode.Ok;
        }

        public static ExitCode ExitCode => exitCode;

        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                // GDS accepts any client certificate
                e.Accept = true;
                Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
            }
        }

        private async Task ConsoleGlobalDiscoveryServer(
            OpcVaultApiOptions opcVaultOptions,
            OpcVaultAzureADOptions azureADOptions)
        {
            ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
            ApplicationInstance application = new ApplicationInstance
            {
                ApplicationName = Program.Name,
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = "Microsoft.Azure.IIoT.OpcUa.Modules.Vault"
            };

            // load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false);

            // check the application certificate.
            bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0);
            if (!haveAppCertificate)
            {
                throw new Exception("Application instance certificate invalid!");
            }

            if (!config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
            }

            // get the DatabaseStorePath configuration parameter.
            GlobalDiscoveryServerConfiguration opcVaultConfiguration = config.ParseExtension<GlobalDiscoveryServerConfiguration>();

            // extract appId and vault name from database storage path
            string[] keyVaultConfig = opcVaultConfiguration.DatabaseStorePath?.Split(',');
            if (keyVaultConfig != null)
            {
                if (String.IsNullOrEmpty(opcVaultOptions.BaseAddress))
                {
                    // try configuration using XML config
                    opcVaultOptions.BaseAddress = keyVaultConfig[0];
                }

                if (String.IsNullOrEmpty(opcVaultOptions.ResourceId))
                {
                    if (keyVaultConfig.Length > 1 && !String.IsNullOrEmpty(keyVaultConfig[1]))
                    {
                        opcVaultOptions.ResourceId = keyVaultConfig[1];
                    }
                }

                if (String.IsNullOrEmpty(azureADOptions.ClientId))
                {
                    if (keyVaultConfig.Length > 2 && !String.IsNullOrEmpty(keyVaultConfig[2]))
                    {
                        azureADOptions.ClientId = keyVaultConfig[2];
                    }
                }

                if (String.IsNullOrEmpty(azureADOptions.ClientSecret))
                {
                    if (keyVaultConfig.Length > 3 && !String.IsNullOrEmpty(keyVaultConfig[3]))
                    {
                        azureADOptions.ClientSecret = keyVaultConfig[3];
                    }
                }

                if (String.IsNullOrEmpty(azureADOptions.TenantId))
                {
                    if (keyVaultConfig.Length > 4 && !String.IsNullOrEmpty(keyVaultConfig[4]))
                    {
                        azureADOptions.TenantId = keyVaultConfig[4];
                    }
                }

                if (String.IsNullOrEmpty(azureADOptions.Authority))
                {
                    if (keyVaultConfig.Length > 5 && !String.IsNullOrEmpty(keyVaultConfig[5]))
                    {
                        azureADOptions.Authority = keyVaultConfig[5];
                    }
                }

            }

            var serviceClient = new OpcVaultLoginCredentials(opcVaultOptions, azureADOptions);
            IOpcVault opcVaultServiceClient = new Microsoft.Azure.IIoT.OpcUa.Api.Vault.OpcVault(new Uri(opcVaultOptions.BaseAddress), serviceClient);
            var opcVaultHandler = new OpcVaultClientHandler(opcVaultServiceClient);

            // read configurations from OpcVault secret
            opcVaultConfiguration.CertificateGroups = await opcVaultHandler.GetCertificateConfigurationGroupsAsync(opcVaultConfiguration.BaseCertificateGroupStorePath);
            UpdateGDSConfigurationDocument(config.Extensions, opcVaultConfiguration);

            var certGroup = new OpcVaultCertificateGroup(opcVaultHandler);
            var requestDB = new OpcVaultCertificateRequest(opcVaultServiceClient);
            var appDB = new OpcVaultApplicationsDatabase(opcVaultServiceClient);

            requestDB.Initialize();
            // for UNITTEST set auto approve true
            server = new OpcVaultGlobalDiscoveryServer(appDB, requestDB, certGroup, false);

            // start the server.
            await application.Start(server);

            // print endpoint info
            var endpoints = application.Server.GetEndpoints().Select(e => e.EndpointUrl).Distinct();
            foreach (var endpoint in endpoints)
            {
                Console.WriteLine(endpoint);
            }

            // start the status thread
            status = Task.Run(new Action(StatusThread));

            // print notification on session events
            server.CurrentInstance.SessionManager.SessionActivated += EventStatus;
            server.CurrentInstance.SessionManager.SessionClosing += EventStatus;
            server.CurrentInstance.SessionManager.SessionCreated += EventStatus;

        }

        /// <summary>
        /// Updates the config extension with the new configuration information.
        /// </summary>
        private static void UpdateGDSConfigurationDocument(XmlElementCollection extensions, GlobalDiscoveryServerConfiguration gdsConfiguration)
        {
            XmlDocument gdsDoc = new XmlDocument();
            var qualifiedName = EncodeableFactory.GetXmlName(typeof(GlobalDiscoveryServerConfiguration));
            XmlSerializer gdsSerializer = new XmlSerializer(typeof(GlobalDiscoveryServerConfiguration), qualifiedName.Namespace);
            using (XmlWriter writer = gdsDoc.CreateNavigator().AppendChild())
            {
                gdsSerializer.Serialize(writer, gdsConfiguration);
            }

            foreach (var extension in extensions)
            {
                if (extension.Name == qualifiedName.Name)
                {
                    extension.InnerXml = gdsDoc.DocumentElement.InnerXml;
                }
            }
        }


        private void EventStatus(Session session, SessionEventReason reason)
        {
            lastEventTime = DateTime.UtcNow;
            PrintSessionStatus(session, reason.ToString());
        }

        void PrintSessionStatus(Session session, string reason, bool lastContact = false)
        {
            lock (session.DiagnosticsLock)
            {
                string item = String.Format("{0,9}:{1,20}:", reason, session.SessionDiagnostics.SessionName);
                if (lastContact)
                {
                    item += String.Format("Last Event:{0:HH:mm:ss}", session.SessionDiagnostics.ClientLastContactTime.ToLocalTime());
                }
                else
                {
                    if (session.Identity != null)
                    {
                        item += String.Format(":{0,20}", session.Identity.DisplayName);
                    }
                    item += String.Format(":{0}", session.Id);
                }
                Console.WriteLine(item);
            }
        }

        private async void StatusThread()
        {
            while (server != null)
            {
                if (DateTime.UtcNow - lastEventTime > TimeSpan.FromMilliseconds(6000))
                {
                    IList<Session> sessions = server.CurrentInstance.SessionManager.GetSessions();
                    for (int ii = 0; ii < sessions.Count; ii++)
                    {
                        Session session = sessions[ii];
                        PrintSessionStatus(session, "-Status-", true);
                    }
                    lastEventTime = DateTime.UtcNow;
                }
                await Task.Delay(1000);
            }
        }
    }
}
