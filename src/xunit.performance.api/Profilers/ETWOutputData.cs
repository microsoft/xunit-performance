namespace Microsoft.Xunit.Performance.Api
{
    struct ETWOutputData
    {
        public string KernelFileName { get; set; }
        public string Name { get; set; }

        public string SessionName { get; set; }
        public string UserFileName { get; set; }
    }
}