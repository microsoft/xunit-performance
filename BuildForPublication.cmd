@echo off
goto :main

:main
setlocal
  set errorlevel=
  set BuildConfiguration=Release
  set VersionSuffix=build0044
  set PackageVersion=1.0.0-alpha-%VersionSuffix%

  echo/==================
  echo/ Building version %PackageVersion% NuGet packages.
  echo/==================

  call build.cmd %BuildConfiguration% %VersionSuffix% %PackageVersion%
endlocal& exit /b %errorlevel%
