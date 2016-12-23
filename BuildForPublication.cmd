@echo off
goto :main

:main
setlocal

set VersionSuffix=build0043
set PackageVersion=1.0.0-alpha-%VersionSuffix%

echo Building version %PackageVersion% NuGet packages.

build.cmd Release %VersionSuffix% %PackageVersion%

goto :eof
