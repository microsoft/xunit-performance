@echo off
@if defined _echo echo on

:install_dotnet_cli
setlocal
  set "DOTNET_MULTILEVEL_LOOKUP=0"

  set /p DotNet_Version=<"%~dp0DotNetCLIVersion.txt"
  if not defined DotNet_Version (
    call :print_error_message Unknown DotNet CLI Version.
    exit /b 1
  )

  set DotNet_Path=%~dp0packages\dotnet\%DotNet_Version%
  set DotNet=%DotNet_Path%\dotnet.exe
  set Init_Tools_Log=%DotNet_Path%\install.log
  set DotNet_Installer_Url=https://raw.githubusercontent.com/dotnet/cli/release/2.0.0/scripts/obtain/dotnet-install.ps1

  REM dotnet.exe might exist, but it might not be the right version.
  REM Here we verify that if it is not the right version, then we install it
  if exist "%DotNet%" (
    (call "%DotNet%" --version|findstr /i "%DotNet_Version%" 1>nul 2>&1) && goto :install_dotnet_cli_exit
    call :print_error_message Current version of "%DotNet%" does not match expected version "%DotNet_Version%"
    call :remove_directory "%DotNet_Path%" || exit /b 1
  )

  if not exist "%DotNet_Path%" mkdir "%DotNet_Path%"
  if not exist "%DotNet_Path%" (
    call :print_error_message Unable to create the "%DotNet_Path%" folder.
    exit /b 1
  )

  echo Installing Dotnet CLI
  echo Downloading dotnet installer script dotnet-install.ps1
  powershell -NoProfile -ExecutionPolicy unrestricted -Command "Invoke-WebRequest -Uri '%DotNet_Installer_Url%' -OutFile '%DotNet_Path%\dotnet-install.ps1'"
  if not exist "%DotNet_Path%\dotnet-install.ps1" (
    call :print_error_message Failed to download "%DotNet_Path%\dotnet-install.ps1"
    exit /b 1
  )

  echo Executing dotnet installer script "%DotNet_Path%\dotnet-install.ps1"
  for %%v in (2.1.300) do (
    echo Installing dotnet sdk version %%~v
    powershell -NoProfile -ExecutionPolicy unrestricted -Command "&'%DotNet_Path%\dotnet-install.ps1' -InstallDir '%DotNet_Path%' -Version '%%~v'" || (
      call :print_error_message Failed to install dotnet shared runtime %%~v
      exit /b 1
    )
  )

  if not exist "%DotNet%" (
    call :print_error_message Could not install dotnet cli correctly. See '%Init_Tools_Log%' for more details.
    exit /b 1
  )

:install_dotnet_cli_exit
  ECHO/
  call "%DotNet%" --info
  ECHO/
endlocal& (
  set "PATH=%DotNet_Path%;%PATH%"
  exit /b 0
)

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
