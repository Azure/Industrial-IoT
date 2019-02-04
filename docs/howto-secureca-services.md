# How to run the the Certificate Management Service securely

This article explains how to manage the OPC UA Certificate Management Service securely in Azure and other guidelines to consider.

## Roles

### Trusted and authorized roles must be defined

The Service is configured to allow for distinct roles to access various parts of the service.

**Important Note:** During deployment, the OPC Vault only adds the user who runs the deployment script as a user for all roles.
This role assignment should be reviewed for a production deployment and reconfigured appropriately following the guidelines below.
This task requires manual assignment of roles and services in the Azure AD Enterprise Applications portal.

### Certificate Management Service Roles

The service defines the following roles:

- **Reader**: By default any authenticated user in the tenant has read access. 
  - Read access to applications and certificate requests. Can list and query for applications and certificate requests. Also device discovery information and public certificates are accessible with read access.
- **Writer**: The writer role is assigned to a user to add write permissions for certain tasks. 
  - Read/Write access to applications and certificate requests. Can register, update and unregister applications. Can create certificate requests and obtain approved private keys and certificates. Can also delete private keys.
- **Approver**: The approver role is assigned to a user to approve or reject certificate requests. The role does not include any other role.
  - In addition to the Approver role to access the microservice the user must also have signing rights in Key Vault to be able to sign the certificates.
  - The Writer and Approver role should be assigned to different users.
  - Ideally, Writer, Approver and Administrator role are assigned to different users. For additional security, a user with Approver role needs also Signing rights in KeyVault to issue certificates or to renew an Issuer CA certificate.
  - The main role of the Approver is the Approval of the generation and rejection of certificate requests.
- **Administrator**: The administrator role is assigned to a user to manage the certificate groups. The role does not support the Approver role, but includes the Writer role.
  - The administrator can manage the certificate groups, change the configuration and revoke application certificates by issueing a new CRL.
  - In addition to the service role, his role includes but is not limited to:
    1. Responsible for administering the implementation of the CA’s security practices.
    2. Management of the generation, revocation, and suspension of certificates. 
    3. Cryptographic key life cycle management (e.g. the renewal of the Issuer CA keys).
    4. Installation, configuration, and maintenance of services that operate the CA.
    5. Day-to-day operation of the services. 
    6. CA and database backup and recovery.

### Other role assignments

The following roles should also be considered and assigned when running the service:

- Business owner of the certificate procurement contract with the external Root certification authority 
(in the case when the owner purchases certificates from an external CA or operates a CA that is subordinate to an external CA).
- Development and validation of the Certificate Authority.
- Review of audit records.
- Personnel that helps to support the CA or to manage the physical and cloud facilities, 
but are not directly trusted to perform CA operations are defined to be in the authorized role. 
The set of tasks persons in the authorized role are allowed to perform must also be documented.

### Memberships of Trusted and Authorized Roles must be reviewed annually

Membership of trusted and authorized roles must be reviewed at least quarterly to 
ensure the set of people (for manual processes) or service identities 
(for automated processes) in each role is kept to a minimum.

### Certificate issuance process must enforce role separation between certificate requester and approver

The certificate issuance process must enforce role separation between certificate requester 
and certificate approver roles (persons or automated systems). Certificate issuance must be 
authorized by a certificate approver role that verifies that the certificate requestor 
is authorized to obtain certificates. 
The persons that hold the certificate approver role must be a formally authorized person.

### Privileged role management must restrict access and be reviewed quarterly

Assignment of privileged roles, such as authorizing membership of the Administrators and Approvers group,
must be restricted to a limited set of authorized personnel. Any privileged role changes must have 
access revoked within 24 hours. Finally, privileged role assignments must be reviewed on a quarterly 
basis and any unneeded or expired assignments must be removed.

### Privileged roles should use Two-Factor Authentication

Multi-factor authentication (Two-Factor Authentication, MFA, or TFA) must be used for 
interactive logons of Approvers and Administrators to the service.

## Certificate service operation guidelines

### Operational contacts

The certificate service must have an up-to-date Security Response Plan on file which contains detailed operational incident response contacts.

### Security updates

All systems must be continuously monitored and updated with latest security updates/patch compliance.

**Important note:** The github repository of the OPC Vault service is continuously updated with security patches. The updates on github 
should be monitored and the updates be applied to the service at regular intervals.

### Security monitoring

Subscribe to or implement appropriate security monitoring e.g. by subscribing to a central monitoring solution 
(e.g Azure monitoring solution, O365 monitoring solution) and configure it appropriately to ensure 
that security events are transmitted to the monitoring solution.

**Important note:** By default, the OPC Vault service is deployed with the Azure Application Insights as a monitoring solution. 

### Assess Security of Open Source Software Components

All open source components used within a product or service must be free of moderate or greater security vulnerabilities.

**Important note:** The github repository of the OPC Vault service is scanning all components during continously integration 
builds for vulnerabilities. The updates on github should be monitored and the updates be applied to the service at regular intervals.

### Maintain an inventory

An asset inventory must be maintained for all production hosts (including persistent virtual machines), devices, 
all internal IP address ranges, VIPs, and public DNS domain names. This inventory must be updated with addition 
or removal of a system, device IP address, VIP, or public DNS domain within 30 days.

#### Inventory of the default Azure OPC Vault production deployment: 

In **Azure**:
- **App Service Plan**: App service plan for service hosts. Default S1.
- **App Service** for microservice: The OPC Vault service host.
- **App Service** for sample application: The OPC Vault sample application host.
- **KeyVault Standard**: To store secrets and Cosmos DB keys for web service.
- **KeyVault Premium**: To host the Issuer CA keys, for signing service, for vault configuration and storage of application private keys.
- **Cosmos DB**: Database for application- 
- **Application Insights**: (optional) Monitoring solution for web service and application.

In **IoT Edge** or local a **Server**:
- **Global Discovery Server**: To support a factory network Global Discovery Server. 
- 
### Document the Certification Authorities (CAs)

The CA hierarchy documentation must contain all operated CAs including all related 
subordinate CAs, parent CAs, and root CAs, even when they are not managed by the service. 
An exhaustive set of all non-expired CA certificates may be provided instead of formal documentation.

**Important note:** The OPC Vault sample application supports for download of all certificates used and prodcued in the service for documentation.

### Document the issued certificates by all Certification Authorities (CAs)

An exhaustive set of all certificates issued in the past 12 months should be provided for documentation.

### Document the SOP for securely deleting cryptographic keys

Key deletion may only rarely happen during the lifetime of a CA, this is why no user has KeyVault Certificate Delete 
rights assigned and why there are no APIs exposed to delete an Issuer CA certificate. 
The manual standard operating procedure for securely deleting certification authority cryptographic keys is only available by directly
accessing the KeyVault in the Azure portal and by deleting the certificate group in KeyVault. For ensure immediate deletion
[KeyVault soft delete](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-ovw-soft-delete) should be disabled.

