using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using TaleWorlds.MountAndBlade;

using HarmonyLib;

using Bannerlord.ButterLib.Common.Extensions;

namespace DeservingCompanions
{
    public class SubModule : MBSubModuleBase
    {
        public static readonly string Version = "v1.2.3";
        public static readonly string Name = "DeservingCompanions";
        public static readonly string DisplayName = "Deserving Companions";

        public static SubModule Instance { get; private set; }

        public ILogger Log { get; set; }

        public SubModule()
        {
            Instance = this;
        }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Instance = this;

            this.AddSerilogLoggerProvider($"{Name}.log");

            var harmony = new Harmony("DeservingCompanions");
            harmony.PatchAll();
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            var serviceProvider = Instance.GetServiceProvider();
            Log = serviceProvider.GetRequiredService<ILogger<SubModule>>();
            Log.LogInformation($"Loaded {Name} {Version}.");
            base.OnBeforeInitialModuleScreenSetAsRoot();
        }
    }
}
