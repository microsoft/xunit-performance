@echo off
goto :main

======================================================================
Build the components of xunit.performance that support build via the
Dotnet CLI.

This script will bootstrap the latest version of the CLI into the
tools directory prior to build.
======================================================================


:main
setlocal

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
echo Building CLI-based components
pushd %~dp0src\cli\Microsoft.DotNet.xunit.performance.runner.cli
call %DotNet% restore
call %DotNet% build -c Release
call %DotNet% pack -c Release -o %OutputDirectory%
popd
pushd %~dp0src\cli\Microsoft.DotNet.xunit.performance.analysis.cli
call %DotNet% restore
call %DotNet% build -c Release
call %DotNet% pack -c Release -o %OutputDirectory%
popd

goto :eof