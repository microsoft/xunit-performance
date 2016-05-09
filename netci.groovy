// Import the utility functionality.

import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName

// Standard build jobs

[true, false].each { isPR ->
    ['Windows_NT', 'Ubuntu'] { os ->
        ['Debug','Release'].each { configuration ->
            def lowerConfigurationName = configuration.toLowerCase();
            def newJob = job(Utilities.getFullJobName(project, lowerConfigurationName, isPR)) {
                steps {
                    if (os == 'Windows_NT') {
                        batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat\" && CIBuild /${lowerConfigurationName}")
                    }
                    else if (os == 'Ubuntu') {
                        shell("uname -a")
                    }
                }
            }
        
            Utilities.setMachineAffinity(newJob, ${os}, 'latest-or-auto')
            Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
            if (os == 'Windows_NT') {
                Utilities.addArchival(newJob, 'msbuild.log', '', true, false)
            }
            if (isPR) {
                Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} ${configuration} Build")
            }
            else {
                Utilities.addGithubPushTrigger(newJob)
            }
        }
    }
}