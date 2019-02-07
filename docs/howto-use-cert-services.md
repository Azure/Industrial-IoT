# Use the Azure Industrial IoT OPC UA Certificate Management Service and dependencies

This article explains how to manage the OPC UA Certificate Management Service in Azure, how to register applications and how to issue signed application certificates for your OPC UA devices.

## Prerequisites

### How to deploy the Certificate Management Service

First of all, the service needs to be deployed to the Azure cloud.
Please find an article describing how to deploy the Certificate Management Service [here](howto-deploy-services.md).

### Create the root CA certificate

1. Open your certificate service at `https://myResourceGroup-app.azurewebsites.net` and login.
2. Navigate to the `Certificate Groups` page.
3. There is one `Default` Certificate Group listed. Click on `Edit`.
4. In `Edit Certificate Group Details` you can modify the Subject Name and Lifetime of your CA and application certificates.
5. Enter a valid Subject in the valid, e.g. `CN=My CA Root, O=MyCompany, OU=MyDepartment`.
6. Click on the `Save` button.
1. If you hit a 'forbidden' error at this point, the user you are logged in with doesn't have the rights to modify or create a new root cert. By default the user who deployed the service has management and signing roles with the service, other users need to be added to the 'Approver', 'Writer' or 'Administrator' roles as appropriate in the AzureAD application registration.
7. Click on the `Details` button. The `View Certificate Group Details` should display the updated information.
8. Click on the `Renew CA Certificate` button to issue your first root CA certificate. Press `Ok` to proceed.
9. After a few seconds the `Certificate Details` are shown. Press `Issuer` or `Crl` to download the latest CA certificate and CRL for distribution to your OPC UA applications.
10. Now the OPC UA Certificate Management Service is ready to issue certificates for OPC UA applications.

## Secure OPC UA applications

### Step 1: Register your OPC UA application 

1. Open your certificate service at `https://myResourceGroup-app.azurewebsites.net` and login.
2. Navigate to the `Register New` page.
1. For an application registration a user needs to have at least the 'Writer' role assigned.
2. The entry form follows naming conventions in the OPC UA world. As an example, in the picture below the settings for the [OPC UA Reference Server](https://github.com/OPCFoundation/UA-.NETStandard/tree/master/SampleApplications/Workshop/Reference) sample in the OPC UA .NetStandard stack is shown:

![UA Reference Server Registration](UAReferenceServerRegistration.png "UA Reference Server Registration")

5. Press the `Register` button to register the application in the certificate service application database. The workflow directly guides the user to the next step to request a signed certificate for the application.

### Step 2: Secure your application with a CA signed application certificate

An OPC UA application can be secured by issueing a signed certificate based on a Certificate Signing
Request (CSR) or by requesting a new key pair, which includes also a new private key in PFX or PEM format. 
Please follow the documentation of your OPC UA device on which method is supported for your application. 
In general, the CSR method is recommended, because it doesn't require a private key to be transferred over a wire.

- If your device supports to issue a Certificate Signing Request (CSR), please follow [Step3a](##Step-3a:).
- If your device requires to issue a new key pair, please follow [Step3b](#Step-3b:).

### Step 3a: Request a new certificate with a new keypair

1. Navigate to the `Applications` page.
3. Start the certificate request workflow by choosing `New Request` for a listed application.

![Request New Certificate](RequestNewCertificate.png "Request New Certificate")

3. Press 'Request new KeyPair and Certificate' to request a private key and a new signed certificate with the public key for your application.

![Generate New Key Pair](GenerateNewKeyPair.png "Generate New Key Pair")

4. Fill in the form with a subject, the domain names and choose PEM or PFX with password for the private key. Press the `Generate New Certificate` button to create the certificate request.

![Approve Certificate](ApproveReject.png "Approve Certificate")

5. The approval step requires a user with Approval role and with Signing rights in Azure KeyVault. In the typical workflow the Approver and Requester role should be assigned to different users.
6. Approve or Reject the certificate request to start or cancel the actual creation of the key pair and the signing operation. The new key pair is created and stored securely in Azure Key Vault until downloaded by the certificate requester. The resulting certificate with public key is signed by the CA. These operations may take a few seconds to finish.

![View Key Pair](ViewKeyPair.png "View Key Pair")

7. The resulting private key (PFX or PEM) and certificate (DER) can be downloaded from here in the format selected as binary file download. A base64 encoded version is also available, e.g. to copy paste the certificate to a command line or text entry. 
8. Once the private key is downloaded and stored securely, it can be deleted from the service with the `Delete Private Key` button. The certificate with public key remains available for future use.
9. Due to the use of a CA signed certificate, the CA cert and CRL should be downloaded here as well.
10. Now it depends on the OPC UA device how to apply the new key pair. Typically, the CA cert and CRL are copied to a `trusted` folder, while the public and private key of the application certificate is applied to a `own` folder in the certificate store. Some devices may already support 'Server Push' for Certificate updates. Please refer to the documentation of your OPC UA device.

### Step 3b: Request a new certificate with a CSR 

1. Navigate to the `Applications` page.
3. Start the certificate request workflow by choosing `New Request` for a listed application.

![Request New Certificate](RequestNewCertificate.png "Request New Certificate")

3. Press 'Request new Certificate with signing request' to request a new signed certificate for your application.

![Generate New Certificate](GenerateNewCertificate.png "Generate New Certificate")

4. Upload CSR by selecting a local file or by pasting a base64 encoded CSR in the form. Press the `Generate New Certificate` button to create the certificate request.

![Approve CSR](ApproveRejectCSR.png "Approve CSR")

5. Approve or Reject the certificate request to start or cancel the actual signing operation. The resulting certificate with public key is signed by the CA. This operation may take a few seconds to finish.

![View Certificate](ViewCertCSR.png "View Certificate")

6. The resulting certificate (DER) can be downloaded from here as binary file. A base64 encoded version is also available, e.g. to copy paste the certificate to a command line or text entry. 
10. Once the certificate is downloaded and stored securely, it can be deleted from the service with the `Delete Certificate` button.
11. Due to the use of a CA signed certificate, the CA cert and CRL should be downloaded here as well.
12. Now it depends on the OPC UA device how to apply the new certificate. Typically, the CA cert and CRL are copied to a `trusted` folder, while the application certificate is applied to a `own` folder in the certificate store. Some devices may already support 'Server Push' for Certificate updates. Please refer to the documentation of your OPC UA device.

### Step 4: Device secured

The OPC UA device is now ready to communicate with other OPC UA devices secured by CA signed certifcates without further configuration. Enjoy!
