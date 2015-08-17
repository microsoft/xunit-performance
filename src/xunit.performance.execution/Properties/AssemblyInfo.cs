using System.Resources;
using System.Reflection;

[assembly: Xunit.Sdk.PlatformSpecificAssembly]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if DOTNETCORE
[assembly: AssemblyTitle("xunit.performance Execution (.Net Core)")]
#else
[assembly: AssemblyTitle("xunit.performance Execution (Desktop)")]
#endif
[assembly: AssemblyDescription("")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en")]
