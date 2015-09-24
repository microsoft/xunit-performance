@echo off
setlocal
call NightlyBuild.cmd
set PackageDir=%~dp0LocalPackages
call dnu restore dnx\src\Microsoft.DotNet.xunit.performance.runner.dnx\project.json -f %PackageDir%
call dnu pack dnx\src\Microsoft.DotNet.xunit.performance.runner.dnx\project.json --out %PackageDir% --configuration Release
