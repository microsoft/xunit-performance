@echo off
@if defined _echo echo on

:main
setlocal enabledelayedexpansion
  set errorlevel=
  set BuildConfiguration=%~1
  if "%BuildConfiguration%"=="" set BuildConfiguration=Release
  
  set BuildVersion=%~2
  if "%BuildVersion%"=="" set /p BuildVersion=<"%~dp0BuildVersion.txt"

  set OutputDirectory=%~dp0LocalPackages
  call :remove_directory "%OutputDirectory%" || exit /b 1

  rem Don't fall back to machine-installed versions of dotnet, only use repo-local version
  set DOTNET_MULTILEVEL_LOOKUP=0

  call "%~dp0.\dotnet-install.cmd" || exit /b 1

  echo Where is dotnet.exe?
  where.exe dotnet.exe

  set procedures=
  set procedures=%procedures% build_xunit_performance_core
  set procedures=%procedures% build_xunit_performance_execution
  set procedures=%procedures% build_xunit_performance_metrics
  set procedures=%procedures% build_xunit_performance_api

  net.exe session 1>nul 2>&1 || (
    call :print_error_message Cannot run tests because this is not an administrator window.
    exit /b 1
  )

  set procedures=%procedures% build_tests_simpleharness
  set procedures=%procedures% build_tests_scenariobenchmark

  for %%p in (%procedures%) do (
    call :%%p || (
      call :print_error_message Failed to run %%p
      exit /b 1
    )
  )
endlocal& exit /b %errorlevel%

:build_xunit_performance_core
setlocal
  cd /d %~dp0src\xunit.performance.core
  call :dotnet_pack
  exit /b %errorlevel%

:build_xunit_performance_execution
setlocal
  cd /d %~dp0src\xunit.performance.execution
  call :dotnet_pack
  exit /b %errorlevel%

:build_xunit_performance_metrics
setlocal
  cd /d %~dp0src\xunit.performance.metrics
  call :dotnet_pack
  exit /b %errorlevel%

:build_xunit_performance_api
setlocal
  cd /d %~dp0src\xunit.performance.api
  call :dotnet_pack
  exit /b %errorlevel%

:build_tests_simpleharness
setlocal
  cd /d %~dp0tests\simpleharness
  call :dotnet_build || exit /b 1

  for %%v in (netcoreapp2.1) do (
    dotnet.exe publish -c %BuildConfiguration% --framework "%%v"                            || exit /b 1
    pushd ".\bin\%BuildConfiguration%\%%v\publish"
    if "%%v" == "net472" (
      ".\simpleharness.exe"            --perf:collect default+gcapi --perf:outputdir "!cd!" || exit /b 1
    ) else (
      dotnet.exe ".\simpleharness.dll" --perf:collect default+gcapi --perf:outputdir "!cd!" || exit /b 1
    )
    popd
  )

  exit /b %errorlevel%

:build_tests_scenariobenchmark
setlocal
  cd /d %~dp0tests\scenariobenchmark
  call :dotnet_build || exit /b 1

  for %%v in (netcoreapp2.1) do (
    dotnet.exe publish -c %BuildConfiguration% --framework "%%v"                          || exit /b 1
    pushd ".\bin\%BuildConfiguration%\%%v\publish"
    if "%%v" == "net472" (
      ".\scenariobenchmark.exe"            --perf:collect default --perf:outputdir "!cd!" || exit /b 1
    ) else (
      dotnet.exe ".\scenariobenchmark.dll" --perf:collect default --perf:outputdir "!cd!" || exit /b 1
    )
    popd
  )

  exit /b %errorlevel%

:dotnet_build
  echo/
  echo/  ==========
  echo/   Building %cd%
  echo/  ==========
  call :remove_directory bin                                                                  || exit /b 1
  call :remove_directory obj                                                                  || exit /b 1
  dotnet.exe restore --no-cache --packages "%~dp0packages"                                    || exit /b 1
  dotnet.exe build --no-dependencies -c %BuildConfiguration%                                  || exit /b 1
  exit /b 0

:dotnet_pack
setlocal
  call :dotnet_build || exit /b 1

  echo/
  echo/  ==========
  echo/   Packing %cd%
  echo/  ==========
  set MsBuildArgs=
  set "MsBuildArgs=%MsBuildArgs% --no-build"
  set "MsBuildArgs=%MsBuildArgs% -c %BuildConfiguration%"
  set "MsBuildArgs=%MsBuildArgs% --output "%OutputDirectory%""
  set "MsBuildArgs=%MsBuildArgs% --include-symbols --include-source"
  if defined LV_GIT_HEAD_SHA (
    set "MsBuildArgs=%MsBuildArgs% /p:GitHeadSha=%LV_GIT_HEAD_SHA%"
  )
  dotnet.exe pack %MsBuildArgs% || exit /b 1
  exit /b 0

:print_error_message
  echo/
  echo/  [ERROR] %*
  echo/
  exit /b %errorlevel%

:remove_directory
  if "%~1" == "" (
    call :print_error_message Directory name was not specified.
    exit /b 1
  )
  if exist "%~1" rmdir /s /q "%~1"
  if exist "%~1" (
    call :print_error_message Failed to remove directory "%~1".
    exit /b 1
  )
  exit /b 0
