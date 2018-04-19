@echo off
@if defined _echo echo on

:main
setlocal EnableDelayedExpansion
  set errorlevel=
  set BuildConfiguration=Release
  set VersionSuffix=beta-build0019

  REM Check that git is on path.
  where.exe /Q git.exe || (
    echo ERROR: git.exe is not in the path.
    exit /b 1
  )

  set /a count = 0
  for /f %%l in ('git clean -xdn') do set /a count += 1
  for /f %%l in ('git status --porcelain') do set /a count += 1
  if %count% neq 0 (
    choice.exe /T 10 /D N /C YN /M "WARNING: The repo contains uncommitted changes and you are building for publication. Press Y to continue or N to stop. "
    if !errorlevel! neq 1 exit /b 1
  )

  set LV_GIT_HEAD_SHA=
  for /f %%c in ('git rev-parse HEAD') do set "LV_GIT_HEAD_SHA=%%c"

  set LocalDotNet_ToolsDir=%~dp0tools
  if exist "%LocalDotNet_ToolsDir%" rmdir /s /q "%LocalDotNet_ToolsDir%"
  if exist "%LocalDotNet_ToolsDir%" (
    echo ERROR: Failed to remove "%LocalDotNet_ToolsDir%" folder.
    exit /b 1
  )

  set LocalDotNet_PackagesDir=%~dp0packages
  if exist "%LocalDotNet_PackagesDir%" rmdir /s /q "%LocalDotNet_PackagesDir%"
  if exist "%LocalDotNet_PackagesDir%" (
    echo ERROR: Failed to remove "%LocalDotNet_PackagesDir%" folder.
    exit /b 1
  )

  echo/==================
  echo/ Building version %VersionSuffix% NuGet packages.
  echo/==================
  call build.cmd %BuildConfiguration% %VersionSuffix%
endlocal& exit /b %errorlevel%
