# AAD App Registration  <!-- omit in toc -->

[Home](readme.md)

## Table Of Contents <!-- omit in toc -->

* [Introduction](#introduction)
* [PowerShell scripts](#powershell-scripts)
* [`Microsoft.Azure.IIoT.Deployment`](#microsoftazureiiotdeployment)

## Introduction

Components of Azure Industrial IoT solution require several App Registrations in your Azure Active Directory
(AAD) to run. Those App Registrations are responsible for:

* providing identity for back-end microservices to run
* defining authentication methods and details for client applications

Both of our deployment methods (`deploy.ps1` script or `Microsoft.Azure.IIoT.Deployment` application) create
those App Registrations for you by default. But they both require user to have `Administrator` role in the AAD
for deployment to succeed. In cases when the user does not have the `Administrator` role, the process of
deployment can be separated into 2 distinct states:

* creation and setup of App Registrations in AAD tenant
* creation of Azure resources and deployment of the Azure Industrial IoT platform components

With this separation, the first step can be performed by an IT administrator. Deployments will output details
of App Registrations which can then be passed to a user for running the second step.

## PowerShell scripts

`deploy.ps1` PowerShell script calls `aad-register.ps1` for creating the following App Registrations:

* `<application-name>-service`: used for providing identity for back-end microservices
* `<application-name>-client`: used for authentication of native clients, such as CLI tool
* `<application-name>-web`: used for authentication of Web clients, such as Swagger

Use the following command to run `aad-register.ps1` script for creation of App Registrations:

```pwsh
cd deploy/scripts
./aad-register.ps1 -Name <application-name> -ReplyUrl https://<application-name>.azurewebsites.net/ -Output aad.json
```

The output of the script is a JSON file containing the relevant information to be used as part of the
deployment and must be passed to the `deploy.ps1` script in the same folder using `-aadConfig` argument.

Use the following command to deploy Azure Industrial IoT platform with pre-created App Registrations:

```pwsh
./deploy.ps1 -aadConfig aad.json -applicationName <application-name>
```

## `Microsoft.Azure.IIoT.Deployment`

`deploy.ps1` script creates the following App Registrations:

* `<application-name>-service`: used for providing identity for back-end microservices
* `<application-name>-client`: used for authentication of native and Web clients, such as CLI tool or Swagger
* `<application-name>-aks`: used for providing identity for AKS cluster

Use the following command to run `Microsoft.Azure.IIoT.Deployment` application for only App Registration creation:

```bash
```
