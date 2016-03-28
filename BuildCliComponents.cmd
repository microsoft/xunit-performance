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

set DotNet_Path=%~dp0tools\
set Init_Tools_Log=%DotNet_Path%\install.log

if NOT exist "%DotNet_Path%" mkdir "%DotNet_Path%"
set /p DotNet_Version=< %~dp0DotnetCLIVersion.txt
set DotNet_Zip_Name=dotnet-win-x64.%DotNet_Version%.zip
set DotNet_Remote_Path=https://dotnetcli.blob.core.windows.net/dotnet/beta/Binaries/%DotNet_Version%/%DotNet_Zip_Name%
set DotNet_Local_Path=%DotNet_Path%%DotNet_Zip_Name%
echo Installing '%DotNet_Remote_Path%' to '%DotNet_Local_Path%' >> %Init_Tools_Log%
powershell -NoProfile -ExecutionPolicy unrestricted -Command "(New-Object Net.WebClient).DownloadFile('%DotNet_Remote_Path%', '%DotNet_Local_Path%'); Add-Type -Assembly 'System.IO.Compression.FileSystem' -ErrorVariable AddTypeErrors; if ($AddTypeErrors.Count -eq 0) { [System.IO.Compression.ZipFile]::ExtractToDirectory('%DotNet_Local_Path%', '%DotNet_Path%') } else { (New-Object -com shell.application).namespace('%DotNet_Path%').CopyHere((new-object -com shell.application).namespace('%DotNet_Local_Path%').Items(),16) }" >> %Init_Tools_Log%

if NOT exist "%DotNet_Local_Path%" (
  echo ERROR: Could not install dotnet cli correctly. See '%Init_Tools_Log%' for more details.
  goto :EOF
)

:build
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