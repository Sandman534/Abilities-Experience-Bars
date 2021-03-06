using StardewModdingAPI;

namespace AbilitiesExperienceBars
{
    public class ModConfig
    {
        public SButton ToggleKey { get; set; }
        public SButton ConfigKey { get; set; }
        public SButton ResetKey { get; set; }

        public bool ShowButtons { get; set; }
        public bool ShowExperienceInfo { get; set; }
        public bool ShowBoxBackground { get; set; }
        public bool ShowLevelUp { get; set; }
        public bool ShowUI { get; set; }

        public float LevelUpMessageDuration { get; set; }

        public int mainPosX { get; set; }
        public int mainPosY { get; set; }
        public int mainScale { get; set; }
    }
}
