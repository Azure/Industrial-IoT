@echo off
shift
pushd scripts
powershell ./acrmatrix.ps1 -Build %*
popd