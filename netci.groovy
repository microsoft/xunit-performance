// Import the utility functionality.

import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName

// Standard build jobs

[true, false].each { isPR ->
    ['Debug','Release'].each { configuration ->
        def lowerConfigurationName = configuration.toLowerCase();
        def newJob = job(Utilities.getFullJobName(project, lowerConfigurationName, isPR)) {
            steps {
                batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat\" && CIBuild /${lowerConfigurationName}")
            }
        }
        
        Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-or-auto')
        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
        Utilities.addArchival(newJob, 'msbuild.log', '', true, false)
        if (isPR) {
            Utilities.addGithubPRTriggerForBranch(newJob, branch, "${configuration} Build")
        }
        else {
            Utilities.addGithubPushTrigger(newJob)
        }
    }
}