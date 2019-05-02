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
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TestCaseOrdering;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test
{

    public class ApplicationDatabaseTestFixture : IDisposable
    {
        private readonly IClientConfig _clientConfig = new ClientConfig();
        private readonly IDocumentDBRepository _documentDBRepository;
        private readonly ILogger _logger;
        private ServicesConfig _serviceConfig = new ServicesConfig();
        private IConfigurationRoot _configuration;
        public IApplicationsDatabase ApplicationsDatabase;
        public IList<ApplicationTestData> ApplicationTestSet;
        public ApplicationTestDataGenerator RandomGenerator;
        public bool RegistrationOk;

        const int _randomStart = 3388;
        const int _testSetSize = 10;

        public ApplicationDatabaseTestFixture()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("testsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            _configuration = builder.Build();
            _configuration.Bind("OpcVault", _serviceConfig);
            _configuration.Bind("Auth", _clientConfig);
            _logger = SerilogTestLogger.Create<ApplicationDatabaseTestFixture>();
            if (!InvalidConfiguration())
            {
                RandomGenerator = new ApplicationTestDataGenerator(_randomStart);
                _documentDBRepository = new OpcVaultDocumentDbRepository(_serviceConfig);
                ApplicationsDatabase = CosmosDBApplicationsDatabaseFactory.Create(null, _serviceConfig, _documentDBRepository, _logger);
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
        }

        public void SkipOnInvalidConfiguration()
        {
            Skip.If(InvalidConfiguration(), "Missing valid CosmosDB configuration.");
        }

        private bool InvalidConfiguration()
        {
            return
            _serviceConfig.CosmosDBCollection == null ||
            _serviceConfig.CosmosDBDatabase == null ||
            _serviceConfig.CosmosDBEndpoint == null ||
            _serviceConfig.CosmosDBToken == null
            ;
        }

    }

    [TestCaseOrderer("TestCaseOrdering.PriorityOrderer", "Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test")]
    public class ApplicationDatabaseTest : IClassFixture<ApplicationDatabaseTestFixture>
    {
        private readonly ApplicationDatabaseTestFixture _fixture;
        private readonly ILogger _logger;
        private readonly IApplicationsDatabase _applicationsDatabase;
        private readonly IList<ApplicationTestData> _applicationTestSet;

        public ApplicationDatabaseTest(ApplicationDatabaseTestFixture fixture, ITestOutputHelper log)
        {
            _fixture = fixture;
            // fixture
            _fixture.SkipOnInvalidConfiguration();
            _logger = SerilogTestLogger.Create<ApplicationDatabaseTest>(log);
            _applicationsDatabase = fixture.ApplicationsDatabase;
            _applicationTestSet = fixture.ApplicationTestSet;
        }

        /// <summary>
        /// Test to register all applications in the test set.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(200)]
        private async Task RegisterAllApplications()
        {
            foreach (var application in _applicationTestSet)
            {
                Assert.Null(application.Model.CreateTime);
                Assert.Null(application.Model.ApproveTime);
                Assert.Null(application.Model.UpdateTime);
                Assert.Null(application.Model.DeleteTime);
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
        private async Task RegisterAllApplicationsThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _applicationsDatabase.RegisterApplicationAsync(null);
            });
            await Assert.ThrowsAsync<ResourceNotFoundException>(async () =>
            {
                var testModelCopy = ApplicationTestData.ApplicationDeepCopy(_applicationTestSet[0].Model);
                testModelCopy.ApplicationId = Guid.NewGuid();
                await _applicationsDatabase.RegisterApplicationAsync(testModelCopy);
            });
        }

        /// <summary>
        /// Test the approve reject state machine.
        /// </summary>
        /// <remarks>After this test all applications are in the approved state.</remarks>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(500)]
        private async Task ApproveAllApplications()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _applicationsDatabase.ApproveApplicationAsync(null, false, false);
            });

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _applicationsDatabase.ApproveApplicationAsync(Guid.Empty.ToString(), false, false);
            });

            Skip.If(!_fixture.RegistrationOk);
            int fullPasses = 0;
            foreach (var application in _applicationTestSet)
            {
                // read model to get state
                var applicationModel = await _applicationsDatabase.GetApplicationAsync(application.Model.ApplicationId.ToString());
                if (applicationModel.ApplicationState == ApplicationState.New)
                {
                    // approve app
                    applicationModel = await _applicationsDatabase.ApproveApplicationAsync(application.Model.ApplicationId.ToString(), true, false);
                    Assert.NotNull(applicationModel);
                    Assert.Equal(ApplicationState.Approved, applicationModel.ApplicationState);
                }

                // verify start condition
                if (applicationModel.ApplicationState != ApplicationState.Approved)
                {
                    continue;
                }

                // reject approved app should fail
                await Assert.ThrowsAsync<ResourceInvalidStateException>(async () =>
                {
                    await _applicationsDatabase.ApproveApplicationAsync(application.Model.ApplicationId.ToString(), false, false);
                });

                // force approved app to rejected state
                applicationModel = await _applicationsDatabase.ApproveApplicationAsync(application.Model.ApplicationId.ToString(), false, true);
                Assert.NotNull(applicationModel);
                Assert.NotEqual(applicationModel.ApplicationId, Guid.Empty);
                Assert.Equal(ApplicationState.Rejected, applicationModel.ApplicationState);
                ApplicationTestData.AssertEqualApplicationModelData(applicationModel, application.Model);

                // approve rejected app should fail
                await Assert.ThrowsAsync<ResourceInvalidStateException>(async () =>
                {
                    await _applicationsDatabase.ApproveApplicationAsync(application.Model.ApplicationId.ToString(), true, false);
                });

                // force approve of rejected app
                applicationModel = await _applicationsDatabase.ApproveApplicationAsync(application.Model.ApplicationId.ToString(), true, true);
                Assert.NotNull(applicationModel);
                Assert.NotEqual(applicationModel.ApplicationId, Guid.Empty);
                Assert.Equal(ApplicationState.Approved, applicationModel.ApplicationState);
                ApplicationTestData.AssertEqualApplicationModelData(applicationModel, application.Model);
                fullPasses++;
            }
            // not enough test passes to verify
            Skip.If(fullPasses < _applicationTestSet.Count / 2);
        }

        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(1000)]
        private async Task GetAllApplications()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _applicationsDatabase.GetApplicationAsync(null);
            });
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _applicationsDatabase.GetApplicationAsync(Guid.Empty.ToString());
            });
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _applicationsDatabase.GetApplicationAsync("abc");
            });
            await Assert.ThrowsAsync<ResourceNotFoundException>(async () =>
            {
                await _applicationsDatabase.GetApplicationAsync(Guid.NewGuid().ToString());
            });
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                var applicationModel = await _applicationsDatabase.GetApplicationAsync(application.Model.ApplicationId.ToString());
                Assert.NotNull(applicationModel);
                Assert.NotEqual(applicationModel.ApplicationId, Guid.Empty);
                Assert.Equal(applicationModel.ApplicationId, applicationModel.ApplicationId);
                ApplicationTestData.AssertEqualApplicationModelData(applicationModel, application.Model);
            }
        }

        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(1500)]
        private async Task ListAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                var applicationModelList = await _applicationsDatabase.ListApplicationAsync(application.Model.ApplicationUri);
                Assert.NotNull(applicationModelList);
                Assert.True(applicationModelList.Count >= 1, "At least one record should be found.");
                foreach (var response in applicationModelList)
                {
                    Assert.NotEqual(response.ApplicationId, Guid.Empty);
                    Assert.Equal(application.Model.ApplicationUri, response.ApplicationUri);
                }
            }
        }

        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(2100)]
        private async Task UpdateAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                var applicationModel = await _applicationsDatabase.UpdateApplicationAsync(application.Model.ApplicationId.ToString(), application.Model);
                Assert.NotNull(applicationModel);
                Assert.NotEqual(applicationModel.ApplicationId, Guid.Empty);
                Assert.Equal(applicationModel.ApplicationId, applicationModel.ApplicationId);
            }
        }

        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(2200)]
        private async Task QueryApplicationsByIdAsync()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                var response = await _applicationsDatabase.QueryApplicationsByIdAsync(0, 0, null, null, 0, null, null, QueryApplicationState.Any);
                Assert.NotNull(response);
            }
        }

        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(2300)]
        private async Task QueryApplicationsAsync()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                var response = await _applicationsDatabase.QueryApplicationsAsync(null, null, 0, null, null, QueryApplicationState.Any);
                Assert.NotNull(response);
            }
        }

        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(8000)]
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

        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(8100)]
        private async Task UnregisteredListAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                var applicationModelList = await _applicationsDatabase.ListApplicationAsync(application.Model.ApplicationUri);
                Assert.NotNull(applicationModelList);
                foreach (var response in applicationModelList)
                {
                    Assert.Equal(application.Model.ApplicationUri, response.ApplicationUri);
                    Assert.NotEqual(response.ApplicationId, application.Model.ApplicationId);
                }
            }
        }

        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(8200)]
        private async Task UnregisteredGetAllApplicationsNot()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                var applicationModel = await _applicationsDatabase.GetApplicationAsync(application.Model.ApplicationId.ToString());
                Assert.NotNull(applicationModel);
                Assert.NotEqual(applicationModel.ApplicationId, Guid.Empty);
                Assert.Equal(applicationModel.ApplicationId, applicationModel.ApplicationId);
                Assert.Equal(ApplicationState.Unregistered, applicationModel.ApplicationState);
                Assert.NotNull(applicationModel.DeleteTime);
                ApplicationTestData.AssertEqualApplicationModelData(applicationModel, application.Model);
            }
        }

        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(9000)]
        private async Task DeleteAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                await _applicationsDatabase.DeleteApplicationAsync(application.Model.ApplicationId.ToString(), false);
            }
        }

        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(9200)]
        private async Task DeletedGetAllApplications()
        {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet)
            {
                await Assert.ThrowsAsync<ResourceNotFoundException>(async () =>
                {
                    var applicationModel = await _applicationsDatabase.GetApplicationAsync(application.Model.ApplicationId.ToString());
                });
            }
        }

    }


}
