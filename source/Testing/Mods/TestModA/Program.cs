namespace TestModA
{
    public class Program : IModV2, ITestHelper, ITestModA, IExports
    {
        public string MyId { get; } = "TestModA";
        public DateTime LoadTime { get; set; }
        public bool ResumeExecuted { get; set; }
        public bool SuspendExecuted { get; set; }

        private IController _controller;

        /* Entry point. */
        public Action Disposing { get; }
        public void StartEx(IModLoaderV1 loader, IModConfigV1 modConfig)
        {
            LoadTime = DateTime.Now;
            _controller = new Controller();
            
            loader.AddOrReplaceController<IController>(this, _controller);
        }

        /* Suspend/Unload */
        public void Suspend()
        {
            SuspendExecuted = true;
        }

        public void Resume()
        {
            ResumeExecuted = true;
        }

        public void Unload()
        {
            
        }

        public bool CanUnload() => true;
        public bool CanSuspend() => true;

        /* Extensions */
        public int GetControllerValue() => _controller.Number;
        public Type[] GetTypes() => new []{ typeof(ITestModAPlugin), typeof(IController) };
    }
}