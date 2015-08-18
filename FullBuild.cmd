@echo off
goto :main

======================================================================
Performs a clean build and create NuGet packages for this solution
Note that this also modifies the following files, replacing
placeholder version numbers with actual version numbers:
    src\common\GlobalAssemblyInfo.cs
    src\*.nuspec
    src\*\project.json
(see the SetVersionNumber task in xunit.performance.msbuild)

These files will, therefore, show up as modified after the build.
Be careful NOT to check them in that way!

If you publish these packages to Nuget, then please remember to
bump the build number on BuildSemanticVersion below.
======================================================================

:main
setlocal

set BuildAssemblyVersion=1.0.0.0
set BuildSemanticVersion=1.0.0-alpha-build0004

echo Building version %BuildSemanticVersion% NuGet packages.
echo WARNING: Some source files will be modified during this build.
echo WARNING: Please be careful not to check in those modifications.

msbuild.exe /m /nologo /t:CI /v:m /fl xunit.performance.msbuild

goto :eof