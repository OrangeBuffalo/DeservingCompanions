using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Settings.Base.Global;

namespace DeservingCompanions
{
    class Settings : AttributeGlobalSettings<Settings>
    {
        public override string Id => "DeservingCompanions";
        public override string DisplayName => "Deserving Companions";
        public override string FolderName => "DeservingCompanions";
        public override string FormatType => "json2";

        [SettingPropertyFloatingInteger("Companion XP Gains", 0f, 10f, "0%", Order = 0, RequireRestart = false, HintText = "Adjust the amount of XP gained by companions after a quest (Default=100%).")]
        [SettingPropertyGroup("Deserving Companions")]
        public float XpGainsFactor { get; set; } = 1f;

        [SettingPropertyFloatingInteger("Leadership increase chance", 0f, 10f, "0%", Order = 0, RequireRestart = false, HintText = "Chance of gaining Leadership skill XP after leading men in a quest (Default=50%).")]
        [SettingPropertyGroup("Deserving Companions")]
        public float LeadershipProba { get; set; } = 0.5f;

        [SettingPropertyFloatingInteger("Steward increase chance", 0f, 10f, "0%", Order = 0, RequireRestart = false, HintText = "Chance of gaining Steward skill XP after leading men in a quest (Default=50%).")]
        [SettingPropertyGroup("Deserving Companions")]
        public float StewardProba { get; set; } = 0.5f;

        [SettingPropertyFloatingInteger("Scouting increase chance", 0f, 10f, "0%", Order = 0, RequireRestart = false, HintText = "Chance of gaining Scouting skill XP after leading men in a quest (Default=50%).")]
        [SettingPropertyGroup("Deserving Companions")]
        public float ScoutingProba { get; set; } = 0.5f;

        [SettingPropertyFloatingInteger("Medicine increase chance", 0f, 10f, "0%", Order = 0, RequireRestart = false, HintText = "Chance of gaining Medicine skill XP after leading men in a quest (Default=25%).")]
        [SettingPropertyGroup("Deserving Companions")]
        public float MedicineProba { get; set; } = 0.25f;

        [SettingPropertyFloatingInteger("Tactics increase chance", 0f, 10f, "0%", Order = 0, RequireRestart = false, HintText = "Chance of gaining Tactics skill XP after leading men in a quest (Default=25%).")]
        [SettingPropertyGroup("Deserving Companions")]
        public float TacticsProba { get; set; } = 0.25f;
    }
}