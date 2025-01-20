namespace TestModB
{
    public class Program : IModV1, ITestHelper, ITestModB
    {
        public string MyId { get; } = "TestModB";
        public DateTime LoadTime { get; set; }
        public bool ResumeExecuted { get; set; }
        public bool SuspendExecuted { get; set; }

        public const string NonexistingDependencyName = "TestModX";
        private IModLoader _loader;
        private WeakReference<ITestModAPlugin> _plugin;

        /* Entry point. */
        public Action Disposing { get; }
        public void Start(IModLoaderV1 loader)
        {
            LoadTime = DateTime.Now;
            _loader = (IModLoader) loader;
            _plugin = _loader.MakeInterfaces<ITestModAPlugin>()[0];
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
        public void ModifyControllerValueFromTestModA(int newValue)
        {
            var weakController = _loader.GetController<IController>();
            weakController.TryGetTarget(out IController target);
            target.Number = newValue;
        }

        public int UsePluginFromTestModA(int value)
        {
            if (_plugin.TryGetTarget(out var target))
            {
                return target.MultiplyByTwo(value);
            } 

            throw new NullReferenceException();
        }
    }
}