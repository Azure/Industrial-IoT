@echo off
REM create the app certificate
cd /D %~dp0
set CERTSTORE=".\OPC Foundation\CertificateStores\MachineDefault"
rd /S/Q %CERTSTORE%
md %CERTSTORE%
Opc.Ua.CertificateGenerator.exe -cmd issue -sp %CERTSTORE% -ks 2048 -an "Opc.Ua.Client.SampleModule" -dn %COMPUTERNAME% -sn "CN=Opc.Ua.Client.SampleModule/DC=%COMPUTERNAME%" -au "urn:localhost:OPCFoundation:SampleModule"
set CERTSTORE=


