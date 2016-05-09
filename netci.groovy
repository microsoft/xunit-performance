// Import the utility functionality.

import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName
def projectFolder = Utilities.getFolderName(project) + '/' + Utilities.getFolderName(branch)

// Windows build jobs

[true, false].each { isPR ->
    ['Debug','Release'].each { configuration ->
        def os = 'Windows_NT';
        def lowerConfigurationName = configuration.toLowerCase();
        def newBuildJobName = "${os.toLowerCase()}_${lowerConfigurationName}";
        def newJob = job(Utilities.getFullJobName(project, newBuildJobName, isPR)) {
            steps {
                batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat\" && CIBuild /${lowerConfigurationName}")
                batchFile("C:\\Packer\\Packer.exe .\\LocalPackages.pack . LocalPackages")
            }
        }
        
        Utilities.setMachineAffinity(newJob, "${os}", 'latest-or-auto')
        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
        Utilities.addArchival(newJob, 'LocalPackages.pack')
        Utilities.addArchival(newJob, 'msbuild.log', '', true, false)
        if (isPR) {
            Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} ${configuration} Build")
        }
        else {
            Utilities.addGithubPushTrigger(newJob)
        }
    }
}


// Linux build jobs

[true, false].each { isPR ->
    ['Ubuntu'].each { os ->
        ['Debug','Release'].each { configuration ->

			// Global job vars.
			def lowerConfigurationName = configuration.toLowerCase();
			def linuxFlowJobName = "LinuxFlow_${os}_${lowerConfigurationName}"

		    // Setup a Windows job to build csproj-based components.
			def winOS = 'Windows_NT';
            def winBuildJobName = "${winOS.toLowerCase()}_${lowerConfigurationName}";
            def newWinJob = job(Utilities.getFullJobName(project, winBuildJobName, isPR)) {
				steps {
			        batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat\" && CIBuild /${lowerConfigurationName}")
			        batchFile("C:\\Packer\\Packer.exe .\\LocalPackages.pack . LocalPackages")
                }
			}
        
            Utilities.setMachineAffinity(newWinJob, "${winOS}", 'latest-or-auto')
            Utilities.standardJobSetup(newWinJob, project, isPR, "*/${branch}")
            Utilities.addArchival(newWinJob, 'LocalPackages.pack')
            Utilities.addArchival(newWinJob, 'msbuild.log', '', true, false)

			// Setup a Linux job to build the Linux components.
			def fullWinBuildJobName = projectFolder + '/' + winBuildJobName
            def linuxBuildJobName = "${os.toLowerCase()}_${lowerConfigurationName}";
            def newLinuxJob = job(Utilities.getFullJobName(project, linuxBuildJobName, isPR)) {
                steps {
					copyArtifacts(fullWinBuildJobName) {
						includePatterns('*.pack')
						buildSelector {
							buildNumber('\${WIN_BUILD}')
						}
					}

					batchFile("PowerShell -command \"\"C:\\Packer\\unpacker.ps1 .\\LocalPackages.pack . > .\\bin\\unpacker.log\"\"")
                    shell("ls -lh")
					shell("ls -lh LocalPackages")
                }

				parameters {
					stringParam('WIN_BUILD', '', 'Build number to use for copying binaries for Linux build.')
				}
            }
        
            Utilities.setMachineAffinity(newLinuxJob, "${os}", 'latest-or-auto')
            Utilities.standardJobSetup(newLinuxJob, project, isPR, "*/${branch}")

			// Setup a flow job to orchestrate things.
			def fullXunitPerformanceTestJobName = projectFolder + '/' + newLinuxJob.name
			def newLinuxFlowJob = buildFlowJob(Utilities.getFullJobName(project, linuxFlowJobName, isPR)) {
				buildFlow("""
					b = build(params, '${fullWinBuildJobName}')
					build(params +
					[WIN_BUILD: b.build.number], '${fullXunitPerformanceTestJobName}')
					""")
			}

			Utilities.setMachineAffinity(newLinuxFlowJob, "${os}", 'latest-or-auto')
			Utilities.standardJobSetup(newLinuxFlowJob, project, isPR, "*/${branch}")

            if (isPR) {
                Utilities.addGithubPRTriggerForBranch(newLinuxFlowJob, branch, "${os} ${configuration} Build")
            }
            else {
                Utilities.addGithubPushTrigger(newLinuxFlowJob)
            }
        }
    }
}