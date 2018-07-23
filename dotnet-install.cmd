@if not defined _echo echo off

:install_dotnet_cli
setlocal
  set "DOTNET_MULTILEVEL_LOOKUP=0"
  set "UseSharedCompilation=false"
  set /p DotNet_Version=<"%~dp0DotNetCLIVersion.txt"
  if not defined DotNet_Version (
    call :print_error_message Unknown DotNet CLI Version.
    exit /b 1
  )

  set "DotNet_Path=%~dp0tools\dotnet\%DotNet_Version%"
  set "DotNet=%DotNet_Path%\dotnet.exe"
  set "DotNet_Installer_Url=https://raw.githubusercontent.com/dotnet/cli/v%DotNet_Version%/scripts/obtain/dotnet-install.ps1"

  REM dotnet.exe might exist, but it might not be the right version.
  REM Here we verify that if it is not the right version, then we install it
  if exist "%DotNet%" (
    (call "%DotNet%" --version|findstr.exe /i /c:"%DotNet_Version%" 1>nul 2>&1) && goto :install_dotnet_cli_exit
    call :print_error_message Current version of "%DotNet%" does not match expected version "%DotNet_Version%"
    call :remove_directory "%DotNet_Path%" || exit /b 1
  )

  if not exist "%DotNet_Path%" mkdir "%DotNet_Path%"
  if not exist "%DotNet_Path%" (
    call :print_error_message Unable to create the "%DotNet_Path%" folder.
    exit /b 1
  )

  call :print_header_message Downloading dotnet-install.ps1
  powershell -NoProfile -ExecutionPolicy unrestricted -Command "Invoke-WebRequest -Uri '%DotNet_Installer_Url%' -OutFile '%DotNet_Path%\dotnet-install.ps1'"
  if not exist "%DotNet_Path%\dotnet-install.ps1" (
    call :print_error_message Failed to download "%DotNet_Path%\dotnet-install.ps1"
    exit /b 1
  )

  call :print_header_message Executing dotnet installer script "%DotNet_Path%\dotnet-install.ps1"
  call :print_header_message Installing .NET Core SDK %DotNet_Version%
  powershell -NoProfile -ExecutionPolicy unrestricted -Command "&'%DotNet_Path%\dotnet-install.ps1' -InstallDir '%DotNet_Path%' -Version '%DotNet_Version%'" || (
    call :print_error_message Failed to install .NET Core SDK %DotNet_Version%
    exit /b 1
  )

  if not exist "%DotNet%" (
    call :print_error_message Failed to install dotnet cli.
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


:print_header_message
  echo/
  echo/------------------------------------------------------------------------------
  echo/  %*
  echo/------------------------------------------------------------------------------
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
