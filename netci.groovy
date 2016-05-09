// Import the utility functionality.

import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName

// Windows build jobs

[true, false].each { isPR ->
    ['Debug','Release'].each { configuration ->
        def os = 'Windows_NT';
        def lowerConfigurationName = configuration.toLowerCase();
        def newBuildJobName = "${os.toLowerCase()}_${lowerConfigurationName}";
        def newJob = job(Utilities.getFullJobName(project, newBuildJobName, isPR)) {
            steps {
                // batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat\" && CIBuild /${lowerConfigurationName}")
                batchFile("dir /s")
            }
        }
        
        Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-or-auto')
        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
        Utilities.addArchival(newJob, 'msbuild.log', '', true, false)
        if (isPR) {
            Utilities.addGithubPRTriggerForBranch(newJob, branch, "Windows ${configuration} Build")
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
            def lowerConfigurationName = configuration.toLowerCase();
            def newBuildJobName = "${os.toLowerCase()}_${lowerConfigurationName}";
            def newJob = job(Utilities.getFullJobName(project, newBuildJobName, isPR)) {
                steps {
                    shell("uname -a")
                    shell("ls -lh")
                }
            }
        
            Utilities.setMachineAffinity(newJob, "${os}", 'latest-or-auto')
            Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
            if (isPR) {
                Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} ${configuration} Build")
            }
            else {
                Utilities.addGithubPushTrigger(newJob)
            }
        }
    }
}