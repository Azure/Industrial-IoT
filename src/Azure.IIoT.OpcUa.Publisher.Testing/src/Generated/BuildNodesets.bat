@echo off
setlocal

pushd Isa95Jobs
echo Building %design%
Opc.Ua.ModelCompiler.exe compile-nodesets -input "Nodesets" -o2 "Design" -uri http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/
popd



