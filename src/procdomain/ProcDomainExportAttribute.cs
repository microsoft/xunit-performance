using System;

namespace Microsoft.ProcessDomain
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ProcDomainExportAttribute : Attribute
    {
    }
}
