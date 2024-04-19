using StardewModdingAPI;

namespace AbilitiesExperienceBars
{
    public class ModConfig
    {
        public SButton ToggleKey { get; set; }
        public SButton ConfigKey { get; set; }
        public SButton ResetKey { get; set; }

        public string UITheme { get; set; }

        public bool ShowButtons { get; set; }
        public bool ShowExperienceInfo { get; set; }
        public bool ShowBoxBackground { get; set; }
        public bool ShowLevelUp { get; set; }
        public bool ShowUI { get; set; }

        public bool ShowExpPopup { get; set; }
        public bool ShowExpPopupTest { get; set; }
        public bool ShowExperiencePopupInfo { get; set; }

        public float LevelUpMessageDuration { get; set; }
        public float PopupMessageDuration { get; set; }

        public int mainPosX { get; set; }
        public int mainPosY { get; set; }
        public int mainScale { get; set; }

        public int popupPosX { get; set; }
        public int popupPosY { get; set; }
        public int popupScale { get; set; }
    }
}
