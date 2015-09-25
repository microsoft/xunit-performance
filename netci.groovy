// Import the utility functionality.

import jobs.generation.Utilities;

// **************************
// Define code coverage build
// **************************

def project = 'Microsoft/xunit-performance'
// Define build string
def debugBuildString = '''call "C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat" && CIBuild /debug'''
def releaseBuildString = '''call "C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat" && CIBuild /release'''

// Generate the builds for debug and release

def windowsDebugJob = job(Utilities.getFullJobName(project, 'debug', false)) {
  label('windows')
  steps {
    batchFile(debugBuildString)
  }
}

def windowsReleaseJob = job(Utilities.getFullJobName(project, 'release', false)) {
  label('windows')
  steps {
    batchFile(releaseBuildString)
  }
}
             
def windowsDebugPRJob = job(Utilities.getFullJobName(project, 'debug', true)) {
  label('windows')
  steps {
    batchFile(debugBuildString)
  }
}

Utilities.addGithubPRTrigger(windowsDebugPRJob, 'Debug Build')

def windowsReleasePRJob = job(Utilities.getFullJobName(project, 'release', true)) {
  label('windows')
  steps {
    batchFile(releaseBuildString)
  }
}

Utilities.addGithubPRTrigger(windowsReleasePRJob, 'Release Build')

[windowsDebugJob, windowsReleaseJob].each { commitJob ->
    Utilities.addScm(commitJob, project)
    Utilities.addStandardOptions(commitJob)
    Utilities.addStandardNonPRParameters(commitJob)
    Utilities.addGithubPushTrigger(commitJob)
    Utilities.addArchival(commitJob, 'msbuild.log', '', true, false)
}

[windowsDebugPRJob, windowsReleasePRJob].each { PRJob ->
    Utilities.addPRTestSCM(PRJob, project)
    Utilities.addStandardOptions(PRJob)
    Utilities.addStandardPRParameters(PRJob, project)
    Utilities.addArchival(PRJob, 'msbuild.log', '', true, false)
}