@echo off
goto :main

:main
setlocal
  set errorlevel=
  set BuildConfiguration=Release
  set VersionSuffix=build0047
  set PackageVersion=1.0.0-alpha-%VersionSuffix%

  echo/==================
  echo/ Building version %PackageVersion% NuGet packages.
  echo/==================

  set LocalDotNet_ToolsDir=%~dp0tools
  if exist "%LocalDotNet_ToolsDir%" rmdir /s /q "%LocalDotNet_ToolsDir%"
  if exist "%LocalDotNet_ToolsDir%" (
    echo ERROR: Failed to remove "%LocalDotNet_ToolsDir%" folder.
    exit /b 1
  )

  call build.cmd %BuildConfiguration% %VersionSuffix% %PackageVersion%
endlocal& exit /b %errorlevel%
