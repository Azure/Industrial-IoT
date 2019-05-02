// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.Auth.Clients;
using Microsoft.Azure.IIoT.Exceptions;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test.Helpers;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Types;
using Microsoft.Extensions.Configuration;
using Opc.Ua.Test;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using TestCaseOrdering;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test
{

    public class CertificateRequestTestFixture : IDisposable
    {
        private readonly IClientConfig _clientConfig = new ClientConfig();
        private readonly IDocumentDBRepository _documentDBRepository;
        private readonly ServicesConfig _serviceConfig = new ServicesConfig();
        private readonly string _configId;
        private readonly string _groupId;
        private readonly KeyVaultCertificateGroup _keyVaultCertificateGroup;
        private readonly ILogger _logger;
        public IApplicationsDatabase ApplicationsDatabase;
        public ICertificateGroup CertificateGroup;
        public ICertificateRequest CertificateRequest;
        public IList<ApplicationTestData> ApplicationTestSet;
        public ApplicationTestDataGenerator RandomGenerator;
        public bool RegistrationOk;

        const int _randomStart = 1234;
        const int _testSetSize = 10;

        public CertificateRequestTestFixture()
        {
            RandomGenerator = new ApplicationTestDataGenerator(_randomStart);
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("testsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            IConfigurationRoot configuration = builder.Build();
            configuration.Bind("OpcVault", _serviceConfig);
            configuration.Bind("Auth", _clientConfig);
            _logger = SerilogTestLogger.Create<CertificateRequestTestFixture>();
            if (!InvalidConfiguration())
            {
                _documentDBRepository = new OpcVaultDocumentDbRepository(_serviceConfig);
                ApplicationsDatabase = CosmosDBApplicationsDatabaseFactory.Create(null, _serviceConfig, _documentDBRepository, _logger);

                var timeid = (DateTime.UtcNow.ToFileTimeUtc() / 1000) % 10000;
                _groupId = "CertReqIssuerCA" + timeid.ToString();
                _configId = "CertReqConfig" + timeid.ToString();
                var keyVaultServiceClient = KeyVaultServiceClient.Get(_configId, _serviceConfig, _clientConfig, _logger);
                _keyVaultCertificateGroup = new KeyVaultCertificateGroup(keyVaultServiceClient, _serviceConfig, _clientConfig, _logger);
                _keyVaultCertificateGroup.PurgeAsync(_configId, _groupId).Wait();
                CertificateGroup = _keyVaultCertificateGroup;
                CertificateGroup = new KeyVaultCertificateGroup(keyVaultServiceClient, _serviceConfig, _clientConfig, _logger);
                CertificateGroup.CreateCertificateGroupConfiguration(_groupId, "CN=OPC Vault Cert Request Test CA, O=Microsoft, OU=Azure IoT", null).Wait();
                CertificateRequest = CosmosDBCertificateRequestFactory.Create(ApplicationsDatabase, CertificateGroup, _serviceConfig, _documentDBRepository, _logger);

                // create test set
                ApplicationTestSet = new List<ApplicationTestData>();
                for (int i = 0; i < _testSetSize; i++)
                {
                    var randomApp = RandomGenerator.RandomApplicationTestData();
                    ApplicationTestSet.Add(randomApp);
                }
                // try initialize DB
                ApplicationsDatabase.Initialize().Wait();
            }
            RegistrationOk = false;
        }

        public void Dispose()
        {
            _keyVaultCertificateGroup?.PurgeAsync(_configId, _groupId).Wait();
        }

        public void SkipOnInvalidConfiguration()
        {
            Skip.If(InvalidConfiguration(), "Missing valid CosmosDB or KeyVault configuration.");
        }

        private bool InvalidConfiguration()
        {
            return
                _serviceConfig.KeyVaultBaseUrl == null ||
                _serviceConfig.KeyVaultResourceId == null ||
                _clientConfig.AppId == null ||
                _clientConfig.AppSecret == null ||
                _serviceConfig.CosmosDBCollection == null ||
                _serviceConfig.CosmosDBDatabase == null ||
                _serviceConfig.CosmosDBEndpoint == null ||
                _serviceConfig.CosmosDBToken == null
                ;
        }

    }

    [TestCaseOrderer("TestCaseOrdering.PriorityOrderer", "Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test")]
    public class CertificateRequestTest : IClassFixture<CertificateRequestTestFixture>
    {
        CertificateRequestTestFixture _fixture;
        private ILogger _logger;
        private IApplicationsDatabase _applicationsDatabase;
        private ICertificateGroup _certificateGroup;
        private ICertificateRequest _certificateRequest;
        private IList<ApplicationTestData> _applicationTestSet;
        private RandomSource _randomSource;

        public CertificateRequestTest(CertificateRequestTestFixture fixture, ITestOutputHelper log)
        {
            _fixture = fixture;
            // fixture
            fixture.SkipOnInvalidConfiguration();
            _logger = SerilogTestLogger.Create<CertificateRequestTest>(log);
            _applicationsDatabase = fixture.ApplicationsDatabase;
            _certificateGroup = fixture.CertificateGroup;
            _certificateRequest = fixture.CertificateRequest;
            _applicationTestSet = fixture.ApplicationTestSet;
            _randomSource = new RandomSource(10815);
        }

        /// <summary>
        /// Test to clean the database from collisions with the test set.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(100)]
        private async Task CleanupAllApplications()
        {
            _logger.Information("Cleanup All Applications");
            foreach (var application in _applicationTestSet)
            {
                var applicationModelList = await _applicationsDatabase.ListApplicationAsync(application.Model.ApplicationUri);
                Assert.NotNull(applicationModelList);
                foreach (var response in applicationModelList)
                {
                    try
                    {
                        await _applicationsDatabase.DeleteApplicationAsync(response.ApplicationId.ToString(), true);
                    }
                    catch { }
                }
            }
            _fixture.RegistrationOk = false;
        }

        /// <summary>
        /// Test to register all applications in the test set.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(200)]
        private async Task RegisterAllApplications()
        {
            foreach (var application in _applicationTestSet)
            {
                var applicationModel = await _applicationsDatabase.RegisterApplicationAsync(application.Model);
                Assert.NotNull(applicationModel);
                Assert.NotEqual(applicationModel.ApplicationId, Guid.Empty);
                ApplicationTestData.AssertEqualApplicationModelData(applicationModel, application.Model);
                application.Model = applicationModel;
                Assert.NotNull(applicationModel);
            }
            _fixture.RegistrationOk = true;
        }

        /// <summary>
        /// Initialize certificate request class.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(190)]
        private async Task InitCertificateRequestAndGroup()
        {
            await _certificateRequest.Initialize();
            string[] groups = await _certificateGroup.GetCertificateGroupIds();
            foreach (var group in groups)
            {
                await _certificateGroup.CreateIssuerCACertificateAsync(group);
                var chain = await _certificateGroup.GetIssuerCACertificateChainAsync(group);
                Assert.NotNull(chain);
                Assert.True(chain.Count > 0);
            }
        }

        /// <summary>
        /// Approve all applications, the valid state for cert requests.
        /// </summary>
        /// <remarks>After this test all applications are in the approved state.</remarks>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(500)]
        private async Task ApproveAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                // approve app
                var applicationModel = await _applicationsDatabase.ApproveApplicationAsync(application.Model.ApplicationId.ToString(), true, true);
                Assert.NotNull(applicationModel);
                Assert.Equal(ApplicationState.Approved, applicationModel.ApplicationState);
            }
        }

        /// <summary>
        /// Create a NewKeyPair request for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(1000)]
        private async Task NewKeyPairRequestAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            int count = 0;
            foreach (var application in _applicationTestSet)
            {
                string[] groups = await _certificateGroup.GetCertificateGroupIds();
                foreach (var group in groups)
                {
                    var applicationId = application.Model.ApplicationId.ToString();
                    string requestId = await _certificateRequest.StartNewKeyPairRequestAsync(
                        applicationId,
                        group,
                        null,
                        application.Subject,
                        application.DomainNames.ToArray(),
                        application.PrivateKeyFormat,
                        application.PrivateKeyPassword,
                        "unittest@opcvault.com");
                    Assert.NotNull(requestId);
                    // read request
                    var request = await _certificateRequest.ReadAsync(requestId);
                    Assert.Equal(CertificateRequestState.New, request.State);
                    Assert.Equal(requestId, request.RequestId);
                    Assert.Equal(applicationId, request.ApplicationId);
                    Assert.False(request.SigningRequest);
                    Assert.True(Opc.Ua.Utils.CompareDistinguishedName(application.Subject, request.SubjectName));
                    Assert.Equal(group, request.CertificateGroupId);
                    //Assert.Equal(null, fullRequest.CertificateTypeId);
                    Assert.Equal(application.DomainNames.ToArray(), request.DomainNames);
                    Assert.Equal(application.PrivateKeyFormat, request.PrivateKeyFormat);

                    // TODO: test all fields
                    application.RequestIds.Add(requestId);
                    count++;
                }
            }
            Assert.True(count > 0);
        }

        /// <summary>
        /// Create a Certificate Signing Request for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(1000)]
        private async Task SigningRequestAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            int count = 0;
            foreach (var application in _applicationTestSet)
            {
                string[] groups = await _certificateGroup.GetCertificateGroupIds();
                foreach (var group in groups)
                {
                    var applicationId = application.Model.ApplicationId.ToString();
                    var certificateGroupConfiguration = await _certificateGroup.GetCertificateGroupConfiguration(group);
                    X509Certificate2 csrCertificate = CertificateFactory.CreateCertificate(
                        null, null, null,
                        application.ApplicationRecord.ApplicationUri,
                        null,
                        application.Subject,
                        application.DomainNames.ToArray(),
                        certificateGroupConfiguration.DefaultCertificateKeySize,
                        DateTime.UtcNow.AddDays(-20),
                        certificateGroupConfiguration.DefaultCertificateLifetime,
                        certificateGroupConfiguration.DefaultCertificateHashSize
                        );
                    byte[] csr = CertificateFactory.CreateSigningRequest(
                        csrCertificate,
                        application.DomainNames);
                    string requestId = await _certificateRequest.StartSigningRequestAsync(
                        applicationId,
                        group,
                        null,
                        csr,
                        "unittest@opcvault.com");
                    Assert.NotNull(requestId);
                    // read request
                    var request = await _certificateRequest.ReadAsync(requestId);
                    Assert.Equal(CertificateRequestState.New, request.State);
                    Assert.Null(request.PrivateKeyFormat);
                    Assert.True(request.SigningRequest);
                    Assert.Equal(requestId, request.RequestId);
                    Assert.Equal(applicationId, request.ApplicationId);
                    Assert.Null(request.SubjectName);
                    Assert.Equal(group, request.CertificateGroupId);
                    //Assert.Equal(null, fullRequest.CertificateTypeId);
                    //Assert.Equal(application.DomainNames.ToArray(), fullRequest.DomainNames);
                    Assert.Null(request.PrivateKeyFormat);
                    // add to list
                    application.RequestIds.Add(requestId);
                    count++;
                }
            }
            Assert.True(count > 0);
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after StartRequests.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(1400)]
        private async Task FetchRequestsAfterStartAllApplications()
        {
            await FetchRequestsAllApplications();
        }


        /// <summary>
        /// Read certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(1500)]
        private async Task ReadRequestsAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                foreach (var requestId in application.RequestIds)
                {
                    var request = await _certificateRequest.ReadAsync(requestId);
                    Assert.Equal(CertificateRequestState.New, request.State);
                }
            }
        }


        /// <summary>
        /// Approve or reject certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(2000)]
        private async Task ApproveRequestsAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                foreach (var requestId in application.RequestIds)
                {
                    var request = await _certificateRequest.ReadAsync(requestId);
                    Assert.Equal(CertificateRequestState.New, request.State);
                    // approve/reject 50% randomly
                    bool reject = _randomSource.NextInt32(100) > 50;
                    await _certificateRequest.ApproveAsync(requestId, reject);
                    request = await _certificateRequest.ReadAsync(requestId);
                    Assert.Equal(reject ? CertificateRequestState.Rejected : CertificateRequestState.Approved, request.State);
                }
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Approve.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(2000)]
        private async Task FetchRequestsAfterApproveAllApplications()
        {
            await FetchRequestsAllApplications();
        }

        /// <summary>
        /// Accept the certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(3000)]
        private async Task AcceptRequestsAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                foreach (var requestId in application.RequestIds)
                {
                    var appModel = application.Model;
                    var applicationId = application.Model.ApplicationId.ToString();
                    var request = await _certificateRequest.ReadAsync(requestId);
                    if (request.State == CertificateRequestState.Approved)
                    {
                        await _certificateRequest.AcceptAsync(requestId);
                        request = await _certificateRequest.ReadAsync(requestId);
                        Assert.Equal(CertificateRequestState.Accepted, request.State);
                    }
                    else
                    {
                        await Assert.ThrowsAsync<ResourceInvalidStateException>(async () =>
                        {
                            await _certificateRequest.AcceptAsync(requestId);
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Approve.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(3100)]
        private async Task FetchRequestsAfterAcceptAllApplications()
        {
            await FetchRequestsAllApplications();
        }


        /// <summary>
        /// Delete the certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(4000)]
        private async Task DeleteRequestsHalfApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                foreach (var requestId in application.RequestIds)
                {
                    if (_randomSource.NextInt32(100) > 50)
                    {
                        var request = await _certificateRequest.ReadAsync(requestId);
                        if (request.State == CertificateRequestState.New ||
                            request.State == CertificateRequestState.Rejected ||
                            request.State == CertificateRequestState.Approved ||
                            request.State == CertificateRequestState.Accepted)
                        {
                            await _certificateRequest.DeleteAsync(requestId);
                        }
                        else
                        {
                            await Assert.ThrowsAsync<ResourceInvalidStateException>(async () =>
                            {
                                await _certificateRequest.DeleteAsync(requestId);
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Approve.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(4500)]
        private async Task FetchRequestsAfterDeleteHalfApplications()
        {
            await FetchRequestsAllApplications();
        }


        /// <summary>
        /// Revoke the certificate requests for all deleted applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5000)]
        private async Task RevokeRequestsAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                foreach (var requestId in application.RequestIds)
                {
                    var request = await _certificateRequest.ReadAsync(requestId);
                    if (request.State == CertificateRequestState.Deleted)
                    {
                        await _certificateRequest.RevokeAsync(requestId);
                    }
                }
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Approve.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5100)]
        private async Task FetchRequestsAfterRevokeAllApplications()
        {
            await FetchRequestsAllApplications();
        }


        /// <summary>
        /// Delete the certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5400)]
        private async Task DeleteRequestsAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                foreach (var requestId in application.RequestIds)
                {
                    var request = await _certificateRequest.ReadAsync(requestId);
                    if (request.State == CertificateRequestState.New ||
                        request.State == CertificateRequestState.Rejected ||
                        request.State == CertificateRequestState.Approved ||
                        request.State == CertificateRequestState.Accepted)
                    {
                        await _certificateRequest.DeleteAsync(requestId);
                    }
                    else
                    {
                        await Assert.ThrowsAsync<ResourceInvalidStateException>(async () =>
                        {
                            await _certificateRequest.DeleteAsync(requestId);
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Approve.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5500)]
        private async Task FetchRequestsAfterDeleteAllApplications()
        {
            await FetchRequestsAllApplications();
        }


        /// <summary>
        /// RevokeGroup the certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5600)]
        private async Task RevokeGroupRequestsAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            string[] groups = await _certificateGroup.GetCertificateGroupIds();
            foreach (var group in groups)
            {
                await _certificateRequest.RevokeGroupAsync(group, true);
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Approve.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5700)]
        private async Task FetchRequestsAfterRevokeGroupAllApplications()
        {
            await FetchRequestsAllApplications();
        }


        /// <summary>
        /// Purge the certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5800)]
        private async Task PurgeRequestsAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                foreach (var requestId in application.RequestIds)
                {
                    var request = await _certificateRequest.ReadAsync(requestId);
                    if (request.State == CertificateRequestState.Revoked ||
                        request.State == CertificateRequestState.Rejected ||
                        request.State == CertificateRequestState.Removed ||
                        request.State == CertificateRequestState.New)
                    {
                        await _certificateRequest.PurgeAsync(requestId);
                    }
                    else
                    {
                        await Assert.ThrowsAsync<ResourceInvalidStateException>(async () =>
                        {
                            await _certificateRequest.PurgeAsync(requestId);
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Purge.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5900)]
        private async Task FetchRequestsAfterPurgeAllApplications()
        {
            await FetchRequestsAllApplications(true);
        }

        /// <summary>
        /// Unregister all applications, clean up test set.
        /// </summary>
        /// <returns></returns>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(6000)]
        private async Task UnregisterAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                var applicationModel = await _applicationsDatabase.UnregisterApplicationAsync(application.Model.ApplicationId.ToString());
                Assert.NotNull(applicationModel);
                Assert.NotEqual(applicationModel.ApplicationId, Guid.Empty);
            }
        }

        /// <summary>
        /// Delete the application test set.
        /// </summary>
        /// <returns></returns>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(9000)]
        private async Task DeleteAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                await _applicationsDatabase.DeleteApplicationAsync(application.Model.ApplicationId.ToString(), false);
            }
        }


        /// <summary>
        /// Test helper to test fetch for various states of requests in the workflow.
        /// </summary>
        private async Task FetchRequestsAllApplications(bool purged = false)
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                foreach (var requestId in application.RequestIds)
                {
                    var appModel = application.Model;
                    var applicationId = application.Model.ApplicationId.ToString();
                    if (purged)
                    {
                        await Assert.ThrowsAsync<ResourceNotFoundException>(async () =>
                        {
                            await _certificateRequest.FetchRequestAsync(requestId, applicationId);
                        });
                        continue;
                    }
                    var fetchResult = await _certificateRequest.FetchRequestAsync(requestId, applicationId);
                    Assert.Equal(requestId, fetchResult.RequestId);
                    Assert.Equal(applicationId, fetchResult.ApplicationId);
                    if (fetchResult.State == CertificateRequestState.Approved ||
                        fetchResult.State == CertificateRequestState.Accepted)
                    {
                        if (fetchResult.PrivateKey != null)
                        {
                            Assert.Equal(application.PrivateKeyFormat, fetchResult.PrivateKeyFormat);
                        }
                        Assert.NotNull(fetchResult.SignedCertificate);
                        if (fetchResult.PrivateKey != null)
                        {
                            Assert.NotNull(fetchResult.PrivateKey);
                        }
                        else
                        {
                            Assert.Null(fetchResult.PrivateKey);
                        }
                    }
                    else
                    if (fetchResult.State == CertificateRequestState.Revoked ||
                        fetchResult.State == CertificateRequestState.Deleted)
                    {
                        Assert.Null(fetchResult.PrivateKey);
                    }
                    else if (fetchResult.State == CertificateRequestState.Rejected ||
                        fetchResult.State == CertificateRequestState.New ||
                        fetchResult.State == CertificateRequestState.Removed
                        )
                    {
                        Assert.Null(fetchResult.PrivateKey);
                        Assert.Null(fetchResult.SignedCertificate);
                    }
                    else
                    {
                        Assert.True(false, "Invalid State");
                    }
                }
            }
        }
    }


}
