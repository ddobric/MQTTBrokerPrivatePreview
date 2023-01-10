
### Create Self-Signed certificate for the publisher

~~~
 New-SelfSignedCertificate -Type Custom -Subject "CN=DotnetPublisher,O=ACP Digital, OU=daenet,DC=com, C=Germany"  -KeyUsage DigitalSignature -KeyAlgorithm RSA -KeyLength 2048 -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable~~~


### Create Self-Signed certificate for the subscriber

~~~
 New-SelfSignedCertificate -Type Custom -Subject "CN=DotnetSubscriber,O=ACP Digital, OU=daenet,DC=com, C=Germany"  -KeyUsage DigitalSignature -KeyAlgorithm RSA -KeyLength 2048 -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable
 ~~~