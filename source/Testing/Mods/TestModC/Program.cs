namespace TestModC
{
    public class Program : IModV1, ITestHelper, IExports
    {
        public string MyId { get; } = "TestModC";
        public DateTime LoadTime { get; set; }
        public bool ResumeExecuted { get; set; }
        public bool SuspendExecuted { get; set; }

        public const string NonexistingDependencyName = "TestModZ";

        /* Entry point. */
        public Action Disposing { get; }
        public void Start(IModLoaderV1 loader)
        {
            LoadTime = DateTime.Now;
        }

        /* Suspend/Unload */
        public void Suspend() => SuspendExecuted = true;

        public void Resume() => ResumeExecuted = true;

        public void Unload() { }

        public bool CanUnload() => true;
        public bool CanSuspend() => true;
        public Type[] GetTypes() => new [] { typeof(ITestHelper) };
    }
}