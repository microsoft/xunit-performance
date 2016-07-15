@echo off
setlocal

set BuildConfiguration=Debug

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

msbuild.exe /nologo /v:m /m /p:Configuration=%BuildConfiguration% /t:CI /fl xunit.performance.msbuild /fileloggerparameters:Verbosity=normal;LogFile=msbuild.log;Append

BuildCliComponents.cmd %BuildConfiguration%

goto :eof

:Usage
@echo Usage: CiBuild.cmd [/debug^|/release]
@echo   /debug 	Perform debug build.  This is the default.
@echo   /release Perform release build
@goto :eof