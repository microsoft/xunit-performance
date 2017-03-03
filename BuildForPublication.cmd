@echo off
goto :main

:main
setlocal

set PackageVersion=1.0.0-alpha-build0100
set VersionSuffix=build0100

echo Building version %PackageVersion% NuGet packages.

build.cmd Release %VersionSuffix% %PackageVersion%

goto :eof
