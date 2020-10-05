# AAD App Registration

[Home](readme.md)

Components of Azure Industrial IoT solution require several App Registrations in your Azure Active Directory
(AAD) to run. Those App Registrations are responsible for:

* providing identity for back-end microservices to run
* defining authentication methods and details

Both of our deployment methods (`deploy.ps1` script or `Microsoft.Azure.IIoT.Deployment` application) create
those App Registrations for you by default. But they both require user to have `Administrator` role in the AAD
for deployment to succeed. In cases when the user does not have the `Administrator` role, the process of
deployment can be separated into 2 distinct states:

* creation of App Registrations in AAD tenant
* creation of Azure resources and deployment of the Azure Industrial IoT components

With this separation, the first step can be performed by an IT administrator. Deployments will output details
of App Registrations which can then be passed to a user for running the second step.

Based on type of your deployment those will be either two or three App Registrations.

* ServicesApp
* ClientsApp
* App registration for AKS cluster.

## ServicesApp

TBD

## ClientsApp

TBD

## AKS

TBD
