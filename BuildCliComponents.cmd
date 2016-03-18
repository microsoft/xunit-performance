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
set DotNet=%~dp0\tools\cli\bin\dotnet

echo Installing Dotnet CLI
powershell -Command "& {%~dp0Install-CLI.ps1 -ToolsDir %~dp0tools}"

echo Building CLI-based components
pushd %~dp0src\cli\Microsoft.DotNet.xunit.performance.runner.cli
call %DotNet% restore
call %DotNet% build -r win7-x64 -c Release
call %DotNet% build -r ubuntu.14.04-x64 -c Release
call %DotNet% build -r rhel.7-x64 -c Release
call %DotNet% build -r osx.10.10-x64 -c Release
call %DotNet% pack -c Release -o %OutputDirectory%
popd

goto :eof