@echo off
goto :main

:main
setlocal
  set errorlevel=
  call "%~dp0.\build.cmd" --api-only %*
endlocal& exit /b %errorlevel%
