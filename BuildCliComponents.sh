#!/bin/bash

######################################################################
# Build the components of xunit.performance that support build via the
# Dotnet CLI.
#
# This script will bootstrap the latest version of the CLI into the
# tools directory prior to build.
######################################################################

######################################
## FOR DEBUGGING ONLY
######################################
# set -x

declare currentDir=`pwd`
declare outputDirectory=${currentDir}/LocalPackages
declare dotnetPath=${currentDir}/tools/bin/ubuntu
declare dotnetCmd=${dotnetPath}/dotnet
declare dotnetInstallerUrl=https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.sh
declare dotnetInstallerScript=${dotnetPath}/dotnet-install.sh
declare dotnetVersion=`cat DotNetCliVersion.txt`

if ! [ -f $dotnetCmd ]
then
	echo Installing Dotnet CLI
	if ! [ -f $dotnetPath ]
	then
		mkdir -p $dotnetPath
	fi
	
	curl $dotnetInstallerUrl -o $dotnetInstallerScript
	chmod +x $dotnetInstallerScript

	$dotnetInstallerScript --version $dotnetVersion --install-dir $dotnetPath --no-path
fi

if ! [ -f $dotnetCmd ]
then
	echo Unable to install Dotnet CLI.  Exiting.
	exit -1
fi

declare buildConfiguration=$1
if [ "$buildConfiguration" == "" ]
then
	buildConfiguration="debug"
fi

declare nupkgCount=`ls LocalPackages/*.nupkg | wc -l`
if [ "$nupkgCount" == "0"  ]
then
	echo Linux builds depend on artifacts from Windows build.  Please build on Windows first.
	exit -1
fi

echo Building CLI-based components
pushd ${currentDir}/src/cli/Microsoft.DotNet.xunit.performance.runner.cli > /dev/null
$dotnetCmd restore
$dotnetCmd build -c $buildConfiguration
$dotnetCmd pack -c $buildConfiguration -o $outputDirectory
popd > /dev/null
pushd ${currentDir}/src/cli/Microsoft.DotNet.xunit.performance.analysis.cli > /dev/null
$dotnetCmd restore
$dotnetCmd build -c $buildConfiguration
$dotnetCmd pack -c $buildConfiguration -o $outputDirectory
popd > /dev/null

echo Build complete
