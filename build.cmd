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

  set DotNet=%~dp0\tools\bin\dotnet.exe

  if not exist "%DotNet%" (
    call :install_dotnet_cli || exit /b 1
  )

  set procedures=

  if not defined lv_api_only (
    set procedures=!procedures! build_procdomain
    set procedures=!procedures! build_xunit_performance_analysis
    set procedures=!procedures! build_xunit_performance_core
    set procedures=!procedures! build_xunit_performance_execution
    set procedures=!procedures! build_xunit_performance_logger
    set procedures=!procedures! build_xunit_performance_metrics
    set procedures=!procedures! build_xunit_performance_run
    set procedures=!procedures! build_microsoft_dotnet_xunit_performance_runner_cli
    set procedures=!procedures! build_microsoft_dotnet_xunit_performance_analysis_cli
    set procedures=!procedures! build_samples_classlibrary_net46
    set procedures=!procedures! build_samples_simpleperftests
    set procedures=!procedures! nuget_pack_src
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

:install_dotnet_cli
setlocal
  echo Installing Dotnet CLI

  set DotNet_Path=%~dp0tools\bin
  set Init_Tools_Log=%DotNet_Path%\install.log

  if not exist "%DotNet_Path%" mkdir "%DotNet_Path%"
  if not exist "%DotNet_Path%" (
    call :print_error_message Unable to create the "%DotNet_Path%" folder.
    exit /b 1
  )

  set /p DotNet_Version=< %~dp0DotNetCLIVersion.txt
  set DotNet_Installer_Url=https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1

  echo Downloading dotnet installer script dotnet-install.ps1
  powershell -NoProfile -ExecutionPolicy unrestricted -Command "Invoke-WebRequest -Uri '%DotNet_Installer_Url%' -OutFile '%DotNet_Path%\dotnet-install.ps1'"
  if not exist "%DotNet_Path%\dotnet-install.ps1" (
    call :print_error_message Failed to download "%DotNet_Path%\dotnet-install.ps1"
    exit /b 1
  )

  echo Executing dotnet installer script "%DotNet_Path%\dotnet-install.ps1"
  powershell -NoProfile -ExecutionPolicy unrestricted -Command "&'%DotNet_Path%\dotnet-install.ps1' -InstallDir '%DotNet_Path%' -Version '%DotNet_Version%'"
  if not exist "%DotNet%" (
    call :print_error_message Could not install dotnet cli correctly. See '%Init_Tools_Log%' for more details.
    exit /b 1
  )

  call "%DotNet%" --version
endlocal& exit /b 0

:build_procdomain
setlocal
  cd /d %~dp0src\procdomain
  call :dotnet_build
  exit /b %errorlevel%

:build_xunit_performance_analysis
setlocal
  cd /d %~dp0src\xunit.performance.analysis
  call :dotnet_pack
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
  call :dotnet_pack
  exit /b %errorlevel%

:build_microsoft_dotnet_xunit_performance_analysis_cli
setlocal
  cd /d %~dp0src\cli\Microsoft.DotNet.xunit.performance.analysis.cli
  call :dotnet_pack
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
  call "%DotNet%" nuget pack xunit.performance.nuspec                -p Configuration=%BuildConfiguration% --version=%PackageVersion% --output-directory "%OutputDirectory%" --symbols  || exit /b 1
  call "%DotNet%" nuget pack xunit.performance.runner.Windows.nuspec -p Configuration=%BuildConfiguration% --version=%PackageVersion% --output-directory "%OutputDirectory%" --symbols  || exit /b 1
  exit /b 0

:build_xunit_performance_api
setlocal
  cd /d %~dp0src\xunit.performance.api
  call :dotnet_pack
  exit /b %errorlevel%

:build_tests_simpleharness
setlocal
  cd /d %~dp0tests\simpleharness
  call :dotnet_build  || exit /b 1
  net.exe session 1>nul 2>&1 || (
    call :print_error_message Cannot run simpleharness test because this is not an administrator window
    exit /b 1
  )
  call "%DotNet%" run -c %BuildConfiguration% "bin\%BuildConfiguration%\netcoreapp1.0\simpleharness.dll" || exit /b 1
  exit /b %errorlevel%

:dotnet_build
  echo/
  echo/  ==========
  echo/   Building %cd%
  echo/  ==========
  call :remove_directory bin                                                      || exit /b 1
  call :remove_directory obj                                                      || exit /b 1
  call "%DotNet%" restore                                                         || exit /b 1
  call "%DotNet%" build -c %BuildConfiguration% --version-suffix %VersionSuffix%  || exit /b 1
  exit /b 0

:dotnet_pack
  echo/
  echo/  ==========
  echo/   Packing %cd%
  echo/  ==========
  call :remove_directory bin                                                                                                    || exit /b 1
  call :remove_directory obj                                                                                                    || exit /b 1
  call "%DotNet%" restore                                                                                                       || exit /b 1
  call "%DotNet%" build -c %BuildConfiguration% --version-suffix %VersionSuffix%                                                || exit /b 1
  call "%DotNet%" pack  -c %BuildConfiguration% --version-suffix %VersionSuffix% --output "%OutputDirectory%" --include-symbols || exit /b 1

  :: FIXME: pack sources does not work with the current mixed version of the Tracing library (EXCEPTION THROWN).
  ::call "%DotNet%" pack  -c %BuildConfiguration% --version-suffix %VersionSuffix% --output "%OutputDirectory%" --include-symbols --include-source  || exit /b 1
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
