@echo off
goto :main

:main
setlocal

@echo ==== Main build ====
msbuild.exe /m /nologo /v:m /fl /t:DevBuild xunit.performance.msbuild

@echo === Clean samples ===
msbuild.exe /m /nologo /v:m /fl /t:Clean samples\samples.sln

@echo === Resture Nuget packages for samples ===
.nuget\nuget.exe restore samples\Samples.sln -NonInteractive

@echo === Build samples ===
msbuild.exe /m /nologo /v:m /fl /t:Build samples\samples.sln

goto :eof
