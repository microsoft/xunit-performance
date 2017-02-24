@echo off
@if defined _echo echo on

:main
setlocal enabledelayedexpansion
  set errorlevel=

  set lv_api_only=
  if /I "%~1" == "--api-only" (
    set lv_api_only=1
    shift
  )

  set BuildConfiguration=%~1
  if "%BuildConfiguration%"=="" set BuildConfiguration=Debug

  set VersionSuffix=%~2
  if "%VersionSuffix%"=="" set VersionSuffix=alpha-build0000

  set PackageVersion=%~3
  if "%PackageVersion%"=="" set PackageVersion=1.0.0-%VersionSuffix%

  set OutputDirectory=%~dp0LocalPackages
  call :remove_directory "%OutputDirectory%" || exit /b 1

  call "%~dp0.\dotnet-install.cmd" || exit /b 1

  set procedures=

  if not defined lv_api_only (
    set procedures=!procedures! build_procdomain
    set procedures=!procedures! build_xunit_performance_analysis
    set procedures=!procedures! build_xunit_performance_core
    set procedures=!procedures! build_xunit_performance_execution
    set procedures=!procedures! build_xunit_performance_metrics
    set procedures=!procedures! build_xunit_performance_logger
    set procedures=!procedures! build_xunit_performance_run
    set procedures=!procedures! build_microsoft_dotnet_xunit_performance_runner_cli
    set procedures=!procedures! build_microsoft_dotnet_xunit_performance_analysis_cli
    set procedures=!procedures! build_samples_classlibrary_net46
    set procedures=!procedures! build_samples_simpleperftests
    REM set procedures=!procedures! nuget_pack_src
  ) else (
    set procedures=!procedures! build_xunit_performance_core
    set procedures=!procedures! build_xunit_performance_execution
    set procedures=!procedures! build_xunit_performance_metrics
  )

  set procedures=%procedures% build_xunit_performance_api
  set procedures=%procedures% build_tests_simpleharness

  for %%p in (%procedures%) do (
    call :%%p || (
      call :print_error_message Failed to run %%p
      exit /b 1
    )
  )
  exit /b %errorlevel%

:build_procdomain
setlocal
  cd /d %~dp0src\procdomain
  call :dotnet_build
  exit /b %errorlevel%

:build_xunit_performance_analysis
setlocal
  cd /d %~dp0src\xunit.performance.analysis
  call :dotnet_build
  exit /b %errorlevel%

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

:build_xunit_performance_logger
setlocal
  cd /d %~dp0src\xunit.performance.logger
  call :dotnet_build
  exit /b %errorlevel%

:build_xunit_performance_metrics
setlocal
  cd /d %~dp0src\xunit.performance.metrics
  call :dotnet_pack
  exit /b %errorlevel%

:build_xunit_performance_run
setlocal
  cd /d %~dp0src\xunit.performance.run
  call :dotnet_build
  exit /b %errorlevel%

:build_microsoft_dotnet_xunit_performance_runner_cli
setlocal
  cd /d %~dp0src\cli\Microsoft.DotNet.xunit.performance.runner.cli
  call :dotnet_build
  exit /b %errorlevel%

:build_microsoft_dotnet_xunit_performance_analysis_cli
setlocal
  cd /d %~dp0src\cli\Microsoft.DotNet.xunit.performance.analysis.cli
  call :dotnet_build
  exit /b %errorlevel%

:build_samples_classlibrary_net46
setlocal
  cd /d %~dp0samples\ClassLibrary.net46
  call :dotnet_build
  exit /b %errorlevel%

:build_samples_simpleperftests
setlocal
  cd /d %~dp0samples\SimplePerfTests
  call :dotnet_build
  exit /b %errorlevel%

:nuget_pack_src
setlocal
  cd /d %~dp0src
  dotnet.exe restore xunit.performance.csproj                                                                                                                           || exit /b 1
  dotnet.exe pack xunit.performance.csproj                 --no-build -c %BuildConfiguration% -o "%OutputDirectory%" --version-suffix %VersionSuffix% --include-symbols || exit /b 1

  dotnet.exe restore xunit.performance.runner.Windows.csproj                                                                                                            || exit /b 1
  dotnet.exe pack xunit.performance.runner.Windows.csproj  --no-build -c %BuildConfiguration% -o "%OutputDirectory%" --version-suffix %VersionSuffix% --include-symbols || exit /b 1
  exit /b 0

:build_xunit_performance_api
setlocal
  cd /d %~dp0src\xunit.performance.api
  call :dotnet_pack
  exit /b %errorlevel%

:build_tests_simpleharness
setlocal
  cd /d %~dp0tests\simpleharness
  call :dotnet_build || exit /b 1
  net.exe session 1>nul 2>&1 || (
    call :print_error_message Cannot run simpleharness test because this is not an administrator window
    exit /b 1
  )
  dotnet.exe run -c %BuildConfiguration% "bin\%BuildConfiguration%\netcoreapp1.0\simpleharness.dll" || exit /b 1
  exit /b %errorlevel%

:dotnet_build
  echo/
  echo/  ==========
  echo/   Building %cd%
  echo/  ==========
  call :remove_directory bin                                                                  || exit /b 1
  call :remove_directory obj                                                                  || exit /b 1
  dotnet.exe restore                                                                          || exit /b 1
  dotnet.exe build --no-dependencies -c %BuildConfiguration% --version-suffix %VersionSuffix% || exit /b 1
  exit /b 0

:dotnet_pack
  echo/
  echo/  ==========
  echo/   Packing %cd%
  echo/  ==========
  call :dotnet_build                                                                                                                                    || exit /b 1
  dotnet.exe pack  --no-build -c %BuildConfiguration% --version-suffix %VersionSuffix% --output "%OutputDirectory%" --include-symbols --include-source  || exit /b 1
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
