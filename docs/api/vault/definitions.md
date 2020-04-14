
<a name="definitions"></a>
## Definitions

<a name="certificaterequestqueryrequestapimodel"></a>
### CertificateRequestQueryRequestApiModel
Certificate request query model


|Name|Description|Schema|
|---|---|---|
|**entityId**  <br>*optional*|The entity id to filter with|string|
|**state**  <br>*optional*||[CertificateRequestState](definitions.md#certificaterequeststate)|


<a name="certificaterequestqueryresponseapimodel"></a>
### CertificateRequestQueryResponseApiModel
Response model


|Name|Description|Schema|
|---|---|---|
|**nextPageLink**  <br>*optional*|Link to the next page of results.|string|
|**requests**  <br>*optional*|The query result.|< [CertificateRequestRecordApiModel](definitions.md#certificaterequestrecordapimodel) > array|


<a name="certificaterequestrecordapimodel"></a>
### CertificateRequestRecordApiModel
Certificate request record model


|Name|Description|Schema|
|---|---|---|
|**accepted**  <br>*optional*||[VaultOperationContextApiModel](definitions.md#vaultoperationcontextapimodel)|
|**approved**  <br>*optional*||[VaultOperationContextApiModel](definitions.md#vaultoperationcontextapimodel)|
|**entityId**  <br>*optional*|Application id|string|
|**errorInfo**  <br>*optional*|Error diagnostics|string|
|**groupId**  <br>*optional*|Trust group|string|
|**requestId**  <br>*optional*|Request id|string|
|**state**  <br>*optional*||[CertificateRequestState](definitions.md#certificaterequeststate)|
|**submitted**  <br>*optional*||[VaultOperationContextApiModel](definitions.md#vaultoperationcontextapimodel)|
|**type**  <br>*optional*||[CertificateRequestType](definitions.md#certificaterequesttype)|


<a name="certificaterequeststate"></a>
### CertificateRequestState
The certificate request states.

*Type* : enum (New, Approved, Rejected, Failure, Completed, Accepted)


<a name="certificaterequesttype"></a>
### CertificateRequestType
The certificate request type.

*Type* : enum (SigningRequest, KeyPairRequest)


<a name="finishnewkeypairrequestresponseapimodel"></a>
### FinishNewKeyPairRequestResponseApiModel
Finish request results


|Name|Schema|
|---|---|
|**certificate**  <br>*optional*|[X509CertificateApiModel](definitions.md#x509certificateapimodel)|
|**privateKey**  <br>*optional*|[PrivateKeyApiModel](definitions.md#privatekeyapimodel)|
|**request**  <br>*optional*|[CertificateRequestRecordApiModel](definitions.md#certificaterequestrecordapimodel)|


<a name="finishsigningrequestresponseapimodel"></a>
### FinishSigningRequestResponseApiModel
Finish request results


|Name|Schema|
|---|---|
|**certificate**  <br>*optional*|[X509CertificateApiModel](definitions.md#x509certificateapimodel)|
|**request**  <br>*optional*|[CertificateRequestRecordApiModel](definitions.md#certificaterequestrecordapimodel)|


<a name="notfoundresult"></a>
### NotFoundResult

|Name|Schema|
|---|---|
|**StatusCode**  <br>*optional*  <br>*read-only*|integer (int32)|


<a name="privatekeyapimodel"></a>
### PrivateKeyApiModel
Private key


|Name|Description|Schema|
|---|---|---|
|**crv**  <br>*optional*|The curve for ECC algorithms|string|
|**d**  <br>*optional*|RSA private exponent or ECC private key.  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**dp**  <br>*optional*|RSA Private Key Parameter  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**dq**  <br>*optional*|RSA Private Key Parameter  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**e**  <br>*optional*|RSA public exponent, in Base64.  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**k**  <br>*optional*|Symmetric key  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**key_hsm**  <br>*optional*|HSM Token, used with "Bring Your Own Key"  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**kty**  <br>*optional*||[PrivateKeyType](definitions.md#privatekeytype)|
|**n**  <br>*optional*|RSA modulus.  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**p**  <br>*optional*|RSA secret prime  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**q**  <br>*optional*|RSA secret prime, with p < q  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**qi**  <br>*optional*|RSA Private Key Parameter  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**x**  <br>*optional*|X coordinate for the Elliptic Curve point.  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**y**  <br>*optional*|Y coordinate for the Elliptic Curve point.  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|


<a name="privatekeytype"></a>
### PrivateKeyType
Key type

*Type* : enum (RSA, ECC, AES)


<a name="signaturealgorithm"></a>
### SignatureAlgorithm
Signature algorithm

*Type* : enum (Rsa256, Rsa384, Rsa512, Rsa256Pss, Rsa384Pss, Rsa512Pss)


<a name="startnewkeypairrequestapimodel"></a>
### StartNewKeyPairRequestApiModel
New key pair request


|Name|Description|Schema|
|---|---|---|
|**certificateType**  <br>*optional*||[TrustGroupType](definitions.md#trustgrouptype)|
|**domainNames**  <br>*optional*|Domain names|< string > array|
|**entityId**  <br>*optional*|Entity id|string|
|**groupId**  <br>*optional*|Certificate group|string|
|**subjectName**  <br>*optional*|Subject name|string|


<a name="startnewkeypairrequestresponseapimodel"></a>
### StartNewKeyPairRequestResponseApiModel
New key pair response


|Name|Description|Schema|
|---|---|---|
|**requestId**  <br>*optional*|Request id|string|


<a name="startsigningrequestapimodel"></a>
### StartSigningRequestApiModel
Signing request


|Name|Description|Schema|
|---|---|---|
|**certificateRequest**  <br>*optional*|Request|string|
|**entityId**  <br>*optional*|Id of entity to sign a certificate for|string|
|**groupId**  <br>*optional*|Certificate group id|string|


<a name="startsigningrequestresponseapimodel"></a>
### StartSigningRequestResponseApiModel
Signing request response


|Name|Description|Schema|
|---|---|---|
|**requestId**  <br>*optional*|Request id|string|


<a name="trustgroupapimodel"></a>
### TrustGroupApiModel
Trust group model


|Name|Description|Schema|
|---|---|---|
|**issuedKeySize**  <br>*optional*|The issued certificate key size in bits.|integer (int32)|
|**issuedLifetime**  <br>*optional*|The issued certificate lifetime in months.|string (date-span)|
|**issuedSignatureAlgorithm**  <br>*optional*||[SignatureAlgorithm](definitions.md#signaturealgorithm)|
|**keySize**  <br>*optional*|The trust group certificate key size in bits.|integer (int32)|
|**lifetime**  <br>*optional*|The lifetime of the trust group certificate.|string (date-span)|
|**name**  <br>*optional*|The name of the trust group.|string|
|**parentId**  <br>*optional*|The identifer of the parent trust group.|string|
|**signatureAlgorithm**  <br>*optional*||[SignatureAlgorithm](definitions.md#signaturealgorithm)|
|**subjectName**  <br>*optional*|The subject name of the group as distinguished name.|string|
|**type**  <br>*optional*||[TrustGroupType](definitions.md#trustgrouptype)|


<a name="trustgroupregistrationapimodel"></a>
### TrustGroupRegistrationApiModel
Trust group registration model


|Name|Description|Schema|
|---|---|---|
|**group**  <br>*optional*||[TrustGroupApiModel](definitions.md#trustgroupapimodel)|
|**id**  <br>*optional*|The registered id of the trust group|string|


<a name="trustgroupregistrationlistapimodel"></a>
### TrustGroupRegistrationListApiModel
Trust group registration collection model


|Name|Description|Schema|
|---|---|---|
|**nextPageLink**  <br>*optional*|Next link|string|
|**registrations**  <br>*optional*|Group registrations|< [TrustGroupRegistrationApiModel](definitions.md#trustgroupregistrationapimodel) > array|


<a name="trustgroupregistrationrequestapimodel"></a>
### TrustGroupRegistrationRequestApiModel
Trust group registration request model


|Name|Description|Schema|
|---|---|---|
|**issuedKeySize**  <br>*optional*|The issued certificate key size in bits.|integer (int32)|
|**issuedLifetime**  <br>*optional*|The lifetime of certificates issued in the group.|string (date-span)|
|**issuedSignatureAlgorithm**  <br>*optional*||[SignatureAlgorithm](definitions.md#signaturealgorithm)|
|**name**  <br>*optional*|The new name of the trust group|string|
|**parentId**  <br>*optional*|The identifer of the parent trust group.|string|
|**subjectName**  <br>*optional*|The subject name of the group as distinguished name.|string|


<a name="trustgroupregistrationresponseapimodel"></a>
### TrustGroupRegistrationResponseApiModel
Trust group registration response model


|Name|Description|Schema|
|---|---|---|
|**id**  <br>*optional*|The id of the trust group|string|


<a name="trustgrouprootcreaterequestapimodel"></a>
### TrustGroupRootCreateRequestApiModel
Trust group root registration model


|Name|Description|Schema|
|---|---|---|
|**issuedKeySize**  <br>*optional*|The issued certificate key size in bits.|integer (int32)|
|**issuedLifetime**  <br>*optional*|The issued certificate lifetime.|string (date-span)|
|**issuedSignatureAlgorithm**  <br>*optional*||[SignatureAlgorithm](definitions.md#signaturealgorithm)|
|**keySize**  <br>*optional*|The certificate key size in bits.|integer (int32)|
|**lifetime**  <br>*optional*|The lifetime of the trust group root certificate.|string (date-span)|
|**name**  <br>*optional*|The new name of the trust group root|string|
|**signatureAlgorithm**  <br>*optional*||[SignatureAlgorithm](definitions.md#signaturealgorithm)|
|**subjectName**  <br>*optional*|The subject name of the group as distinguished name.|string|
|**type**  <br>*optional*||[TrustGroupType](definitions.md#trustgrouptype)|


<a name="trustgrouptype"></a>
### TrustGroupType
Trust group types

*Type* : enum (ApplicationInstanceCertificate, HttpsCertificate, UserCredentialCertificate)


<a name="trustgroupupdaterequestapimodel"></a>
### TrustGroupUpdateRequestApiModel
Trust group update model


|Name|Description|Schema|
|---|---|---|
|**issuedKeySize**  <br>*optional*|The issued certificate key size in bits.|integer (int32)|
|**issuedLifetime**  <br>*optional*|The issued certificate lifetime.|string (date-span)|
|**issuedSignatureAlgorithm**  <br>*optional*||[SignatureAlgorithm](definitions.md#signaturealgorithm)|
|**name**  <br>*optional*|The name of the trust group|string|


<a name="vaultoperationcontextapimodel"></a>
### VaultOperationContextApiModel
Vault operation log model


|Name|Description|Schema|
|---|---|---|
|**authorityId**  <br>*optional*|User|string|
|**time**  <br>*optional*|Operation time|string (date-time)|


<a name="x509certificateapimodel"></a>
### X509CertificateApiModel
Certificate model


|Name|Description|Schema|
|---|---|---|
|**certificate**  <br>*optional*|Raw data  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**notAfterUtc**  <br>*optional*|Not after validity|string (date-time)|
|**notBeforeUtc**  <br>*optional*|Not before validity|string (date-time)|
|**selfSigned**  <br>*optional*|Self signed|boolean|
|**serialNumber**  <br>*optional*|Serial number|string|
|**subject**  <br>*optional*|Subject|string|
|**thumbprint**  <br>*optional*|Thumbprint|string|


<a name="x509certificatechainapimodel"></a>
### X509CertificateChainApiModel
Certificate chain


|Name|Description|Schema|
|---|---|---|
|**chain**  <br>*optional*|Chain|< [X509CertificateApiModel](definitions.md#x509certificateapimodel) > array|
|**status**  <br>*optional*|Chain validation status if validated|enum (NoError, NotTimeValid, Revoked, NotSignatureValid, NotValidForUsage, UntrustedRoot, RevocationStatusUnknown, Cyclic, InvalidExtension, InvalidPolicyConstraints, InvalidBasicConstraints, InvalidNameConstraints, HasNotSupportedNameConstraint, HasNotDefinedNameConstraint, HasNotPermittedNameConstraint, HasExcludedNameConstraint, PartialChain, CtlNotTimeValid, CtlNotSignatureValid, CtlNotValidForUsage, HasWeakSignature, OfflineRevocation, NoIssuanceChainPolicy, ExplicitDistrust, HasNotSupportedCriticalExtension)|


<a name="x509certificatelistapimodel"></a>
### X509CertificateListApiModel
Certificate list


|Name|Description|Schema|
|---|---|---|
|**certificates**  <br>*optional*|Certificates|< [X509CertificateApiModel](definitions.md#x509certificateapimodel) > array|
|**nextPageLink**  <br>*optional*|Next link|string|


<a name="x509chainstatus"></a>
### X509ChainStatus
Status of x509 chain

*Type* : enum (NoError, NotTimeValid, Revoked, NotSignatureValid, NotValidForUsage, UntrustedRoot, RevocationStatusUnknown, Cyclic, InvalidExtension, InvalidPolicyConstraints, InvalidBasicConstraints, InvalidNameConstraints, HasNotSupportedNameConstraint, HasNotDefinedNameConstraint, HasNotPermittedNameConstraint, HasExcludedNameConstraint, PartialChain, CtlNotTimeValid, CtlNotSignatureValid, CtlNotValidForUsage, HasWeakSignature, OfflineRevocation, NoIssuanceChainPolicy, ExplicitDistrust, HasNotSupportedCriticalExtension)


<a name="x509crlapimodel"></a>
### X509CrlApiModel
A X509 certificate revocation list.


|Name|Description|Schema|
|---|---|---|
|**crl**  <br>*optional*|The certificate revocation list.|string|
|**issuer**  <br>*optional*|The Issuer name of the revocation list.|string|


<a name="x509crlchainapimodel"></a>
### X509CrlChainApiModel
Crl chain model


|Name|Description|Schema|
|---|---|---|
|**chain**  <br>*optional*|Chain|< [X509CrlApiModel](definitions.md#x509crlapimodel) > array|



