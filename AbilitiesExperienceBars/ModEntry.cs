using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AbilitiesExperienceBars
{
    public class skillHolder
    {
        private string skillID;

        public Texture2D skillIcon;

        public Color skillColor;
        public Color skillRestorationColor;
        public Color skillFinalColor = new Color(150, 175, 55);
        public Color skillGoldColor = new Color(150, 175, 55);

        public int iconIndex;
        public int colorIndex;

        public int currentEXP;
        public int previousEXP;
        public int currentLevel;
        public int previousLevel;

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

        public skillHolder(IModHelper Helper, string ID, string iconFilename, Color skillColorCode)
        {
            // Set the skill ID
            skillID = ID;

            // Load Skill Icon
            skillIcon = Helper.ModContent.Load<Texture2D>($"assets/ui/icons/{iconFilename}.png");

            // Load Colors
            skillColor = skillColorCode;
            skillRestorationColor = skillColorCode;

            // Set Current Data
            setCurrentData();
        }

        public void setPreviousData()
        {
            // Stardew Base Skills
            if (skillID == "farming")
            {
                previousLevel = Game1.player.farmingLevel.Value;
                previousEXP = Game1.player.experiencePoints[0];
            }
            else if (skillID == "fishing")
            {
                previousLevel = Game1.player.fishingLevel.Value;
                previousEXP = Game1.player.experiencePoints[1];
            }
            else if (skillID == "foraging")
            {
                previousLevel = Game1.player.foragingLevel.Value;
                previousEXP = Game1.player.experiencePoints[2];
            }
            else if (skillID == "mining")
            {
                previousLevel = Game1.player.miningLevel.Value;
                previousEXP = Game1.player.experiencePoints[3];
            }
            else if (skillID == "combat")
            {
                previousLevel = Game1.player.combatLevel.Value;
                previousEXP = Game1.player.experiencePoints[4];
            }
            else if (skillID == "luck")
            {
                previousLevel = Game1.player.luckLevel.Value;
                previousEXP = Game1.player.experiencePoints[5];
            }

            // Mod Added Skills
            else
            {
                previousLevel = Game1.player.GetCustomSkillLevel(Skills.GetSkill(skillID));
                previousEXP = Game1.player.GetCustomSkillExperience(Skills.GetSkill(skillID));
            }
        }

        public void setCurrentData()
        {
            // Stardew Base Skills
            if (skillID == "farming")
            {
                currentLevel = Game1.player.farmingLevel.Value;
                currentEXP = Game1.player.experiencePoints[0];
            }
            else if (skillID == "fishing")
            {
                currentLevel = Game1.player.fishingLevel.Value;
                currentEXP = Game1.player.experiencePoints[1];
            }
            else if (skillID == "foraging")
            {
                currentLevel = Game1.player.foragingLevel.Value;
                currentEXP = Game1.player.experiencePoints[2];
            }
            else if (skillID == "mining")
            {
                currentLevel = Game1.player.miningLevel.Value;
                currentEXP = Game1.player.experiencePoints[3];
            }
            else if (skillID == "combat")
            {
                currentLevel = Game1.player.combatLevel.Value;
                currentEXP = Game1.player.experiencePoints[4];
            }
            else if (skillID == "luck")
            {
                currentLevel = Game1.player.luckLevel.Value;
                currentEXP = Game1.player.experiencePoints[5];
            }

            // Mod Added Skills
            else
            {
                currentLevel = Game1.player.GetCustomSkillLevel(Skills.GetSkill(skillID));
                currentEXP = Game1.player.GetCustomSkillExperience(Skills.GetSkill(skillID));
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
        private int maxLevel = 10;
        private int levelUpPosY;
        private bool inConfigMode;
        private int expAdvicePositionX;
        private SoundEffectInstance levelUp;

        // Sprite Variables
        private Texture2D backgroundTop,
            backgroundBottom,
            backgroundFiller,
            backgroundBar,
            backgroundPopupBar,
            backgroundExp,
            barFiller,
            backgroundBoxConfig,
            backgroundLevelUp,
            buttonConfig,
            buttonDecreaseSize,
            buttonIncreaseSize,
            backgroundButton,
            levelUpButton,
            experienceButton,
            buttonConfigApply,
            buttonVisibility,
            buttonHidden,
            buttonReset;

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

        private bool canShowPopupExp;
        private bool canCountPopupTimer;

        // Data Variables
        public ModEntry instance;
        private ModConfig config;

        // Timer Variables
        private float timeLeft;
        private float timeLeftPopup;

        // Level Up Variables
        Texture2D levelUpIcon;
        string levelUpMessage;

        // Experience Popup
        skillHolder popupSkill;
        bool sampleRunOnce;

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
            DirectoryInfo directoryInfo = new DirectoryInfo(rootDir);
            var dirs = new List<string>();
            foreach (var dir in directoryInfo.GetDirectories())
                dirs.Add(dir.Name.Replace($"{rootDir}\\", ""));

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
                    allowedValues: dirs.ToArray(),
                    getValue: () => config.UITheme,
                    setValue: value => config.UITheme = value
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
                    max: 6,
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
                    max: 6,
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
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("LevelUpMessageDuration"),
                    tooltip: () => Helper.Translation.Get("LevelUpMessageDurationT"),
                    getValue: () => config.LevelUpMessageDuration,
                    setValue: value => config.LevelUpMessageDuration = value
                );
            }
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
            // Adjust Decrease Size Button color and value
            if (this.config.mainScale < 1 || this.config.mainScale == 1)
            {
                this.config.mainScale = 1;
                decreaseSizeButtonColor = MyHelper.ChangeColorIntensity(Color.DarkGray, 1, 0.7f);
            }
            else
                decreaseSizeButtonColor = Color.White;

            // Adjust Increase Size Button color and value
            if (this.config.mainScale > 5 || this.config.mainScale == 5)
            {
                this.config.mainScale = 5;
                increaseSizeButtonColor = MyHelper.ChangeColorIntensity(Color.DarkGray, 1, 0.7f);
            }
            else
                increaseSizeButtonColor = Color.White;

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

            // Level up message duration
            if (this.config.LevelUpMessageDuration < 1)
                this.config.LevelUpMessageDuration = 1;

            // Default UI Theme
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
            this.config.LevelUpMessageDuration = 4;

            // Save Info
            configSave();
            configAdjust();
        }

        private void loadSound()
        {
            var path = Path.Combine(Helper.DirectoryPath, "assets", "sound", "LevelUp.wav");
            levelUp = SoundEffect.FromStream(new FileStream(path, FileMode.Open)).CreateInstance();
        }

        private void loadTextures()
        {
            // Primary Path
            string uiPath;
            if (config.UITheme == null)
                uiPath = $"assets/ui/themes/Vanilla/";
            else
                uiPath = $"assets/ui/themes/{config.UITheme}/";

            // Experience Bar Background
            backgroundTop = Helper.ModContent.Load<Texture2D>($"{uiPath}backgroundTop.png");
            backgroundBottom = Helper.ModContent.Load<Texture2D>($"{uiPath}backgroundBottom.png");
            backgroundFiller = Helper.ModContent.Load<Texture2D>($"{uiPath}backgroundFiller.png");

            // Experience Bar
            backgroundPopupBar = Helper.ModContent.Load<Texture2D>($"{uiPath}backgroundPopupBar.png");
            backgroundBar = Helper.ModContent.Load<Texture2D>($"{uiPath}backgroundBar.png");
            
            backgroundExp = Helper.ModContent.Load<Texture2D>($"{uiPath}expHolder.png");
            

            // Configuration Buttons
            buttonConfig = Helper.ModContent.Load<Texture2D>($"{uiPath}iconBoxConfig.png");
            buttonConfigApply = Helper.ModContent.Load<Texture2D>($"{uiPath}checkIcon.png");
            buttonDecreaseSize = Helper.ModContent.Load<Texture2D>($"{uiPath}decreaseSize.png");
            buttonIncreaseSize = Helper.ModContent.Load<Texture2D>($"{uiPath}increaseSize.png");
            backgroundButton = Helper.ModContent.Load<Texture2D>($"{uiPath}backgroundButton.png");
            levelUpButton = Helper.ModContent.Load<Texture2D>($"{uiPath}levelUpButton.png");
            experienceButton = Helper.ModContent.Load<Texture2D>($"{uiPath}experienceButton.png");
            buttonVisibility = Helper.ModContent.Load<Texture2D>($"{uiPath}visibleIcon.png");
            buttonHidden = Helper.ModContent.Load<Texture2D>($"{uiPath}hiddenIcon.png");
            buttonReset = Helper.ModContent.Load<Texture2D>($"{uiPath}resetButton.png");

            // Level Up
            backgroundLevelUp = Helper.ModContent.Load<Texture2D>($"{uiPath}backgroundLevelUp.png");


            // Non-Theme Textures
            backgroundBoxConfig = Helper.ModContent.Load<Texture2D>("assets/ui/backgroundBoxConfig.png");
            barFiller = Helper.ModContent.Load<Texture2D>("assets/ui/barFiller.png");
        }

        private void loadSkills()
        {
            // Add Base Skills
            playerSkills.Add(new skillHolder(Helper, "farming", "farming", new Color(115, 150, 56)));
            playerSkills.Add(new skillHolder(Helper, "fishing", "fishing", new Color(117, 150, 150)));
            playerSkills.Add(new skillHolder(Helper, "foraging", "foraging", new Color(145, 102, 0)));
            playerSkills.Add(new skillHolder(Helper, "mining", "mining", new Color(150, 80, 120)));
            playerSkills.Add(new skillHolder(Helper, "combat", "combat", new Color(150, 31, 0)));

            // Mod Compatibility
            if (this.Helper.ModRegistry.IsLoaded("spacechase0.LuckSkill"))
                playerSkills.Add(new skillHolder(Helper, "luck", "luck", new Color(150, 150, 0)));
            if (this.Helper.ModRegistry.IsLoaded("spacechase0.CookingSkill"))
                playerSkills.Add(new skillHolder(Helper, "cooking", "cooking", new Color(165, 100, 30)));
            if (this.Helper.ModRegistry.IsLoaded("moonslime.CookingSkill"))
                playerSkills.Add(new skillHolder(Helper, "moonslime.Cooking", "cooking", new Color(165, 100, 30)));
            if (this.Helper.ModRegistry.IsLoaded("blueberry.LoveOfCooking"))
                playerSkills.Add(new skillHolder(Helper, "blueberry.LoveOfCooking.CookingSkill", "loveCooking", new Color(150, 55, 5)));
            if (this.Helper.ModRegistry.IsLoaded("moonslime.ArchaeologySkill"))
                playerSkills.Add(new skillHolder(Helper, "moonslime.Archaeology", "archaeology", new Color(63, 24, 0)));
            if (this.Helper.ModRegistry.IsLoaded("drbirbdev.SocializingSkill"))
                playerSkills.Add(new skillHolder(Helper, "drbirbdev.Socializing", "socializing", new Color(221, 0, 59)));
            if (this.Helper.ModRegistry.IsLoaded("Achtuur.StardewTravelSkill"))
                playerSkills.Add(new skillHolder(Helper, "Achtuur.Travelling", "travelling", new Color(73, 100, 98)));
            if (this.Helper.ModRegistry.IsLoaded("drbirbdev.BinningSkill"))
                playerSkills.Add(new skillHolder(Helper, "drbirbdev.Binning", "binning", new Color(60, 60, 77)));
            if (this.Helper.ModRegistry.IsLoaded("spacechase0.Magic"))
                playerSkills.Add(new skillHolder(Helper, "magic", "magic", new Color(155, 25, 135)));
        }

        private void onRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // Show Level up Window
            if (canShowLevelUp && this.config.ShowLevelUp)
            {
                e.SpriteBatch.Draw(backgroundLevelUp, new Rectangle((Game1.uiViewport.Width / 2) - (backgroundLevelUp.Width * 3) / 2, levelUpPosY, backgroundLevelUp.Width * 3, backgroundLevelUp.Height * 3), Color.White);
                e.SpriteBatch.Draw(levelUpIcon, new Rectangle(((Game1.uiViewport.Width / 2) - (levelUpIcon.Width * 3) / 2) - 2, levelUpPosY + 16, levelUpIcon.Width * 3, levelUpIcon.Height * 3), Color.White);

                Vector2 centralizedStringPos = MyHelper.GetStringCenter(levelUpMessage, Game1.dialogueFont);
                e.SpriteBatch.DrawString(Game1.dialogueFont, levelUpMessage, new Vector2((Game1.uiViewport.Width / 2) - centralizedStringPos.X + 5, MyHelper.AdjustLanguagePosition(levelUpPosY + 63, Helper.Translation.LocaleEnum.ToString())), Color.Black);
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
            if (canCountPopupTimer && popupSkill != null && this.config.ShowExpPopup)
            {
                // Dont draw bar if experience is maxed
                if (popupSkill.currentLevel >= 10) return;

                // Bar position
                int barPosX = this.config.popupPosX;
                int barPosY = this.config.popupPosY;

                // Draw bars background
                e.SpriteBatch.Draw(backgroundPopupBar,
                    new Rectangle(barPosX, barPosY, backgroundPopupBar.Width * this.config.popupScale, backgroundPopupBar.Height * this.config.popupScale),
                    globalChangeColor);


                // Draw icons
                e.SpriteBatch.Draw(popupSkill.skillIcon, new Rectangle(barPosX + (((26 * this.config.popupScale) / 2) - ((popupSkill.skillIcon.Width * this.config.popupScale) / 2)),
                    barPosY + (((24 * this.config.popupScale) / 2) - (int)MyHelper.GetSpriteCenter(popupSkill.skillIcon,
                    this.config.popupScale).Y), popupSkill.skillIcon.Width * this.config.popupScale, popupSkill.skillIcon.Height * this.config.popupScale),
                    globalChangeColor);

                // Draw level text
                int posNumber;
                if (popupSkill.currentLevel < 10) posNumber = 34;
                else if (popupSkill.currentLevel < 100) posNumber = 37;
                else posNumber = 40;

                NumberSprite.draw(popupSkill.currentLevel,
                    e.SpriteBatch,
                    new Vector2(barPosX + (posNumber * this.config.popupScale), barPosY + (12 * this.config.popupScale)),
                    globalChangeColor,
                    BarController.AdjustLevelScale(this.config.popupScale, popupSkill.currentLevel, maxLevel),
                    0, 1, 0);

                // Draw Experience Bars
                Color barColor;
                if (popupSkill.currentLevel >= maxLevel) barColor = popupSkill.skillFinalColor;
                else barColor = popupSkill.skillColor;

                e.SpriteBatch.Draw(barFiller,
                    BarController.GetExperienceBar(new Vector2(barPosX + (43 * this.config.popupScale), barPosY + ((25 * this.config.popupScale) / 2) - ((barFiller.Height * this.config.popupScale) / 2)),
                        new Vector2(58, barFiller.Height), 
                        popupSkill.currentEXP,
                        popupSkill.currentLevel,
                        maxLevel,
                        this.config.popupScale),
                    barColor);

                // Draw Experience Text
                if (this.config.ShowExperiencePopupInfo)
                {
                    byte alpha;
                    Color actualColor;
                    if (popupSkill.currentLevel < maxLevel)
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
                        BarController.GetExperienceText(popupSkill.currentEXP, popupSkill.currentLevel, maxLevel),
                        new Vector2(barPosX + (43 * this.config.popupScale) + 5,
                        barPosY + ((25 * this.config.popupScale) / 2) - ((barFiller.Height * this.config.popupScale) / 2)),
                        MyHelper.ChangeColorIntensity(actualColor, 0.45f, alpha), 0f, Vector2.Zero,
                        BarController.AdjustExperienceScale(this.config.popupScale), SpriteEffects.None, 1);
                }
            }


            // In-Config background
            if (inConfigMode)
                e.SpriteBatch.Draw(backgroundBoxConfig, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Color(0, 0, 0, 0.50f));

            if (this.config.ShowButtons)
            {
                if (inConfigMode)
                {
                    e.SpriteBatch.Draw(buttonConfigApply, new Rectangle(configButtonPosX, configButtonPosY, buttonConfig.Width * 3, buttonConfig.Height * 3), Color.White);
                    e.SpriteBatch.Draw(buttonReset, new Rectangle(configButtonPosX + 75, configButtonPosY, buttonReset.Width * 3, buttonReset.Height * 3), Color.White);
                }
                else
                    e.SpriteBatch.Draw(buttonConfig, new Rectangle(configButtonPosX, configButtonPosY, buttonConfig.Width * 3, buttonConfig.Height * 3), Color.White);
            }
            else
            {
                if (inConfigMode)
                {
                    e.SpriteBatch.Draw(buttonConfigApply, new Rectangle(configButtonPosX, configButtonPosY, buttonConfig.Width * 3, buttonConfig.Height * 3), Color.White);
                    e.SpriteBatch.Draw(buttonReset, new Rectangle(configButtonPosX + 75, configButtonPosY, buttonReset.Width * 3, buttonReset.Height * 3), Color.White);
                }
            }

            // Draw config button
            if (this.config.ShowButtons)
            {
                if (!inConfigMode)
                {
                    if (!this.config.ShowUI)
                        e.SpriteBatch.Draw(buttonHidden, new Rectangle(configButtonPosX + 75, configButtonPosY, buttonConfig.Width * 3, buttonConfig.Height * 3), Color.White);
                    else
                        e.SpriteBatch.Draw(buttonVisibility, new Rectangle(configButtonPosX + 75, configButtonPosY, buttonConfig.Width * 3, buttonConfig.Height * 3), Color.White);
                }
            }

            if (!this.config.ShowUI) return;

            // Draw adjust buttons
            if (inConfigMode)
            {
                e.SpriteBatch.Draw(buttonDecreaseSize, new Rectangle(this.config.mainPosX, this.config.mainPosY - 30, buttonDecreaseSize.Width * 3, buttonDecreaseSize.Height * 3), decreaseSizeButtonColor);
                e.SpriteBatch.Draw(buttonIncreaseSize, new Rectangle(this.config.mainPosX + 25, this.config.mainPosY - 30, buttonIncreaseSize.Width * 3, buttonIncreaseSize.Height * 3), increaseSizeButtonColor);
                e.SpriteBatch.Draw(backgroundButton, new Rectangle(this.config.mainPosX + 75, this.config.mainPosY - 30, backgroundButton.Width * 3, backgroundButton.Height * 3), backgroundButtonColor);
                e.SpriteBatch.Draw(levelUpButton, new Rectangle(this.config.mainPosX + 100, this.config.mainPosY - 30, levelUpButton.Width * 3, levelUpButton.Height * 3), levelUpButtonColor);
                e.SpriteBatch.Draw(experienceButton, new Rectangle(this.config.mainPosX + 125, this.config.mainPosY - 30, experienceButton.Width * 3, experienceButton.Height * 3), experienceButtonColor);
            }

            // Draw Background
            if (this.config.ShowBoxBackground)
            {
                e.SpriteBatch.Draw(backgroundTop, new Rectangle(this.config.mainPosX, this.config.mainPosY, backgroundTop.Width * this.config.mainScale, backgroundTop.Height * this.config.mainScale), globalChangeColor);
                e.SpriteBatch.Draw(backgroundFiller, new Rectangle(this.config.mainPosX, this.config.mainPosY + (backgroundTop.Height * this.config.mainScale), backgroundTop.Width * this.config.mainScale, BarController.AdjustBackgroundSize(playerSkills.Count(), backgroundBar.Height * this.config.mainScale, barSpacement)), globalChangeColor);
                e.SpriteBatch.Draw(backgroundBottom, new Rectangle(this.config.mainPosX, this.config.mainPosY + (backgroundTop.Height * this.config.mainScale) + BarController.AdjustBackgroundSize(playerSkills.Count(), backgroundBar.Height * this.config.mainScale, barSpacement), backgroundTop.Width * this.config.mainScale, backgroundTop.Height * this.config.mainScale), globalChangeColor);
            }

            int posControlY = this.config.mainPosY + (backgroundTop.Height * this.config.mainScale) + (barSpacement / 2);

            // Draw Experience Bars
            foreach (skillHolder sh in playerSkills)
            {
                getPlayerInformation();
                int barPosX = this.config.mainPosX + ((int)MyHelper.GetSpriteCenter(backgroundFiller, this.config.mainScale).X - (int)MyHelper.GetSpriteCenter(backgroundBar, this.config.mainScale).X);

                //Draw bars background
                e.SpriteBatch.Draw(backgroundBar, 
                    new Rectangle(barPosX, posControlY, backgroundBar.Width * this.config.mainScale, backgroundBar.Height * this.config.mainScale), 
                    globalChangeColor);


                // Draw icons
                e.SpriteBatch.Draw(sh.skillIcon, new Rectangle(barPosX + (((26 * this.config.mainScale) / 2) - ((sh.skillIcon.Width * this.config.mainScale) / 2)),
                    posControlY + (((24 * this.config.mainScale) / 2) - (int)MyHelper.GetSpriteCenter(sh.skillIcon,
                    this.config.mainScale).Y), sh.skillIcon.Width * this.config.mainScale, sh.skillIcon.Height * this.config.mainScale),
                    globalChangeColor);

                // Draw level text
                int posNumber;
                if (sh.currentLevel < 10) posNumber = 34;
                else if (sh.currentLevel < 100) posNumber = 37;
                else posNumber = 40;

                NumberSprite.draw(sh.currentLevel,
                    e.SpriteBatch,
                    new Vector2(barPosX + (posNumber * this.config.mainScale), posControlY + (12 * this.config.mainScale)),
                    globalChangeColor,
                    BarController.AdjustLevelScale(this.config.mainScale, sh.currentLevel, maxLevel),
                    0, 1, 0);

                // Draw Experience Bars
                Color barColor;
                if (draggingBox)
                    if (sh.currentLevel >= maxLevel) barColor = MyHelper.ChangeColorIntensity(sh.skillFinalColor, 0.35f, 1);
                    else barColor = MyHelper.ChangeColorIntensity(sh.skillColor, 0.35f, 1);
                else
                    if (sh.currentLevel >= maxLevel) barColor = sh.skillFinalColor;
                    else barColor = sh.skillColor;

                e.SpriteBatch.Draw(barFiller,
                    BarController.GetExperienceBar(new Vector2(barPosX + (43 * this.config.mainScale),
                    posControlY + ((25 * this.config.mainScale) / 2) - ((barFiller.Height * this.config.mainScale) / 2)),
                    new Vector2(83, barFiller.Height), sh.currentEXP,
                    sh.currentLevel,
                    maxLevel,
                    this.config.mainScale),
                    barColor);
                    
                // Draw Experience Text
                if (this.config.ShowExperienceInfo)
                {
                    byte alpha;
                    Color actualColor;
                    if (sh.currentLevel < maxLevel)
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
                        BarController.GetExperienceText(sh.currentEXP, sh.currentLevel, maxLevel),
                        new Vector2(barPosX + (43 * this.config.mainScale) + 5,
                        posControlY + ((25 * this.config.mainScale) / 2) - ((barFiller.Height * this.config.mainScale) / 2)),
                        MyHelper.ChangeColorIntensity(actualColor, 0.45f, alpha), 0f, Vector2.Zero, 
                        BarController.AdjustExperienceScale(this.config.mainScale), SpriteEffects.None, 1);
                }

                if (sh.actualExpGainedMessage && this.config.ShowExperienceInfo)
                {
                    e.SpriteBatch.Draw(backgroundExp,
                        new Rectangle(barPosX + expAdvicePositionX, 
                        posControlY + ((backgroundBar.Height * this.config.mainScale) / 2) - ((backgroundExp.Height * this.config.mainScale) / 2), 
                        backgroundExp.Width * this.config.mainScale, 
                        backgroundExp.Height * this.config.mainScale), 
                        MyHelper.ChangeColorIntensity(globalChangeColor, 
                        1, sh.expAlpha));

                    Vector2 centralizedStringPos = MyHelper.GetStringCenter(sh.expGained.ToString(), Game1.dialogueFont);
                    e.SpriteBatch.DrawString(Game1.dialogueFont,
                        $"+{sh.expGained}", new Vector2(barPosX + expAdvicePositionX + ((backgroundExp.Width * this.config.mainScale) / 2) - centralizedStringPos.X, 
                        posControlY + ((25 * this.config.mainScale) / 2) - ((barFiller.Height * this.config.mainScale) / 2)), 
                        MyHelper.ChangeColorIntensity(sh.skillRestorationColor, 0.45f, sh.expAlpha), 0f, 
                        Vector2.Zero, BarController.AdjustExperienceScale(this.config.mainScale), 
                        SpriteEffects.None, 1);
                }

                posControlY += barSpacement + (backgroundBar.Height * this.config.mainScale);
            }
        }

        private void onSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Get current and load Previous
            getPlayerInformation();
            foreach (skillHolder sh in playerSkills)
                sh.setPreviousData();

            configButtonPosX = MyHelper.AdjustPositionMineLevelWidth(configButtonPosX, Game1.player.currentLocation, defaultButtonPosX);
        }

        private void onUpdate(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady && Game1.CurrentEvent == null) return;

            // Get New Player Info
            getPlayerInformation();

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
                    int toColor = sh.colorIndex;

                    // Color Values
                    int virtualColorValue;
                    byte intensity = 5;

                    if (sh.expIncreasing)
                    {
                        if (actualSkillLevel < 10)
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
                        if (actualSkillLevel < 10)
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
                if (sh.GainLevel())
                {
                    // Show level up information
                    levelUpIcon = sh.skillIcon;
                    levelUpMessage = Helper.Translation.Get("LevelUpMessage");
                    timeLeft = this.config.LevelUpMessageDuration * 60;
                    canShowLevelUp = true;

                    // Set Level up window and bool
                    levelUpPosY = (int)new Vector2(0, 0 - (backgroundLevelUp.Height * 3) - 5).Y;
                    animLevelUpDir = "bottom";
                    animDestPosLevelUp = new Vector2(0, 150);
                    animatingLevelUp = true;

                    // Play Level Up Sound
                    levelUpSound();
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

        private void levelUpSound()
        {
            if (levelUp == null) return;

            levelUp.Volume = Game1.options.soundVolumeLevel;
            Task.Factory.StartNew(async () => { 
                await Task.Delay(200);
                levelUp?.Play();
            });
        }

        private void getPlayerInformation()
        {
            if (playerSkills.Count <= 0)
                loadSkills();
            else
                foreach (skillHolder sh in playerSkills)
                    sh.setCurrentData();
        }

        private void onButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.CurrentEvent != null) return;

            Vector2 mousePos;
            mousePos.X = Game1.getMousePosition(true).X;
            mousePos.Y = Game1.getMousePosition(true).Y;

            //Reset button check click
            if (e.Button == SButton.MouseLeft &&
                mousePos.X >= configButtonPosX + 75 && mousePos.X <= configButtonPosX + 75 + (buttonReset.Width * 3) &&
                mousePos.Y >= configButtonPosY && mousePos.Y <= configButtonPosY + (buttonReset.Height * 3) &&
                inConfigMode)
            {
                configReset();
            }
            //Reset Box Position - Button
            if (e.Button == this.config.ResetKey && inConfigMode)
                configReset();

            //Config button click check - Button
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
                    mousePos.X >= this.config.mainPosX && mousePos.X <= this.config.mainPosX + (buttonDecreaseSize.Width * 3) &&
                    mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecreaseSize.Height * 3))
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
                    mousePos.X >= this.config.mainPosX + 25 && mousePos.X <= (this.config.mainPosX + 25) + (buttonDecreaseSize.Width * 3) &&
                    mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecreaseSize.Height * 3))
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
                    mousePos.X >= this.config.mainPosX + 75 && mousePos.X <= (this.config.mainPosX + 75) + (buttonDecreaseSize.Width * 3) &&
                    mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecreaseSize.Height * 3))
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

                //Levelup toggler button check click
                if (e.Button == SButton.MouseLeft &&
                    mousePos.X >= this.config.mainPosX + 100 && mousePos.X <= (this.config.mainPosX + 100) + (buttonDecreaseSize.Width * 3) &&
                    mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecreaseSize.Height * 3))
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

                //Experience toggler button check click
                if (e.Button == SButton.MouseLeft &&
                    mousePos.X >= this.config.mainPosX + 125 && mousePos.X <= (this.config.mainPosX + 125) + (buttonDecreaseSize.Width * 3) &&
                    mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecreaseSize.Height * 3))
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

            //Reset button check click
            if (mousePos.X >= configButtonPosX + 75 && mousePos.X <= configButtonPosX + 75 + (buttonReset.Width * 3) &&
                mousePos.Y >= configButtonPosY && mousePos.Y <= configButtonPosY + (buttonReset.Height * 3) &&
                inConfigMode)
            {
                blockActions();
            }

            //CONFIG KEY
            else if (mousePos.X >= configButtonPosX && mousePos.X <= configButtonPosX + (buttonConfig.Width * 3) &&
                mousePos.Y >= configButtonPosY && mousePos.Y <= configButtonPosY + (buttonConfig.Height * 3))
            {
                blockActions();
            }

            //TOGGLE UI
            else if (mousePos.X >= configButtonPosX + 75 && mousePos.X <= configButtonPosX + 75 + (buttonConfig.Width * 3) &&
                mousePos.Y >= configButtonPosY && mousePos.Y <= configButtonPosY + (buttonConfig.Height * 3))
            {
                blockActions();
            }

            //Box click check
            else if (mousePos.X >= this.config.mainPosX && mousePos.X <= this.config.mainPosX + (backgroundTop.Width * this.config.mainScale) &&
                mousePos.Y >= this.config.mainPosY && mousePos.Y <= this.config.mainPosY + totalBackgroundSize &&
                inConfigMode)
            {
                blockActions();
            }

            //Decrease button click check
            else if (mousePos.X >= this.config.mainPosX && mousePos.X <= this.config.mainPosX + (buttonDecreaseSize.Width * 3) &&
                mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecreaseSize.Height * 3) &&
                inConfigMode)
            {
                blockActions();
            }

            //Increase button click check
            else if (mousePos.X >= this.config.mainPosX + 25 && mousePos.X <= (this.config.mainPosX + 25) + (buttonDecreaseSize.Width * 3) &&
                mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecreaseSize.Height * 3) &&
                inConfigMode)
            {
                blockActions();
            }

            //Background toggler button check click
            else if (mousePos.X >= this.config.mainPosX + 75 && mousePos.X <= (this.config.mainPosX + 75) + (buttonDecreaseSize.Width * 3) &&
                mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecreaseSize.Height * 3) &&
                inConfigMode)
            {
                blockActions();
            }

            //Levelup toggler button check click
            else if (mousePos.X >= this.config.mainPosX + 100 && mousePos.X <= (this.config.mainPosX + 100) + (buttonDecreaseSize.Width * 3) &&
                mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecreaseSize.Height * 3) &&
                inConfigMode)
            {
                blockActions();
            }

            //Experience toggler button check click
            else if (mousePos.X >= this.config.mainPosX + 125 && mousePos.X <= (this.config.mainPosX + 125) + (buttonDecreaseSize.Width * 3) &&
                mousePos.Y >= this.config.mainPosY - 30 && mousePos.Y <= (this.config.mainPosY - 30) + (buttonDecreaseSize.Height * 3) &&
                inConfigMode)
            {
                blockActions();
            }

            else
            {
                unblockActions();
            }
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
