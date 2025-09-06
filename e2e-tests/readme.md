# E2E test

## Background
Poperties of E2E tests:
* black-box
* end-to-end
* high cost tests (long preparation / execution times)

Goal of E2E tests:
* check the customer point of view (validate)
* cover the most important scenarios

## How to write E2E tests
### Preparation
Use the `TestCaseOrderer` attribute with `TestCaseOrderer.FullName` parameter on
your test class, and `PriorityOrder` on your test methods to order them.

Use the `Collection` attribute on your test class to share the context between 
test methods.

Use the `Trait` attribute on your test class to distinguish between orchestrated and standalone mode:
* for orchestrated mode use the `Trait` with the name `TestConstants.TraitConstants.PublisherModeTraitName` and the value `TestConstants.TraitConstants.PublisherModeOrchestratedTraitValue`,
* for standalone mode use the `Trait` with the name `TestConstants.TraitConstants.PublisherModeTraitName` and the value `TestConstants.TraitConstants.PublisherModeStandaloneTraitValue`.

The test class gets the context as a parameter of its constructor.
Use one of the following context types:
* `IIoTPlatformTestContext` - for orchestrated mode
* `IIoTStandaloneTestContext` - for standalone mode

In order for the context to log information the `OutputHelper` of the context needs to be set to the `IOutputHelper` the test class gets as a constructor parameter.

If necessary you can clean the context by calling its `Reset` method, otherwise it's state is shared between test methods, and even between test classes of the same `Collection`. It is recommended to call `Reset` on the context in the first test method of a test class.

All test collections should start by setting the desired mode as a first step. E.g.<br>
`await TestHelper.SwitchToOrchestratedModeAsync(_context);`

Use and extend the `TestHelper` class.

`TestEventProcessor` listens to IoT Hub and analyzes the value changes.

## Executing tests in Visual Studio

You can reuse a test deployment to speed up test development.
Follow these steps:
* Start new E2E pipeline build with `Cleanup` variable set to `false`.
* Find the `ResourceGroupName` in the pipeline logs.
* Navigate to the given resource group on the Azure portal.
* Change tags:
  * add or edit tag named `owner` where the value identifies you,
  * add the tag `DoNotDelete` with the value `true`.
* Add yourself to the KeyVault:
  * open "Access policies" on the azure portal,
  * click "Add Access Policy",
  * at "Configure from template" select "Key & Secret Management",
  * at "Select principal" select your principal,
  * press the "Add" button,
  * press the "Save" button.
* Execute /tools/e2etesting/GetSecrets.ps1 -KeyVaultName &lt;YourKeyVaultName&gt;. You will be asked whether you want to overwrite the .env file or not.
  * if you choose 'yes' just wait for the script to finish and no further steps are needed
  * if you choose 'no' copy the script output to *reporoot*/e2etests/.env
    > The .env file is excluded from git, but double check you are not committing secrets.
* Now you can use Visual Studio to execute your tests.
* Don't forget to clean up by executing the pipeline with these variables set:
  * `Cleanup = false`
  * `UseExisting = true`
  * `ResourceGroupName = $[format('<your_resource_group_name>')]`

### To debug publisher messages and message validation issues

* Stop the app service instance in the resource group.
* Start the TestEventProcessor service from the tools\e2etesting\TestEventProcessor\TestEventProcessor.slnx solution file
* Change the .env file generated from KeyVault and change the TESTEVENTPROCESSOR_* variables to the default values found
  in the TestEventProcessor's appsettings.json and launchSettings.json files.
    "TESTEVENTPROCESSOR_BASEURL": "https://localhost:49450",
    "TESTEVENTPROCESSOR_USERNAME": "username",
    "TESTEVENTPROCESSOR_PASSWORD": "password",
* Start the unit test (development)