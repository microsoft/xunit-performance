namespace Microsoft.Xunit.Performance.Api
{
    internal struct ETWOutputData
    {
        public string Name { get; set; }

        public string SessionName { get; set; }

        public string KernelFileName { get; set; }

        public string UserFileName { get; set; }
    }
}
