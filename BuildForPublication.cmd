@echo off
setlocal
call NightlyBuild.cmd
set PackageDir=%~dp0LocalPackages
pushd %~dp0dnx\src\Microsoft.DotNet.xunit.performance.runner.dnx
call dnu restore
call dnu pack --out %PackageDir% --configuration Release
popd