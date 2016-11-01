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

declare versionSuffix=$2
if [ "$versionSuffix" == "" ]
then
	versionSuffix="build0000"
fi

$dotnetCmd restore
pushd ${currentDir}/src/cli/Microsoft.DotNet.xunit.performance.runner.cli > /dev/null
$dotnetCmd build -c $buildConfiguration --version-suffix $versionSuffix
$dotnetCmd pack -c $buildConfiguration -o $outputDirectory --version-suffix $versionSuffix
popd > /dev/null
pushd ${currentDir}/src/cli/Microsoft.DotNet.xunit.performance.analysis.cli > /dev/null
$dotnetCmd build -c $buildConfiguration --version-suffix $versionSuffix
$dotnetCmd pack -c $buildConfiguration -o $outputDirectory --version-suffix $versionSuffix
popd > /dev/null

echo Build complete
