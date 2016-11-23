@echo off
goto :main

:main
setlocal

set PackageVersion=1.0.0-alpha-build0043
set VersionSuffix=build0043

echo Building version %PackageVersion% NuGet packages.

build.cmd Release %VersionSuffix% %PackageVersion%

goto :eof
