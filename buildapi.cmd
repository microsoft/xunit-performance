@echo off
goto :main

:main
setlocal

set BuildConfiguration=%1%
if "%BuildConfiguration%"=="" set BuildConfiguration=Debug

set VersionSuffix=%2%
if "%VersionSuffix%"=="" set VersionSuffix=build0000

set PackageVersion=%3%
if "%PackageVersion%"=="" set PackageVersion=1.0.0-alpha-build0000

set OutputDirectory=%~dp0LocalPackages
set DotNet=%~dp0\tools\bin\dotnet.exe

if exist "%DotNet%" goto :build
echo Installing Dotnet CLI

set DotNet_Path=%~dp0tools\bin
set Init_Tools_Log=%DotNet_Path%\install.log

if NOT exist "%DotNet_Path%" mkdir "%DotNet_Path%"

set /p DotNet_Version=< %~dp0DotNetCLIVersion.txt
set DotNet_Installer_Url=https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1

powershell -NoProfile -ExecutionPolicy unrestricted -Command "Invoke-WebRequest -Uri '%DotNet_Installer_Url%' -OutFile '%DotNet_Path%\dotnet-install.ps1'"
echo Executing dotnet installer script %DotNet_Path%\dotnet-install.ps1
powershell -NoProfile -ExecutionPolicy unrestricted -Command "%DotNet_Path%\dotnet-install.ps1 -InstallDir %DotNet_Path% -Version '%DotNet_Version%'"

if NOT exist "%DotNet%" (
  echo ERROR: Could not install dotnet cli correctly. See '%Init_Tools_Log%' for more details.
  goto :EOF
)

:build



pushd %~dp0src\xunit.performance.api
call %DotNet% restore
call %DotNet% build -c %BuildConfiguration% --version-suffix %VersionSuffix%
popd


pushd %~dp0tests\simpleharness
call %DotNet% restore
call %DotNet% build -c %BuildConfiguration% --version-suffix %VersionSuffix%
call %DotNet% bin\%BuildConfiguration%\netcoreapp1.0\simpleharness.dll
popd

goto :eof