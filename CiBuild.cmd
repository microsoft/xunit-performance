@if not defined _echo echo off

setlocal

set "ERRORLEVEL="
set "BuildConfiguration=Debug"

:ParseArguments
  if "%1" == "" goto :DoneParsing
  if /I "%1" == "/?" call :Usage && exit /b 1
  if /I "%1" == "/debug" (
    set BuildConfiguration=Debug
    shift /1
    goto :ParseArguments
  )
  if /I "%1" == "/release" (
    set BuildConfiguration=Release
    shift /1
    goto :ParseArguments
  )
  call :Usage && exit /b 1
:DoneParsing

call "%~dp0build.cmd" %BuildConfiguration%
exit /b %ERRORLEVEL%

:Usage
@echo Usage: CiBuild.cmd [/debug^|/release]
@echo   /debug 	Perform debug build.  This is the default.
@echo   /release Perform release build
@goto :eof