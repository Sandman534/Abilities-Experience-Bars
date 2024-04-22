using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AbilitiesExperienceBars
{
    public class skillHolder
    {
        // Skill ID
        public string skillID;

        // Skill Vectores
        public Rectangle smallIcon;
        public Rectangle bigIcon;

        // Skill Colors
        public Color skillColor;
        public Color skillRestorationColor;
        public Color skillFinalColor = new Color(150, 175, 55);
        public Color skillGoldColor = new Color(150, 175, 55);

        // EXP and Level tracking
        public int currentEXP;
        public int previousEXP;
        public int currentLevel;
        public int previousLevel;

        // Animation tracking
        public bool animateSkill;
        public bool expIncreasing;
        public bool expPopup;
        public bool actualExpGainedMessage;
        public int expGained;
        public byte expAlpha;
        public bool inIncrease;
        public bool inWait;
        public bool inDecrease;
        public int timeExpMessageLeft;

        // Mastery Test
        public bool isMastery;
        public int maxLevel;

        // API Interface
        ISpaceCoreApi _spaceCoreAPI;

        public skillHolder(IModHelper Helper, string ID, int skillIndex, Color skillColorCode)
        {
            // Set the skill ID
            skillID = ID;

            // Setup API
            _spaceCoreAPI = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");

            // Load Skill Icon
            setSkillIcon(skillIndex);

            // Load Colors
            skillColor = skillColorCode;
            skillRestorationColor = skillColorCode;

            // Mastery or regular skill
            if (skillID == "mastery")
            {
                isMastery = true;
                maxLevel = 5;
            }
            else
            {
                isMastery = false;
                maxLevel = 10;
            }

            // Set Current Data
            setSkillData(true);
        }

        public void setSkillIcon(int skillIndex)
        {
            // Change the Y postion based on the skill index
            int xPosition = 10 * ((skillIndex % 6 > 0 ? skillIndex % 6 : 6) - 1);
            int yPosition = 64 + (10 * (skillIndex % 6 > 0 ? skillIndex / 6 : (skillIndex / 6) - 1));
            smallIcon = new Rectangle(xPosition, yPosition, 10, 10);

            // Change the Y postion based on the skill index
            xPosition = 16 * ((skillIndex % 6 > 0 ? skillIndex % 6 : 6) - 1);
            yPosition = 16 * (skillIndex % 6 > 0 ? skillIndex / 6 : (skillIndex / 6) - 1);
            bigIcon = new Rectangle(xPosition, yPosition, 16, 16);
        }

        public void setSkillData(bool isCurrent)
        {
            // Stardew Base Skills
            if (skillID == "farming")
                setData(Game1.player.farmingLevel.Value, Game1.player.experiencePoints[0], isCurrent);
            else if (skillID == "fishing")
                setData(Game1.player.fishingLevel.Value, Game1.player.experiencePoints[1], isCurrent);
            else if (skillID == "foraging")
                setData(Game1.player.foragingLevel.Value, Game1.player.experiencePoints[2], isCurrent);
            else if (skillID == "mining")
                setData(Game1.player.miningLevel.Value, Game1.player.experiencePoints[3], isCurrent);
            else if (skillID == "combat")
                setData(Game1.player.combatLevel.Value, Game1.player.experiencePoints[4], isCurrent);
            else if (skillID == "luck")
                setData(Game1.player.luckLevel.Value, Game1.player.experiencePoints[5], isCurrent);
            else if (skillID == "mastery")
                setData((int)Game1.stats.Get("masteryLevelsSpent"), (int)Game1.stats.Get("MasteryExp"), isCurrent);

            // Mod Added Skills
            else if (_spaceCoreAPI != null)
                setData(_spaceCoreAPI.GetLevelForCustomSkill(Game1.player, skillID), _spaceCoreAPI.GetExperienceForCustomSkill(Game1.player, skillID), isCurrent);
        }

        private void setData(int iLevel, int iExp, bool bCurrent)
        {
            if (bCurrent)
            {
                currentLevel = iLevel;
                currentEXP = iExp;
            }
            else
            {
                previousLevel = iLevel;
                previousEXP = iExp;
            }
        }

        public void ExperienceAlpha(byte intensity)
        {
            if (inIncrease)
            {
                int virtualAlphaValue = expAlpha + intensity;
                if (virtualAlphaValue < 255) 
                    expAlpha += intensity;
                else
                {
                    expAlpha = 255;
                    inIncrease = false;
                    inWait = true;
                }
            }
            else if (inWait)
            {
                if (timeExpMessageLeft > 0)
                    timeExpMessageLeft--;
                else
                {
                    inWait = false;
                    inDecrease = true;
                }
            }
            else if (inDecrease)
            {
                int virtualAlphaValue = expAlpha - intensity;
                if (virtualAlphaValue > 0)
                    expAlpha -= intensity;
                else
                {
                    expAlpha = 0;
                    inDecrease = false;
                    actualExpGainedMessage = false;
                }
            }
        }

        public bool GainLevel()
        {
            if (currentLevel == previousLevel) return false;

            // Set Level
            previousLevel = currentLevel;
            return true;
        }

        public void GainExperience()
        {
            if (currentEXP == previousEXP) return;

            // Set Experience Values
            expGained = currentEXP - previousEXP;
            previousEXP = currentEXP;

            // Set Experience Values
            inIncrease = true;
            actualExpGainedMessage = true;
            timeExpMessageLeft = 3 * 60;
            expAlpha = 0;

            // Set Experience Bools
            expPopup = true;
            expIncreasing = true;
            animateSkill = true;

        }
    }

    public class ModEntry : StardewModdingAPI.Mod
    {
        #region // Variables

        // Holds all player skills
        private List<skillHolder> playerSkills = new List<skillHolder>();

        // Private Variables
        private int configButtonPosX = 25, configButtonPosY = 10;
        private int defaultButtonPosX = 25, defaultButtonPosY = 10;
        private int levelUpPosY;
        private bool inConfigMode;
        private int expAdvicePositionX;

        // Sprite Variables
        private Texture2D iconSheet, barSheet, barFiller, backgroundConfig;

        // Sprite Locations
        private Rectangle backgroundTop = new Rectangle(98, 0, 116, 5);
        private Rectangle backgroundMiddle = new Rectangle(98, 5, 116, 1);
        private Rectangle backgroundBottom = new Rectangle(98, 6, 116, 5);

        private Rectangle backgroundBar = new Rectangle(0, 0, 98, 22);
        private Rectangle backgroundLevelUp = new Rectangle(0, 22, 86, 37);
        private Rectangle backgroundExp = new Rectangle(0, 59, 34, 17);

        private Rectangle buttonConfig = new Rectangle(98, 11, 18, 18);
        private Rectangle buttonConfigApply = new Rectangle(116, 11, 18, 18);
        private Rectangle buttonVisibility = new Rectangle(98, 29, 18, 18);
        private Rectangle buttonHidden = new Rectangle(116, 29, 18, 18);

        private Rectangle buttonBackground = new Rectangle(134, 11, 7, 8);
        private Rectangle buttonLevel = new Rectangle(141, 11, 7, 8);
        private Rectangle buttonExperience = new Rectangle(148, 11, 7, 8);
        private Rectangle buttonIncrease = new Rectangle(134, 19, 7, 8);
        private Rectangle buttonDecrease = new Rectangle(141, 19, 7, 8);
        private Rectangle buttonReset = new Rectangle(155, 11, 13, 13);

        // EXP and Level Text
        int[] expTextOffeset = new int[6] { 0, 19, 21, 22, 22, 22 };
        int[] levelPosition = new int[11] { 29, 28, 29, 29, 29, 29, 29, 28, 28, 28, 31 };

        // Color Variables
        private Color globalChangeColor = Color.White;
        private Color decreaseSizeButtonColor = Color.White,
            increaseSizeButtonColor = Color.White,
            backgroundButtonColor = Color.White,
            levelUpButtonColor = Color.White,
            experienceButtonColor = Color.White;

        // Global Info Variables
        private int barSpacement = 10;
        public bool luckCompatibility, cookingCompatibility, magicCompatibility, loveCookingCompatibility,
            luckCheck, cookingCheck, magicCheck, loveCookingCheck;

        // Animation Variables
        public bool animatingBox, animatingLevelUp;
        public Vector2 animDestPosBox, animDestPosLevelUp;
        public string animBoxDir, animLevelUpDir;

        // Control Variables
        private bool draggingBox;
        private bool canShowLevelUp;
        private bool canCountTimer;
        private bool canCountPopupTimer;

        // Data Variables
        public ModEntry instance;
        private ModConfig config;

        // Mastery Variables
        private List<string> masterySkills = new List<string> { "farming", "fishing", "foraging", "mining", "combat" };
        private bool masteryProcessed;

        // Timer Variables
        private float timeLeft;
        private float timeLeftPopup;

        // Level Up Variables
        private Rectangle levelUpSource;
        private string levelUpMessage;
        private string levelUpID = "abilitybars.LevelUp";

        // Experience Popup
        private skillHolder popupSkill;
        private bool sampleRunOnce;

        // Load Control
        private bool loadedSaveFlag;

        // Mouse Check Control
        private int[] mainX = new int[5] { 0, 25, 75, 100, 125 };
        private int[] mainY = new int[2] { 0, -30 };

        #endregion

        public override void Entry(IModHelper helper)
        {
            instance = this;
            configGet();
            loadTextures();
            loadSound();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Display.RenderedHud += onRenderedHud;
            helper.Events.GameLoop.UpdateTicked += onUpdate;
            helper.Events.Input.ButtonPressed += onButtonPressed;
            helper.Events.Input.ButtonReleased += onButtonReleased;
            helper.Events.GameLoop.SaveLoaded += onSaveLoaded;
            helper.Events.Player.Warped += onPlayerWarped;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Get Installed Themes
            var rootDir = Path.Combine(Helper.DirectoryPath, "assets", "ui", "themes");
            string[] filePaths = Directory.GetFiles(rootDir, "*.png", SearchOption.TopDirectoryOnly).Select(Path.GetFileNameWithoutExtension).ToArray();

            // Config Menu
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu != null)
            {
                configMenu.Register(
                    mod: ModManifest,
                    reset: () => config = new ModConfig(),
                    save: () => Helper.WriteConfig(config)
                );

                configMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("Theme"),
                    tooltip: () => Helper.Translation.Get("ThemeT"),
                    allowedValues: filePaths,
                    getValue: () => config.UITheme,
                    setValue: value => reloadUI(value)
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("SmallIcon"),
                    tooltip: () => Helper.Translation.Get("SmallIconT"),
                    getValue: () => config.SmallIcons,
                    setValue: value => config.SmallIcons = value
                );

                // Keybinds
                configMenu.AddSectionTitle(
                    mod: ModManifest,
                    text: () => Helper.Translation.Get("KeybindsM")
                );
                configMenu.AddKeybind(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ToggleKey"),
                    tooltip: () => Helper.Translation.Get("ToggleKeyT"),
                    getValue: () => config.ToggleKey,
                    setValue: value => config.ToggleKey = value
                );
                configMenu.AddKeybind(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ConfigKey"),
                    tooltip: () => Helper.Translation.Get("ConfigKeyT"),
                    getValue: () => config.ConfigKey,
                    setValue: value => config.ConfigKey = value
                );
                configMenu.AddKeybind(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ResetKey"),
                    tooltip: () => Helper.Translation.Get("ResetKeyT"),
                    getValue: () => config.ResetKey,
                    setValue: value => config.ResetKey = value
                );

                // Interface
                configMenu.AddSectionTitle(
                    mod: ModManifest,
                    text: () => Helper.Translation.Get("ExperienceBarM")
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ShowButtons"),
                    tooltip: () => Helper.Translation.Get("ShowButtonsT"),
                    getValue: () => config.ShowButtons,
                    setValue: value => config.ShowButtons = value
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ShowUI"),
                    tooltip: () => Helper.Translation.Get("ShowUIT"),
                    getValue: () => config.ShowUI,
                    setValue: value => config.ShowUI = value
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ShowBoxBackground"),
                    tooltip: () => Helper.Translation.Get("ShowBoxBackgroundT"),
                    getValue: () => config.ShowBoxBackground,
                    setValue: value => config.ShowBoxBackground = value
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ShowExperienceInfo"),
                    tooltip: () => Helper.Translation.Get("ShowExperienceInfoT"),
                    getValue: () => config.ShowExperienceInfo,
                    setValue: value => config.ShowExperienceInfo = value
                );
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("mainPosX"),
                    tooltip: () => Helper.Translation.Get("mainPosXT"),
                    getValue: () => config.mainPosX,
                    setValue: value => config.mainPosX = value
                );
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("mainPosY"),
                    tooltip: () => Helper.Translation.Get("mainPosYT"),
                    getValue: () => config.mainPosY,
                    setValue: value => config.mainPosY = value
                );
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("mainScale"),
                    tooltip: () => Helper.Translation.Get("mainScaleT"),
                    min: 1,
                    max: 5,
                    interval: 1,
                    getValue: () => config.mainScale,
                    setValue: value => config.mainScale = value
                );

                // Experience Popup
                configMenu.AddSectionTitle(
                    mod: ModManifest,
                    text: () => Helper.Translation.Get("ExperiencePopupM")
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ShowPopup"),
                    tooltip: () => Helper.Translation.Get("ShowPopupT"),
                    getValue: () => config.ShowExpPopup,
                    setValue: value => config.ShowExpPopup = value
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ShowExperienceInfo"),
                    tooltip: () => Helper.Translation.Get("ShowExperienceInfoT"),
                    getValue: () => config.ShowExperiencePopupInfo,
                    setValue: value => config.ShowExperiencePopupInfo = value
                );
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("PopupMessageDuration"),
                    tooltip: () => Helper.Translation.Get("PopupMessageDurationT"),
                    getValue: () => config.PopupMessageDuration,
                    setValue: value => config.PopupMessageDuration = value
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ShowExpPopupTest"),
                    tooltip: () => Helper.Translation.Get("ShowExpPopupTestT"),
                    getValue: () => config.ShowExpPopupTest,
                    setValue: value => config.ShowExpPopupTest = value
                );
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("popupPosX"),
                    tooltip: () => Helper.Translation.Get("popupPosXT"),
                    getValue: () => config.popupPosX,
                    setValue: value => config.popupPosX = value
                );
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("popupPosY"),
                    tooltip: () => Helper.Translation.Get("popupPosYT"),
                    getValue: () => config.popupPosY,
                    setValue: value => config.popupPosY = value
                );
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("popupScale"),
                    tooltip: () => Helper.Translation.Get("popupScaleT"),
                    min: 1,
                    max: 5,
                    interval: 1,
                    getValue: () => config.popupScale,
                    setValue: value => config.popupScale = value
                );

                // Level Up Windows
                configMenu.AddSectionTitle(
                    mod: ModManifest,
                    text: () => Helper.Translation.Get("LevelUpM")
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ShowLevelUp"),
                    tooltip: () => Helper.Translation.Get("ShowLevelUpT"),
                    getValue: () => config.ShowLevelUp,
                    setValue: value => config.ShowLevelUp = value
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("LevelUpSound"),
                    tooltip: () => Helper.Translation.Get("LevelUpSoundT"),
                    getValue: () => config.LevelUpSound,
                    setValue: value => config.LevelUpSound = value
                );
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("LevelUpMessageDuration"),
                    tooltip: () => Helper.Translation.Get("LevelUpMessageDurationT"),
                    getValue: () => config.LevelUpMessageDuration,
                    setValue: value => config.LevelUpMessageDuration = value
                );
            }
        }

        private void reloadUI(string value)
        {
            config.UITheme = value;
            loadTextures();
        }

        private void repositionExpInfo()
        {
            if (!Context.IsWorldReady) return;

            int rightPosX = this.config.mainPosX + backgroundTop.Width * this.config.mainScale;
            if (rightPosX >= Game1.uiViewport.Width - (backgroundExp.Width * this.config.mainScale))
                expAdvicePositionX = -(backgroundExp.Width * this.config.mainScale + (10 * this.config.mainScale));
            else
                expAdvicePositionX = backgroundTop.Width * this.config.mainScale + 1;
        }

        private void onPlayerWarped(object sender, WarpedEventArgs e)
        {
            configButtonPosX = MyHelper.AdjustPositionMineLevelWidth(configButtonPosX, e.NewLocation, defaultButtonPosX);
        }

        private void configGet()
        {
            this.config = this.Helper.ReadConfig<ModConfig>();
            configAdjust();
        }

        private void configSave()
        {
            this.Helper.WriteConfig(config);
        }

        private void configAdjust()
        {
            // Main Window Scale Catch
            if (this.config.mainScale < 1 || this.config.mainScale == 1)
            {
                this.config.mainScale = 1;
                decreaseSizeButtonColor = MyHelper.ChangeColorIntensity(Color.DarkGray, 1, 0.7f);
            }
            else
                decreaseSizeButtonColor = Color.White;

            if (this.config.mainScale > 5 || this.config.mainScale == 5)
            {
                this.config.mainScale = 5;
                increaseSizeButtonColor = MyHelper.ChangeColorIntensity(Color.DarkGray, 1, 0.7f);
            }
            else
                increaseSizeButtonColor = Color.White;

            // Popup Scale Catch
            if (this.config.popupScale < 1)
                this.config.popupScale = 1;

            if (this.config.popupScale > 5)
                this.config.popupScale = 5;

            // Adjust Background Button color
            if (!this.config.ShowBoxBackground)
                backgroundButtonColor = MyHelper.ChangeColorIntensity(Color.DarkGray, 1, 0.7f);
            else
                backgroundButtonColor = Color.White;

            // Adjust Level up Button color
            if (!this.config.ShowLevelUp)
                levelUpButtonColor = MyHelper.ChangeColorIntensity(Color.DarkGray, 1, 0.7f);
            else
                levelUpButtonColor = Color.White;

            // Adjust Experience Button color
            if (!this.config.ShowExperienceInfo)
                experienceButtonColor = MyHelper.ChangeColorIntensity(Color.DarkGray, 1, 0.7f);
            else
                experienceButtonColor = Color.White;

            // Level up message duration catch
            if (this.config.LevelUpMessageDuration < 1)
                this.config.LevelUpMessageDuration = 1;

            // Popup message duration catch
            if (this.config.PopupMessageDuration < 1)
                this.config.PopupMessageDuration = 1;

            // Default UI theme catch
            if (this.config.UITheme == null)
                this.config.UITheme = "Vanilla";

            configSave();
        }

        private void configReset()
        {
            // Experience Bar
            this.config.ShowUI = true;
            this.config.ShowBoxBackground = true;
            this.config.ShowExperienceInfo = true;
            this.config.mainPosX = 25;
            this.config.mainPosY = 125;
            this.config.mainScale = 3;

            // Popup Settings
            this.config.ShowExpPopup = false;
            this.config.ShowExpPopupTest = false;
            this.config.ShowExperiencePopupInfo = true;
            this.config.PopupMessageDuration = 4;
            this.config.popupPosX = 25;
            this.config.popupPosY = 800;
            this.config.popupScale = 3;

            // Level Up
            this.config.ShowLevelUp = true;
            this.config.LevelUpSound = true;
            this.config.LevelUpMessageDuration = 4;

            // Save Info
            configSave();
            configAdjust();
        }

        private void loadSound()
        {
            // Create cue definition
            CueDefinition newCueDefinition = new() { name = levelUpID };

            // Load the file into a stream
            var path = Path.Combine(Helper.DirectoryPath, "assets", "sound", "LevelUp.wav");
            SoundEffect levelUp = SoundEffect.FromStream(new FileStream(path, FileMode.Open));

            // Set the cue definition and load it into the soundbank
            newCueDefinition.SetSound(levelUp, Game1.audioEngine.GetCategoryIndex("Sound"));
            Game1.soundBank.AddCue(newCueDefinition);
        }

        private void loadTextures()
        {
            // Load Images
            string uiPath = config.UITheme == null ? $"assets/ui/themes/Vanilla.png" : $"assets/ui/themes/{config.UITheme}.png";
            barSheet = Helper.ModContent.Load<Texture2D>(uiPath);
            iconSheet = Helper.ModContent.Load<Texture2D>("assets/ui/icons.png");

            barFiller = Helper.ModContent.Load<Texture2D>("assets/ui/barFiller.png");
            backgroundConfig = Helper.ModContent.Load<Texture2D>("assets/ui/backgroundBoxConfig.png");
        }

        private void loadSkills()
        {
            // Add Base Skills
            playerSkills.Add(new skillHolder(Helper, "farming", 1, new Color(115, 150, 56)));
            playerSkills.Add(new skillHolder(Helper, "fishing", 2, new Color(117, 150, 150)));
            playerSkills.Add(new skillHolder(Helper, "foraging", 6, new Color(145, 102, 0)));
            playerSkills.Add(new skillHolder(Helper, "mining", 3, new Color(150, 80, 120)));
            playerSkills.Add(new skillHolder(Helper, "combat", 4, new Color(150, 31, 0)));

            // Mod Compatibility
            if (this.Helper.ModRegistry.IsLoaded("spacechase0.LuckSkill"))
                playerSkills.Add(new skillHolder(Helper, "luck", 5, new Color(150, 150, 0)));
            if (this.Helper.ModRegistry.IsLoaded("spacechase0.CookingSkill"))
                playerSkills.Add(new skillHolder(Helper, "cooking", 12, new Color(165, 100, 30)));
            if (this.Helper.ModRegistry.IsLoaded("moonslime.CookingSkill"))
                playerSkills.Add(new skillHolder(Helper, "moonslime.Cooking", 10, new Color(165, 100, 30)));
            if (this.Helper.ModRegistry.IsLoaded("blueberry.LoveOfCooking"))
                playerSkills.Add(new skillHolder(Helper, "blueberry.LoveOfCooking.CookingSkill", 11, new Color(150, 55, 5)));
            if (this.Helper.ModRegistry.IsLoaded("moonslime.ArchaeologySkill"))
                playerSkills.Add(new skillHolder(Helper, "moonslime.Archaeology", 7, new Color(84, 32, 0)));
            if (this.Helper.ModRegistry.IsLoaded("drbirbdev.SocializingSkill"))
                playerSkills.Add(new skillHolder(Helper, "drbirbdev.Socializing", 9, new Color(221, 0, 59)));
            if (this.Helper.ModRegistry.IsLoaded("Achtuur.StardewTravelSkill"))
                playerSkills.Add(new skillHolder(Helper, "Achtuur.Travelling", 13, new Color(73, 100, 98)));
            if (this.Helper.ModRegistry.IsLoaded("drbirbdev.BinningSkill"))
                playerSkills.Add(new skillHolder(Helper, "drbirbdev.Binning", 8, new Color(60, 60, 77)));
            if (this.Helper.ModRegistry.IsLoaded("spacechase0.Magic"))
                playerSkills.Add(new skillHolder(Helper, "magic", 14, new Color(155, 25, 135)));
        }

        private void onRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // Show Level up Window
            if (canShowLevelUp && this.config.ShowLevelUp)
            {
                e.SpriteBatch.Draw(barSheet, 
                    new Rectangle((Game1.uiViewport.Width / 2) - (backgroundLevelUp.Width * 3) / 2, levelUpPosY, backgroundLevelUp.Width * 3, backgroundLevelUp.Height * 3),
                    backgroundLevelUp,
                    Color.White);

                if (config.SmallIcons)
                    e.SpriteBatch.Draw(iconSheet, 
                        new Rectangle(((Game1.uiViewport.Width / 2) - (10 * 3) / 2),levelUpPosY + 15, 10 * 3, 10 * 3),
                        levelUpSource,
                        Color.White);
                else
                    e.SpriteBatch.Draw(iconSheet,
                        new Rectangle(((Game1.uiViewport.Width / 2) - (16 * 3) / 2),levelUpPosY + 9,16 * 3,16 * 3),
                        levelUpSource,
                        Color.White);


                Vector2 centralizedStringPos = MyHelper.GetStringCenter(levelUpMessage, Game1.dialogueFont);
                e.SpriteBatch.DrawString(Game1.dialogueFont, 
                    levelUpMessage, 
                    new Vector2((Game1.uiViewport.Width / 2) - centralizedStringPos.X + 5, MyHelper.AdjustLanguagePosition(levelUpPosY + 58, Helper.Translation.LocaleEnum.ToString())), 
                    Color.Black);
            }

            if (!Context.IsWorldReady || Game1.CurrentEvent != null) return;

            
            // Test Experience Popup
            if (this.config.ShowExpPopupTest)
            {
                canCountPopupTimer = true;
                popupSkill = playerSkills[0];
                sampleRunOnce = false;
            }
            else if (!sampleRunOnce)
            {
                canCountPopupTimer = false;
                popupSkill = null;
                sampleRunOnce = true;
            }

            // Experience Popup
            if (canCountPopupTimer && popupSkill != null && this.config.ShowExpPopup && popupSkill.currentLevel < popupSkill.maxLevel)
            {

                // Bar position
                int barPosX = this.config.popupPosX;
                int barPosY = this.config.popupPosY;

                // Draw bars background
                e.SpriteBatch.Draw(barSheet,
                    new Rectangle(barPosX, barPosY, backgroundBar.Width * this.config.popupScale, backgroundBar.Height * this.config.popupScale),
                    backgroundBar,
                    globalChangeColor);


                // Draw icons
                if (config.SmallIcons)
                    e.SpriteBatch.Draw(iconSheet,
                        new Rectangle(barPosX + (((22 * this.config.popupScale) / 2) - ((10 * this.config.popupScale) / 2)),
                            barPosY + (((22 * this.config.popupScale) / 2) - (5 * this.config.popupScale)), 
                            10 * this.config.popupScale, 
                            10 * this.config.popupScale),
                        popupSkill.smallIcon,
                        globalChangeColor);
                else
                    e.SpriteBatch.Draw(iconSheet,
                        new Rectangle(barPosX + (((22 * this.config.popupScale) / 2) - ((16 * this.config.popupScale) / 2)),
                            barPosY + (((22 * this.config.popupScale) / 2) - (8 * this.config.popupScale)),
                            16 * this.config.popupScale,
                            16 * this.config.popupScale),
                        popupSkill.bigIcon,
                        globalChangeColor);

                // Draw level text
                int posNumber = popupSkill.currentLevel <= 10 ? levelPosition[popupSkill.currentLevel] : 33;
                NumberSprite.draw(popupSkill.currentLevel,
                    e.SpriteBatch,
                    new Vector2(barPosX + (posNumber * this.config.popupScale), barPosY + (11 * this.config.popupScale)),
                    globalChangeColor,
                    BarController.AdjustLevelScale(this.config.popupScale, popupSkill.currentLevel, popupSkill.maxLevel),
                    0, 1, 0);

                // Draw Experience Bars
                Color barColor;
                if (popupSkill.currentLevel >= popupSkill.maxLevel) barColor = popupSkill.skillFinalColor;
                else barColor = popupSkill.skillColor;

                e.SpriteBatch.Draw(barFiller,
                    BarController.GetExperienceBar(new Vector2(barPosX + (37 * this.config.popupScale), 
                        barPosY + ((22 * this.config.popupScale) / 2) - ((barFiller.Height * this.config.popupScale) / 2)),
                        new Vector2(58, barFiller.Height), 
                        popupSkill.currentEXP,
                        popupSkill.currentLevel,
                        popupSkill.maxLevel,
                        this.config.popupScale,
                        popupSkill.isMastery),
                    barColor);

                // Draw Experience Text
                if (this.config.ShowExperiencePopupInfo)
                {
                    byte alpha;
                    Color actualColor;
                    if (popupSkill.currentLevel < popupSkill.maxLevel)
                    {
                        actualColor = popupSkill.skillColor;
                        alpha = popupSkill.skillColor.A;
                    }
                    else
                    {
                        actualColor = popupSkill.skillFinalColor;
                        alpha = popupSkill.skillFinalColor.A;
                    }
                    e.SpriteBatch.DrawString(Game1.dialogueFont,
                        BarController.GetExperienceText(popupSkill.currentEXP, popupSkill.currentLevel, popupSkill.maxLevel, popupSkill.isMastery),
                        new Vector2(barPosX + (42 * this.config.popupScale),
                            barPosY + ((expTextOffeset[this.config.popupScale] * this.config.popupScale) / 2) - ((barFiller.Height * this.config.popupScale) / 2)),
                        MyHelper.ChangeColorIntensity(actualColor, 0.45f, alpha), 0f, Vector2.Zero,
                        BarController.AdjustExperienceScale(this.config.popupScale), SpriteEffects.None, 1);
                }
            }


            // In-Config background
            if (inConfigMode)
                e.SpriteBatch.Draw(backgroundConfig, 
                    new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                    new Color(0, 0, 0, 0.50f));

            if (this.config.ShowButtons)
            {
                if (inConfigMode)
                {
                    e.SpriteBatch.Draw(barSheet, 
                        new Rectangle(configButtonPosX, configButtonPosY, buttonConfig.Width * 3, buttonConfig.Height * 3), 
                        buttonConfigApply,
                        Color.White);
                    e.SpriteBatch.Draw(barSheet, 
                        new Rectangle(configButtonPosX + 75, configButtonPosY, buttonReset.Width * 3, buttonReset.Height * 3), 
                        buttonReset,
                        Color.White);
                }
                else
                    e.SpriteBatch.Draw(barSheet,
                        new Rectangle(configButtonPosX, configButtonPosY, buttonConfig.Width * 3, buttonConfig.Height * 3), 
                        buttonConfig,
                        Color.White);
            }
            else
            {
                if (inConfigMode)
                {
                    e.SpriteBatch.Draw(barSheet, 
                        new Rectangle(configButtonPosX, configButtonPosY, buttonConfig.Width * 3, buttonConfig.Height * 3),
                        buttonConfigApply,
                        Color.White);
                    e.SpriteBatch.Draw(barSheet, 
                        new Rectangle(configButtonPosX + 75, configButtonPosY, buttonReset.Width * 3, buttonReset.Height * 3), 
                        buttonReset,
                        Color.White);
                }
            }

            // Draw config button
            if (this.config.ShowButtons)
            {
                if (!inConfigMode)
                {
                    if (!this.config.ShowUI)
                        e.SpriteBatch.Draw(barSheet,
                            new Rectangle(configButtonPosX + 75, configButtonPosY, buttonConfig.Width * 3, buttonConfig.Height * 3), 
                            buttonHidden,
                            Color.White);
                    else
                        e.SpriteBatch.Draw(barSheet, 
                            new Rectangle(configButtonPosX + 75, configButtonPosY, buttonConfig.Width * 3, buttonConfig.Height * 3),
                            buttonVisibility,
                            Color.White);
                }
            }

            if (!this.config.ShowUI) return;

            // Draw adjust buttons
            if (inConfigMode)
            {
                e.SpriteBatch.Draw(barSheet, 
                    new Rectangle(this.config.mainPosX, this.config.mainPosY - 30, buttonDecrease.Width * 3, buttonDecrease.Height * 3),
                    buttonDecrease,
                    decreaseSizeButtonColor);

                e.SpriteBatch.Draw(barSheet, 
                    new Rectangle(this.config.mainPosX + 25, this.config.mainPosY - 30, buttonIncrease.Width * 3, buttonIncrease.Height * 3),
                    buttonIncrease,
                    increaseSizeButtonColor);

                e.SpriteBatch.Draw(barSheet, 
                    new Rectangle(this.config.mainPosX + 75, this.config.mainPosY - 30, buttonBackground.Width * 3, buttonBackground.Height * 3),
                    buttonBackground,
                    backgroundButtonColor);

                e.SpriteBatch.Draw(barSheet, 
                    new Rectangle(this.config.mainPosX + 100, this.config.mainPosY - 30, buttonLevel.Width * 3, buttonLevel.Height * 3),
                    buttonLevel,
                    levelUpButtonColor);

                e.SpriteBatch.Draw(barSheet, 
                    new Rectangle(this.config.mainPosX + 125, this.config.mainPosY - 30, buttonExperience.Width * 3, buttonExperience.Height * 3), 
                    buttonExperience,
                    experienceButtonColor);
            }

            // Draw Background
            if (this.config.ShowBoxBackground)
            {
                e.SpriteBatch.Draw(barSheet, 
                    new Rectangle(this.config.mainPosX, this.config.mainPosY, backgroundTop.Width * this.config.mainScale, backgroundTop.Height * this.config.mainScale),
                    backgroundTop,
                    globalChangeColor);

                e.SpriteBatch.Draw(barSheet, 
                    new Rectangle(this.config.mainPosX, this.config.mainPosY + (backgroundTop.Height * this.config.mainScale), backgroundTop.Width * this.config.mainScale, BarController.AdjustBackgroundSize(playerSkills.Count(), backgroundBar.Height * this.config.mainScale, barSpacement)),
                    backgroundMiddle,
                    globalChangeColor);

                e.SpriteBatch.Draw(barSheet, 
                    new Rectangle(this.config.mainPosX, this.config.mainPosY + (backgroundTop.Height * this.config.mainScale) + BarController.AdjustBackgroundSize(playerSkills.Count(), backgroundBar.Height * this.config.mainScale, barSpacement), backgroundTop.Width * this.config.mainScale, backgroundTop.Height * this.config.mainScale),
                    backgroundBottom,
                    globalChangeColor);
            }

            int posControlY = this.config.mainPosY + (backgroundTop.Height * this.config.mainScale) + (barSpacement / 2);

            // Draw Experience Bars
            foreach (skillHolder sh in playerSkills)
            {
                getPlayerInformation();
                int barPosX = this.config.mainPosX + (((backgroundMiddle.Width / 2) * this.config.mainScale) -  ((backgroundBar.Width / 2) * this.config.mainScale));

                //Draw bars background
                e.SpriteBatch.Draw(barSheet, 
                    new Rectangle(barPosX, posControlY, backgroundBar.Width * this.config.mainScale, backgroundBar.Height * this.config.mainScale),
                    backgroundBar,
                    globalChangeColor);


                // Draw icons
                if (config.SmallIcons)
                    e.SpriteBatch.Draw(iconSheet,
                        new Rectangle(barPosX + (((28 * this.config.mainScale) / 2) - ((16 * this.config.mainScale) / 2)),
                            posControlY + (((22 * this.config.mainScale) / 2) - (5 * this.config.mainScale)),
                            10 * this.config.mainScale,
                            10 * this.config.mainScale),
                        sh.smallIcon,
                        globalChangeColor);
                else
                    e.SpriteBatch.Draw(iconSheet,
                        new Rectangle(barPosX + (((22 * this.config.mainScale) / 2) - ((16 * this.config.mainScale) / 2)),
                            posControlY + (((22 * this.config.mainScale) / 2) - (8 * this.config.mainScale)), 
                            16 * this.config.mainScale,
                            16 * this.config.mainScale),
                        sh.bigIcon,
                        globalChangeColor);

                // Draw level text
                int posNumber = sh.currentLevel <= 10 ? levelPosition[sh.currentLevel] : 33;
                NumberSprite.draw(sh.currentLevel,
                    e.SpriteBatch,
                    new Vector2(barPosX + (posNumber * this.config.mainScale), posControlY + (11 * this.config.mainScale)),
                    globalChangeColor,
                    BarController.AdjustLevelScale(this.config.mainScale, sh.currentLevel, sh.maxLevel),
                    0, 1, 0);

                // Draw Experience Bars
                Color barColor;
                if (draggingBox)
                    if (sh.currentLevel >= sh.maxLevel) barColor = MyHelper.ChangeColorIntensity(sh.skillFinalColor, 0.35f, 1);
                    else barColor = MyHelper.ChangeColorIntensity(sh.skillColor, 0.35f, 1);
                else
                    if (sh.currentLevel >= sh.maxLevel) barColor = sh.skillFinalColor;
                    else barColor = sh.skillColor;

                e.SpriteBatch.Draw(barFiller,
                    BarController.GetExperienceBar(new Vector2(barPosX + (37 * this.config.mainScale),
                        posControlY + ((22 * this.config.mainScale) / 2) - ((barFiller.Height * this.config.mainScale) / 2)),
                        new Vector2(58, barFiller.Height), 
                        sh.currentEXP,
                        sh.currentLevel,
                        sh.maxLevel,
                        this.config.mainScale,
                        sh.isMastery),
                    barColor);
                    
                // Draw Experience Text
                if (this.config.ShowExperienceInfo)
                {
                    byte alpha;
                    Color actualColor;
                    if (sh.currentLevel < sh.maxLevel)
                    {
                        actualColor = sh.skillColor;
                        alpha = sh.skillColor.A;
                    }
                    else
                    {
                        actualColor = sh.skillFinalColor;
                        alpha = sh.skillFinalColor.A;
                    }

                    e.SpriteBatch.DrawString(Game1.dialogueFont,
                        BarController.GetExperienceText(sh.currentEXP, sh.currentLevel, sh.maxLevel, sh.isMastery),
                        new Vector2(barPosX + (42 * this.config.mainScale),
                        posControlY + ((expTextOffeset[this.config.mainScale] * this.config.mainScale) / 2) - ((barFiller.Height * this.config.mainScale) / 2)),
                        MyHelper.ChangeColorIntensity(actualColor, 0.45f, alpha), 0f, Vector2.Zero, 
                        BarController.AdjustExperienceScale(this.config.mainScale), SpriteEffects.None, 1);
                }

                if (sh.actualExpGainedMessage && this.config.ShowExperienceInfo)
                {
                    e.SpriteBatch.Draw(barSheet,
                        new Rectangle(barPosX + expAdvicePositionX, 
                            posControlY + ((backgroundBar.Height * this.config.mainScale) / 2) - ((backgroundExp.Height * this.config.mainScale) / 2), 
                            backgroundExp.Width * this.config.mainScale, 
                            backgroundExp.Height * this.config.mainScale),
                        backgroundExp,
                        MyHelper.ChangeColorIntensity(globalChangeColor, 1, sh.expAlpha));

                    Vector2 centralizedStringPos = MyHelper.GetStringCenter(sh.expGained.ToString(), Game1.dialogueFont);
                    e.SpriteBatch.DrawString(Game1.dialogueFont,
                        $"+{sh.expGained}", 
                        new Vector2(barPosX + expAdvicePositionX + ((backgroundExp.Width * this.config.mainScale) / 2) - centralizedStringPos.X, posControlY + ((23 * this.config.mainScale) / 2) - ((barFiller.Height * this.config.mainScale) / 2)), 
                        MyHelper.ChangeColorIntensity(sh.skillRestorationColor, 0.45f, sh.expAlpha), 
                        0f, 
                        Vector2.Zero, 
                        BarController.AdjustExperienceScale(this.config.mainScale), 
                        SpriteEffects.None, 1);
                }

                posControlY += barSpacement + (backgroundBar.Height * this.config.mainScale);
            }
        }

        private void onSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Reset player skills   
            playerSkills = new List<skillHolder>();

            // Set Load Flag for later data
            loadedSaveFlag = false;
            
            // Adjust width for Mine level    
            configButtonPosX = MyHelper.AdjustPositionMineLevelWidth(configButtonPosX, Game1.player.currentLocation, defaultButtonPosX);
        }

        private void onUpdate(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady && Game1.CurrentEvent == null) return;

            // Get New Player Info
            getPlayerInformation();

            // Animate experience or level gains
            animateThings();

            // Experience Gain Check
            checkExperienceGain();
            expTimer();
            expAlphaChanger();

            // Experience Popup Timer
            expPopupTimer();

            // Level Up Check
            checkLevelGain();
            levelUpTimer();

            // Player is dragging box
            if (draggingBox)
            {
                this.config.mainPosX = Game1.getMousePosition(true).X - ((backgroundTop.Width * this.config.mainScale) / 2);
                this.config.mainPosY = Game1.getMousePosition(true).Y - (BarController.AdjustBackgroundSize(playerSkills.Count(), backgroundBar.Height * this.config.mainScale, barSpacement) / 2) - (backgroundTop.Height * this.config.mainScale);
            }

            // Check position
            checkMousePosition();
            repositionExpInfo();

            // Special Case for the Club
            if (Game1.currentLocation.Name == "Club")
                configButtonPosY = 90;
            else
                configButtonPosY = defaultButtonPosY;
        }

        private void checkExperienceGain()
        {
            foreach (skillHolder sh in playerSkills)
            {
                sh.GainExperience();

                // Show popup window
                if (sh.expIncreasing && this.config.ShowExpPopup) 
                {
                    popupSkill = sh;
                    timeLeftPopup = this.config.PopupMessageDuration * 60;
                    canCountPopupTimer = true;

                }

            }
        }

        private void expTimer()
        {
            if (playerSkills.Any(sh => sh.animateSkill == true)) {
                List<skillHolder> animateList = playerSkills.FindAll(sh => sh.animateSkill == true);

                foreach (skillHolder sh in animateList)
                {
                    // Skill Info
                    int actualSkillLevel = sh.currentLevel;

                    // Color Values
                    int virtualColorValue;
                    byte intensity = 5;

                    if (sh.expIncreasing)
                    {
                        if (actualSkillLevel < sh.maxLevel)
                        {
                            if (sh.skillColor.R < 255 && sh.skillColor.G < 255 && sh.skillColor.B < 255)
                            {
                                virtualColorValue = sh.skillColor.R + intensity;
                                if (virtualColorValue < 255) sh.skillColor.R += intensity;
                                else sh.skillColor.R = 255;

                                virtualColorValue = sh.skillColor.G + intensity;
                                if (virtualColorValue < 255) sh.skillColor.G += intensity;
                                else sh.skillColor.G = 255;

                                virtualColorValue = sh.skillColor.B + intensity;
                                if (virtualColorValue < 255) sh.skillColor.B += intensity;
                                else sh.skillColor.B = 255;
                            }
                            else
                                sh.expIncreasing = false;
                        }
                        else
                        {
                            if (sh.skillFinalColor.R < 255 && sh.skillFinalColor.G < 255 && sh.skillFinalColor.B < 255)
                            {
                                virtualColorValue = sh.skillFinalColor.R + intensity;
                                if (virtualColorValue < 255) sh.skillFinalColor.R += intensity;
                                else sh.skillFinalColor.R = 255;

                                virtualColorValue = sh.skillFinalColor.G + intensity;
                                if (virtualColorValue < 255) sh.skillFinalColor.G += intensity;
                                else sh.skillFinalColor.G = 255;

                                virtualColorValue = sh.skillFinalColor.B + intensity;
                                if (virtualColorValue < 255) sh.skillFinalColor.B += intensity;
                                else sh.skillFinalColor.B = 255;
                            }
                            else
                                sh.expIncreasing = false;
                        }
                    }
                    else
                    {
                        if (actualSkillLevel < sh.maxLevel)
                        {
                            if (sh.skillColor != sh.skillRestorationColor)
                            {
                                virtualColorValue = sh.skillColor.R - intensity;
                                if (virtualColorValue > sh.skillRestorationColor.R) sh.skillColor.R -= intensity;
                                else sh.skillColor.R = sh.skillRestorationColor.R;

                                virtualColorValue = sh.skillColor.G - intensity;
                                if (virtualColorValue > sh.skillRestorationColor.G) sh.skillColor.G -= intensity;
                                else sh.skillColor.G = sh.skillRestorationColor.G;

                                virtualColorValue = sh.skillColor.B - intensity;
                                if (virtualColorValue > sh.skillRestorationColor.B) sh.skillColor.B -= intensity;
                                else sh.skillColor.B = sh.skillRestorationColor.B;
                            }
                            else
                                sh.animateSkill = false;
                        }
                        else
                        {
                            if (sh.skillFinalColor != sh.skillGoldColor)
                            {
                                virtualColorValue = sh.skillFinalColor.R - intensity;
                                if (virtualColorValue > sh.skillGoldColor.R) sh.skillFinalColor.R -= intensity;
                                else sh.skillFinalColor.R = sh.skillGoldColor.R;

                                virtualColorValue = sh.skillFinalColor.G - intensity;
                                if (virtualColorValue > sh.skillGoldColor.G) sh.skillFinalColor.G -= intensity;
                                else sh.skillFinalColor.G = sh.skillGoldColor.G;

                                virtualColorValue = sh.skillFinalColor.B - intensity;
                                if (virtualColorValue > sh.skillGoldColor.B) sh.skillFinalColor.B -= intensity;
                                else sh.skillFinalColor.B = sh.skillGoldColor.B;
                            }
                            else
                                sh.animateSkill = false;
                        }
                    }
                }
            }
        }

        private void expAlphaChanger()
        {
            if (playerSkills.Any(sh => sh.actualExpGainedMessage == true))
            {
                List<skillHolder> expGainedList = playerSkills.FindAll(sh => sh.actualExpGainedMessage == true);
                foreach (skillHolder sh in expGainedList)
                    sh.ExperienceAlpha(12);
            }
        }

        private void expPopupTimer()
        {
            if (!canCountPopupTimer) return;
            if (timeLeftPopup > 0) timeLeftPopup--;
            else
            {
                canCountPopupTimer = false;
                popupSkill = null;
                foreach (skillHolder sh in playerSkills)
                    sh.expPopup = false;
            }
        }

        private void checkLevelGain()
        {
            foreach (skillHolder sh in playerSkills)
            {
                if (sh.GainLevel() && !sh.isMastery)
                {
                    // Show level up information
                    if (this.config.SmallIcons)
                        levelUpSource = sh.smallIcon;
                    else
                        levelUpSource = sh.bigIcon;
                    levelUpMessage = Helper.Translation.Get("LevelUpMessage");
                    timeLeft = this.config.LevelUpMessageDuration * 60;
                    canShowLevelUp = true;

                    // Set Level up window and bool
                    levelUpPosY = (int)new Vector2(0, 0 - (backgroundLevelUp.Height * 3) - 5).Y;
                    animLevelUpDir = "bottom";
                    animDestPosLevelUp = new Vector2(0, 150);
                    animatingLevelUp = true;

                    // Play Level Up Sound
                    if (this.config.LevelUpSound)
                        Game1.playSound(levelUpID);
                }
            }
        }

        private void levelUpTimer()
        {
            if (!canShowLevelUp || !canCountTimer) return;

            if (timeLeft > 0)
                timeLeft--;
            else
            {
                canCountTimer = false;
                animLevelUpDir = "top";
                animDestPosLevelUp = new Vector2(0, 0 - (backgroundLevelUp.Height * 3) - 5);
                animatingLevelUp = true;
            }
        }

        private void getPlayerInformation()
        {
            // Load skills or just set current data
            if (playerSkills.Count <= 0)
                loadSkills();
            else
                foreach (skillHolder sh in playerSkills)
                    sh.setSkillData(true);

            // Load prior data if we just loaded a save - SpaceCore skill data is no available immedietly on load
            if (!loadedSaveFlag)
            {
                foreach (skillHolder sh in playerSkills)
                    sh.setSkillData(false);

                loadedSaveFlag = true;
            }

            // Check for player mastery
            if (!masteryProcessed && playerSkills.Where(x => (masterySkills.Contains(x.skillID)) && x.currentLevel >= 10).Count() == 5)
            {
                playerSkills.RemoveAll(s => masterySkills.Contains(s.skillID));
                playerSkills.Insert(0, new skillHolder(Helper, "mastery", 15, new Color(39, 185, 101)));
                masteryProcessed = true;
            }
        }

        private void onButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.CurrentEvent != null) return;

            Vector2 mousePos;
            mousePos.X = Game1.getMousePosition(true).X;
            mousePos.Y = Game1.getMousePosition(true).Y;

            //Reset button check click
            if (e.Button == SButton.MouseLeft && buttonCheck(mousePos, 4, 2, configButtonPosX, configButtonPosY, buttonReset.Width, buttonReset.Height, true))
                configReset();

            // Reset Box Position - Button
            if (e.Button == this.config.ResetKey && inConfigMode)
                configReset();

            // Config button click check - Button
            if (e.Button == this.config.ConfigKey)
            {
                if (!this.config.ShowUI)
                    toggleUI();

                switch (inConfigMode)
                {
                    case true:
                        inConfigMode = false;
                        break;
                    case false:
                        inConfigMode = true;
                        break;
                }
            }
            //Toggle UI - Button
            if (e.Button == this.config.ToggleKey)
            {
                if (inConfigMode) return;
                toggleUI();
            }

            //Config button click check
            //Toggle UI
            if (this.config.ShowButtons)
            {
                if (e.Button == SButton.MouseLeft || e.Button == this.config.ConfigKey)
                {
                    if (mousePos.X >= configButtonPosX && mousePos.X <= configButtonPosX + (buttonConfig.Width * 3) && mousePos.Y >= configButtonPosY && mousePos.Y <= configButtonPosY + (buttonConfig.Height * 3))
                    {
                        if (!this.config.ShowUI)
                            toggleUI();

                        switch (inConfigMode)
                        {
                            case true:
                                inConfigMode = false;
                                break;
                            case false:
                                inConfigMode = true;
                                break;
                        }
                    }
                }


                if (e.Button == SButton.MouseLeft)
                {
                    if (mousePos.X >= configButtonPosX + 75 && mousePos.X <= configButtonPosX + 75 + (buttonConfig.Width * 3) &&
                        mousePos.Y >= configButtonPosY && mousePos.Y <= configButtonPosY + (buttonConfig.Height * 3))
                    {
                        if (inConfigMode) return;
                        toggleUI();
                    }
                }
            }
            else
            {
                if (inConfigMode)
                {
                    if (e.Button == SButton.MouseLeft || e.Button == this.config.ConfigKey)
                    {
                        if (mousePos.X >= configButtonPosX && mousePos.X <= configButtonPosX + (buttonConfig.Width * 3) &&
                            mousePos.Y >= configButtonPosY && mousePos.Y <= configButtonPosY + (buttonConfig.Height * 3))
                        {
                            if (!this.config.ShowUI)
                                toggleUI();

                            switch (inConfigMode)
                            {
                                case true:
                                    inConfigMode = false;
                                    break;
                                case false:
                                    inConfigMode = true;
                                    break;
                            }
                        }
                    }
                }
            }

            if (inConfigMode)
            {
                //Box click check
                int totalBackgroundSize = BarController.AdjustBackgroundSize(playerSkills.Count(), backgroundBar.Height * this.config.mainScale, barSpacement) + (backgroundTop.Height * this.config.mainScale) + (backgroundBottom.Height * this.config.mainScale);
                if (e.Button == SButton.MouseLeft &&
                    mousePos.X >= this.config.mainPosX && mousePos.X <= this.config.mainPosX + (backgroundTop.Width * this.config.mainScale) &&
                    mousePos.Y >= this.config.mainPosY && mousePos.Y <= this.config.mainPosY + totalBackgroundSize)
                {
                    draggingBox = true;
                    globalChangeColor = Color.DarkGray;
                }

                //Decrease button click check
                if (e.Button == SButton.MouseLeft &&
                    mousePos.X >= this.config.mainPosX && mousePos.X <= this.config.mainPosX + (buttonDecrease.Width * 3) &&
                    mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecrease.Height * 3))
                {
                    if (this.config.mainScale > 1)
                    {
                        decreaseSizeButtonColor = Color.White;
                        increaseSizeButtonColor = Color.White;
                        this.config.mainScale -= 1;
                        if (this.config.mainScale == 1)
                            decreaseSizeButtonColor = MyHelper.ChangeColorIntensity(Color.DarkGray, 1, 0.7f);
                        configSave();
                    }
                }
                //Increase button click check
                if (e.Button == SButton.MouseLeft &&
                    mousePos.X >= this.config.mainPosX + 25 && mousePos.X <= (this.config.mainPosX + 25) + (buttonDecrease.Width * 3) &&
                    mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecrease.Height * 3))
                {
                    if (this.config.mainScale < 5)
                    {
                        increaseSizeButtonColor = Color.White;
                        decreaseSizeButtonColor = Color.White;
                        this.config.mainScale += 1;
                        if (this.config.mainScale == 5)
                            increaseSizeButtonColor = MyHelper.ChangeColorIntensity(Color.DarkGray, 1, 0.7f);
                        configSave();
                    }
                }

                //Background toggler button check click
                if (e.Button == SButton.MouseLeft &&
                    mousePos.X >= this.config.mainPosX + 75 && mousePos.X <= (this.config.mainPosX + 75) + (buttonDecrease.Width * 3) &&
                    mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecrease.Height * 3))
                {
                    switch (this.config.ShowBoxBackground)
                    {
                        case true:
                            this.config.ShowBoxBackground = false;
                            backgroundButtonColor = MyHelper.ChangeColorIntensity(Color.DarkGray, 1, 0.7f);
                            break;
                        case false:
                            this.config.ShowBoxBackground = true;
                            backgroundButtonColor = Color.White;
                            break;
                    }
                    configSave();
                }

                // Levelup toggler button check click
                if (e.Button == SButton.MouseLeft &&
                    mousePos.X >= this.config.mainPosX + 100 && mousePos.X <= (this.config.mainPosX + 100) + (buttonDecrease.Width * 3) &&
                    mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecrease.Height * 3))
                {
                    switch (this.config.ShowLevelUp)
                    {
                        case true:
                            this.config.ShowLevelUp = false;
                            levelUpButtonColor = MyHelper.ChangeColorIntensity(Color.DarkGray, 1, 0.7f);
                            Game1.addHUDMessage(new HUDMessage("Level up popup DISABLED", 3));
                            break;
                        case false:
                            this.config.ShowLevelUp = true;
                            levelUpButtonColor = Color.White;
                            Game1.addHUDMessage(new HUDMessage("Level up popup ENABLED", 3));
                            break;
                    }
                    configSave();
                }

                // Experience toggler button check click
                if (e.Button == SButton.MouseLeft &&
                    mousePos.X >= this.config.mainPosX + 125 && mousePos.X <= (this.config.mainPosX + 125) + (buttonDecrease.Width * 3) &&
                    mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecrease.Height * 3))
                {
                    switch (this.config.ShowExperienceInfo)
                    {
                        case true:
                            this.config.ShowExperienceInfo = false;
                            experienceButtonColor = MyHelper.ChangeColorIntensity(Color.DarkGray, 1, 0.7f);
                            break;
                        case false:
                            this.config.ShowExperienceInfo = true;
                            experienceButtonColor = Color.White;
                            break;
                    }
                    configSave();
                }

            }
        }

        private void onButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.CurrentEvent != null) return;

            if (inConfigMode)
            {
                //Box release check
                if (e.Button == SButton.MouseLeft && draggingBox)
                {
                    draggingBox = false;
                    globalChangeColor = Color.White;
                    configSave();
                }
            }
        }

        private void toggleUI()
        {
            switch (this.config.ShowUI)
            {
                case true:
                    this.config.ShowUI = false;
                    break;

                case false:
                    this.config.ShowUI = true;
                    break;
            }
            configSave();
        }

        private void animateThings()
        {
            Vector2 levelUpPosHolder = new Vector2(0, levelUpPosY);

            if (animatingLevelUp)
            {
                if (levelUpPosHolder != animDestPosLevelUp)
                {
                    levelUpPosHolder = AnimController.Animate(levelUpPosHolder, animDestPosLevelUp, 8f, animLevelUpDir);
                    levelUpPosY = (int)levelUpPosHolder.Y;
                }
                else
                {
                    animatingLevelUp = false;
                    canCountTimer = true;
                }
            }
        }

        private void checkMousePosition()
        {
            if (!Context.IsWorldReady && Game1.CurrentEvent == null) return;

            Vector2 mousePos;
            mousePos.X = Game1.getMousePosition(true).X;
            mousePos.Y = Game1.getMousePosition(true).Y;
            int totalBackgroundSize = BarController.AdjustBackgroundSize(playerSkills.Count(), backgroundBar.Height * this.config.mainScale, barSpacement) + (backgroundTop.Height * this.config.mainScale) + (backgroundBottom.Height * this.config.mainScale);

            // Reset Button
            if (buttonCheck(mousePos, 3, 0, configButtonPosX, configButtonPosY, buttonReset.Width, buttonReset.Height, true))
                blockActions();

            // Config Button
            else if (buttonCheck(mousePos, 0, 0, configButtonPosX, configButtonPosY, buttonConfig.Width, buttonConfig.Height))
                blockActions();

            // Toggle UI
            else if (buttonCheck(mousePos, 3, 0, configButtonPosX, configButtonPosY, buttonConfig.Width, buttonConfig.Height))
                blockActions();

            // Background Select
            else if (buttonCheck(mousePos, 0, 0, this.config.mainPosX, this.config.mainPosY, backgroundTop.Width, totalBackgroundSize, true, true))
                blockActions();

            // Decrease Scale Button
            else if (buttonCheck(mousePos, 0, 1, this.config.mainPosX, this.config.mainPosY, buttonDecrease.Width, buttonDecrease.Height, true))
                blockActions();

            // Increase Scale Button
            else if (buttonCheck(mousePos, 1, 1, this.config.mainPosX, this.config.mainPosY, buttonDecrease.Width, buttonDecrease.Height, true))
                blockActions();

            // Background Toggle
            else if (buttonCheck(mousePos, 2, 1, this.config.mainPosX, this.config.mainPosY, buttonDecrease.Width, buttonDecrease.Height, true))
                    blockActions();

            // Level Up Toggle
            else if (buttonCheck(mousePos, 3, 1, this.config.mainPosX, this.config.mainPosY, buttonDecrease.Width, buttonDecrease.Height, true))
                blockActions();

            // Experience Toggle
            else if (buttonCheck(mousePos, 4, 1, this.config.mainPosX, this.config.mainPosY, buttonDecrease.Width, buttonDecrease.Height, true))
                blockActions();

            // Unblock
            else
                unblockActions();
        }

        private bool buttonCheck(Vector2 mousePos, int xIndex, int yIndex, int xPos, int yPos, int xWidth, int yHeight, bool configCheck = false, bool backgroundCheck = false)
        {
            // If checking background
            if (backgroundCheck)
                if (mousePos.X >= xPos && mousePos.X <= xPos + (xWidth * this.config.mainScale) &&
                    mousePos.Y >= yPos && mousePos.Y <= yPos + yHeight &&
                    (configCheck && inConfigMode || !configCheck))
                    return true;
                else
                    return false;

            // Checking Buttons
            if (mousePos.X >= xPos + mainX[xIndex] && mousePos.X <= xPos + mainX[xIndex] + (xWidth * 3) &&
                mousePos.Y >= yPos + mainY[yIndex] && mousePos.Y <= yPos + mainY[yIndex] + (yHeight * 3) &&
                (configCheck && inConfigMode || !configCheck))
                return true;
            else
                return false;
        }

        private void blockActions()
        {
            Game1.player.canOnlyWalk = true;
        }

        private void unblockActions()
        {
            Game1.player.canOnlyWalk = false;
        }
    }
}
