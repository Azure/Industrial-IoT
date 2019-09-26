@echo off
shift
pushd scripts
powershell ./build.ps1 %*
popd