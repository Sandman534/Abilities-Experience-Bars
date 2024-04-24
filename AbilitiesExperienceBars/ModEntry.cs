﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AbilitiesExperienceBars
{
    public class ModEntry : StardewModdingAPI.Mod
    {
        #region // Variables

        // Holds all player skills
        private List<SkillEntry> playerSkills = new();

        // Private Variables
        private int levelUpPosY;
        private int expAdvicePositionX;

        // Sprite Variables
        private Texture2D iconSheet, barSheet, barFiller;

        // Sprite Locations
        private Rectangle backgroundTop = new(98, 0, 116, 5);
        private Rectangle backgroundMiddle = new(98, 5, 116, 1);
        private Rectangle backgroundBottom = new(98, 6, 116, 5);
        private Rectangle backgroundBar = new(0, 0, 98, 22);
        private Rectangle backgroundLevelUp = new(0, 22, 86, 37);
        private Rectangle backgroundExp = new(0, 59, 34, 17);

        // Screen Alignment Variables
        private readonly int barSpacement = 8;
        readonly int[] expTextOffeset = new int[6] { 0, 19, 21, 22, 22, 22 };
        readonly int[] levelPosition = new int[11] { 29, 28, 29, 29, 29, 29, 29, 28, 28, 28, 31 };

        // Color Variables
        private Color globalChangeColor = Color.White;

        // Animation Variables
        public bool animatingBox, animatingLevelUp;
        public Vector2 animDestPosBox, animDestPosLevelUp;
        public string animBoxDir, animLevelUpDir;

        // Control Variables
        private bool canShowLevelUp;
        private bool canCountTimer;
        private bool canCountPopupTimer;

        // Data Variables
        public ModEntry instance;
        private ModConfig config;

        // Mastery Variables
        private readonly List<string> masterySkills = new() { "farming", "fishing", "foraging", "mining", "combat" };
        private bool masteryProcessed;

        // Timer Variables
        private float timeLeft;
        private float timeLeftPopup;

        // Level Up Variables
        private Texture2D levelUpIcon;
        private Rectangle levelUpRectangle;
        private string levelUpMessage;
        private readonly string levelUpID = "abilitybars.LevelUp";

        // Experience Popup
        private SkillEntry popupSkill;
        private bool sampleRunOnce;

        // Load Control
        private bool loadedSaveFlag;

        #endregion

        public override void Entry(IModHelper helper)
        {
            instance = this;
            ConfigGet();
            LoadTextures();
            LoadSound();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Display.RenderedHud += onRenderedHud;
            helper.Events.Input.ButtonPressed += onButtonPressed;
            helper.Events.GameLoop.UpdateTicked += onUpdate;
            helper.Events.GameLoop.SaveLoaded += onSaveLoaded;
        }

        #region // Events

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
                    setValue: value => LoadUITheme(value)
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("SmallIcon"),
                    tooltip: () => Helper.Translation.Get("SmallIconT"),
                    getValue: () => config.SmallIcons,
                    setValue: value => config.SmallIcons = value
                );

                // Interface
                configMenu.AddSectionTitle(
                    mod: ModManifest,
                    text: () => Helper.Translation.Get("ExperienceBarM")
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ShowUI"),
                    tooltip: () => Helper.Translation.Get("ShowUIT"),
                    getValue: () => config.ShowUI,
                    setValue: value => config.ShowUI = value
                );
                configMenu.AddKeybind(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("ToggleKey"),
                    tooltip: () => Helper.Translation.Get("ToggleKeyT"),
                    getValue: () => config.ToggleKey,
                    setValue: value => config.ToggleKey = value
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
                    e.SpriteBatch.Draw(levelUpIcon,
                        new Rectangle(((Game1.uiViewport.Width / 2) - (10 * 3) / 2), levelUpPosY + 15, 10 * 3, 10 * 3),
                        levelUpRectangle,
                        Color.White);
                else
                    e.SpriteBatch.Draw(levelUpIcon,
                        new Rectangle(((Game1.uiViewport.Width / 2) - (16 * 3) / 2), levelUpPosY + 9, 16 * 3, 16 * 3),
                        levelUpRectangle,
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
                ExperienceBar(popupSkill, e.SpriteBatch, this.config.popupPosX, this.config.popupPosY);

            if (!this.config.ShowUI) return;

            // Draw Experience Window Background
            if (this.config.ShowBoxBackground)
            {
                e.SpriteBatch.Draw(barSheet,
                    new Rectangle(this.config.mainPosX, this.config.mainPosY, backgroundTop.Width * this.config.mainScale, backgroundTop.Height * this.config.mainScale),
                    backgroundTop,
                    globalChangeColor);

                e.SpriteBatch.Draw(barSheet,
                    new Rectangle(this.config.mainPosX, this.config.mainPosY + (backgroundTop.Height * this.config.mainScale), backgroundTop.Width * this.config.mainScale, AdjustBackgroundSize(playerSkills.Count, backgroundBar.Height * this.config.mainScale, barSpacement)),
                    backgroundMiddle,
                    globalChangeColor);

                e.SpriteBatch.Draw(barSheet,
                    new Rectangle(this.config.mainPosX, this.config.mainPosY + (backgroundTop.Height * this.config.mainScale) + AdjustBackgroundSize(playerSkills.Count, backgroundBar.Height * this.config.mainScale, barSpacement), backgroundTop.Width * this.config.mainScale, backgroundTop.Height * this.config.mainScale),
                    backgroundBottom,
                    globalChangeColor);
            }

            // Draw Experience Bars
            int barPosX = this.config.mainPosX + (((backgroundMiddle.Width / 2) * this.config.mainScale) - ((backgroundBar.Width / 2) * this.config.mainScale));
            int barPosY = this.config.mainPosY + (backgroundTop.Height * this.config.mainScale) + (barSpacement / 2);
            foreach (SkillEntry se in playerSkills)
            {
                ExperienceBar(se, e.SpriteBatch, barPosX, barPosY);
                barPosY += barSpacement + (backgroundBar.Height * this.config.mainScale);
            }
        }

        private void onButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            //Toggle UI - Button
            if (e.Button == this.config.ToggleKey) ToggleUI();
        }

        private void onSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Reset player skills   
            playerSkills = new List<SkillEntry>();

            // Set Load Flag for later data
            loadedSaveFlag = false;
        }

        private void onUpdate(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady && Game1.CurrentEvent == null) return;

            // Get New Player Info
            GetPlayerInformation();

            // Animate experience or level gains
            AnimateThings();

            // Experience Gain Check
            CheckExperienceGain();
            ExpTimer();
            ExpAlphaChanger();

            // Experience Popup Timer
            ExpPopupTimer();

            // Level Up Check
            CheckLevelGain();
            LevelUpTimer();

            // Check position
            RepositionExpInfo();
        }

        #endregion

        #region // Configuration

        private void ConfigGet()
        {
            this.config = this.Helper.ReadConfig<ModConfig>();
            ConfigAdjust();
        }

        private void ConfigSave()
        {
            this.Helper.WriteConfig(config);
        }

        private void ConfigAdjust()
        {
            // Main Window Scale Catch
            if (this.config.mainScale < 1)
                this.config.mainScale = 1;
            else if (this.config.mainScale > 5)
                this.config.mainScale = 5;

            // Popup Scale Catch
            if (this.config.popupScale < 1)
                this.config.popupScale = 1;
            else if (this.config.popupScale > 5)
                this.config.popupScale = 5;

            // Level up message duration catch
            if (this.config.LevelUpMessageDuration < 1)
                this.config.LevelUpMessageDuration = 1;

            // Popup message duration catch
            if (this.config.PopupMessageDuration < 1)
                this.config.PopupMessageDuration = 1;

            // Default UI theme catch
            this.config.UITheme ??= "Vanilla";

            ConfigSave();
        }

        #endregion

        #region // Load Resources

        private void LoadUITheme(string value)
        {
            config.UITheme = value;
            LoadTextures();
        }

        private void LoadSound()
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

        private void LoadTextures()
        {
            // Load Theme
            string uiPath = config.UITheme == null ? $"assets/ui/themes/Vanilla.png" : $"assets/ui/themes/{config.UITheme}.png";
            barSheet = Helper.ModContent.Load<Texture2D>(uiPath);

            // Icons and Bar Filler
            iconSheet = Helper.ModContent.Load<Texture2D>("assets/ui/icons.png");
            barFiller = Helper.ModContent.Load<Texture2D>("assets/ui/barFiller.png");
        }

        private void LoadSkills()
        {
            // Setup API
            bool levelExtended = false;
            if (this.Helper.ModRegistry.IsLoaded("GoldstoneBosonMeadows.LevelForever"))
                levelExtended = true;
            var spaceCoreAPI = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");

            // Add Base Skills
            playerSkills.Add(new SkillEntry(spaceCoreAPI, "farming", 1, iconSheet, new Color(115, 150, 56), levelExtended));
            playerSkills.Add(new SkillEntry(spaceCoreAPI, "fishing", 2, iconSheet, new Color(117, 150, 150), levelExtended));
            playerSkills.Add(new SkillEntry(spaceCoreAPI, "foraging", 6, iconSheet, new Color(145, 102, 0), levelExtended));
            playerSkills.Add(new SkillEntry(spaceCoreAPI, "mining", 3, iconSheet, new Color(150, 80, 120), levelExtended));
            playerSkills.Add(new SkillEntry(spaceCoreAPI, "combat", 4, iconSheet, new Color(150, 31, 0), levelExtended));

            // Mod Compatibility
            if (this.Helper.ModRegistry.IsLoaded("spacechase0.LuckSkill"))
                playerSkills.Add(new SkillEntry(spaceCoreAPI, "luck", 5, iconSheet, new Color(150, 150, 0), levelExtended));
            if (this.Helper.ModRegistry.IsLoaded("spacechase0.CookingSkill"))
                playerSkills.Add(new SkillEntry(spaceCoreAPI, "cooking", 12, iconSheet, new Color(196, 76, 255), levelExtended));
            if (this.Helper.ModRegistry.IsLoaded("moonslime.CookingSkill"))
                playerSkills.Add(new SkillEntry(spaceCoreAPI, "moonslime.Cooking", 10, iconSheet, new Color(196, 76, 255), levelExtended));
            if (this.Helper.ModRegistry.IsLoaded("blueberry.LoveOfCooking"))
                playerSkills.Add(new SkillEntry(spaceCoreAPI, "blueberry.LoveOfCooking.CookingSkill", 11, iconSheet, new Color(57, 135, 214), levelExtended));
            if (this.Helper.ModRegistry.IsLoaded("moonslime.ArchaeologySkill"))
                playerSkills.Add(new SkillEntry(spaceCoreAPI, "moonslime.Archaeology", 7, iconSheet, new Color(205, 127, 50), levelExtended));
            if (this.Helper.ModRegistry.IsLoaded("drbirbdev.SocializingSkill"))
                playerSkills.Add(new SkillEntry(spaceCoreAPI, "drbirbdev.Socializing", 9, iconSheet, new Color(221, 0, 59), levelExtended));
            if (this.Helper.ModRegistry.IsLoaded("Achtuur.StardewTravelSkill"))
                playerSkills.Add(new SkillEntry(spaceCoreAPI, "Achtuur.Travelling", 13, iconSheet, new Color(100, 189, 132), levelExtended));
            if (this.Helper.ModRegistry.IsLoaded("drbirbdev.BinningSkill"))
                playerSkills.Add(new SkillEntry(spaceCoreAPI, "drbirbdev.Binning", 8, iconSheet, new Color(60, 60, 77), levelExtended));
            if (this.Helper.ModRegistry.IsLoaded("spacechase0.Magic"))
                playerSkills.Add(new SkillEntry(spaceCoreAPI, "magic", 14, iconSheet, new Color(0, 66, 255), levelExtended));

            // Add custom skills not accounted for
            if (spaceCoreAPI != null ) 
                foreach (string skillID in spaceCoreAPI.GetCustomSkills())
                    playerSkills.Add(new SkillEntry(spaceCoreAPI, skillID, -1, null, new Color(210, 210, 210), levelExtended));
        }

        #endregion

        #region // Experience and Level Checks

        private void GetPlayerInformation()
        {
            // Load skills or just set current data
            if (playerSkills.Count <= 0)
                LoadSkills();
            else
                foreach (SkillEntry sh in playerSkills)
                    sh.SetSkillData(true);

            // Load prior data if we just loaded a save - SpaceCore skill data is no available immedietly on load
            if (!loadedSaveFlag)
            {
                foreach (SkillEntry sh in playerSkills)
                    sh.SetSkillData(false);

                loadedSaveFlag = true;
            }

            // Check for player mastery
            if (!masteryProcessed && playerSkills.Where(x => x.isMastered).Count() == 5)
            {
                playerSkills.Insert(0, new SkillEntry(null, "mastery", 15, iconSheet, new Color(39, 185, 101), false));
                playerSkills[0].SetSkillData(false);
                masteryProcessed = true;
            }
        }

        private void CheckExperienceGain()
        {
            foreach (SkillEntry sh in playerSkills)
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

        private void ExpTimer()
        {
            if (playerSkills.Any(sh => sh.animateSkill == true)) {
                List<SkillEntry> animateList = playerSkills.FindAll(sh => sh.animateSkill == true);

                foreach (SkillEntry sh in animateList)
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

        private void ExpAlphaChanger()
        {
            if (playerSkills.Any(sh => sh.actualExpGainedMessage == true))
            {
                List<SkillEntry> expGainedList = playerSkills.FindAll(sh => sh.actualExpGainedMessage == true);
                foreach (SkillEntry sh in expGainedList)
                    sh.ExperienceAlpha(12);
            }
        }

        private void ExpPopupTimer()
        {
            if (!canCountPopupTimer) return;
            if (timeLeftPopup > 0) timeLeftPopup--;
            else
            {
                canCountPopupTimer = false;
                popupSkill = null;
                foreach (SkillEntry sh in playerSkills)
                    sh.expPopup = false;
            }
        }

        private void CheckLevelGain()
        {
            foreach (SkillEntry sh in playerSkills)
            {
                if (sh.GainLevel() && !sh.isMastery)
                {
                    // Show level up information
                    if (this.config.SmallIcons)
                    {
                        levelUpIcon = sh.smallIcon;
                        levelUpRectangle = sh.smallIconRectangle;
                    }
                    else
                    {
                        levelUpIcon = sh.bigIcon;
                        levelUpRectangle = sh.bigIconRectangle;
                    }

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

        private void LevelUpTimer()
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

        #endregion

        #region // UI Functions

        private void ExperienceBar(SkillEntry se, SpriteBatch b, int barPosX, int barPosY)
        {
            //Draw bars background
            b.Draw(barSheet,
                new Rectangle(barPosX, barPosY, backgroundBar.Width * this.config.mainScale, backgroundBar.Height * this.config.mainScale),
                backgroundBar,
                globalChangeColor);

            // Draw icons
            if (config.SmallIcons)
                b.Draw(se.smallIcon,
                    new Rectangle(barPosX + (6 * this.config.mainScale), barPosY + (6 * this.config.mainScale), 10 * this.config.mainScale, 10 * this.config.mainScale),
                    se.smallIconRectangle,
                    globalChangeColor);
            else
                b.Draw(se.bigIcon,
                    new Rectangle(barPosX + (3 * this.config.mainScale), barPosY + (3 * this.config.mainScale), 16 * this.config.mainScale, 16 * this.config.mainScale),
                    se.bigIconRectangle,
                    globalChangeColor);

            // Draw level text
            DrawLevel(b,
                se.currentLevel,
                se.maxLevel,
                barPosX + ((se.currentLevel > 9 && se.currentLevel != se.maxLevel ? 25 : 24) * this.config.mainScale),
                barPosY + ((se.currentLevel > 9 && se.currentLevel != se.maxLevel ? 11 : 7) * this.config.mainScale),
                globalChangeColor,
                se.LevelTextScale(this.config.mainScale));

            // Draw Experience Bars
            Color barColor = se.currentLevel >= se.maxLevel ? se.skillFinalColor : se.skillColor;
            b.Draw(barFiller,
                se.GetExperienceBar(new Vector2(barPosX + (37 * this.config.mainScale), barPosY + (6 * this.config.mainScale)),
                    new Vector2(58, barFiller.Height),
                    this.config.mainScale),
                barColor);

            // Draw Experience Text
            if (this.config.ShowExperienceInfo)
            {
                byte alpha = se.currentLevel < se.maxLevel ? se.skillColor.A : se.skillFinalColor.A;
                Color actualColor = se.currentLevel < se.maxLevel ? se.skillColor : se.skillFinalColor;

                // Add to the X based on scale
                int expAddX = 6;
                if (this.config.mainScale == 2) expAddX = 5;
                else if (this.config.mainScale == 1) expAddX = 4;

                b.DrawString(Game1.dialogueFont,
                    se.GetExperienceText(this.config.mainScale),
                    new Vector2(barPosX + (37 * this.config.mainScale), barPosY + (expAddX * this.config.mainScale)),
                    MyHelper.ChangeColorIntensity(actualColor, 0.45f, alpha), 0f, Vector2.Zero,
                    se.ExperienceTextScale(this.config.mainScale), SpriteEffects.None, 1);
            }

            // Gained Experience Window
            if (se.actualExpGainedMessage && this.config.ShowExperienceInfo)
            {
                b.Draw(barSheet,
                    new Rectangle(barPosX + expAdvicePositionX,
                        barPosY + ((backgroundBar.Height * this.config.mainScale) / 2) - ((backgroundExp.Height * this.config.mainScale) / 2),
                        backgroundExp.Width * this.config.mainScale,
                        backgroundExp.Height * this.config.mainScale),
                    backgroundExp,
                    MyHelper.ChangeColorIntensity(globalChangeColor, 1, se.expAlpha));

                Vector2 centralizedStringPos = MyHelper.GetStringCenter(se.expGained.ToString(), Game1.dialogueFont);
                b.DrawString(Game1.dialogueFont,
                    $"+{se.expGained}",
                    new Vector2(barPosX + expAdvicePositionX + ((backgroundExp.Width * this.config.mainScale) / 2) - centralizedStringPos.X,
                        barPosY + (6 * this.config.mainScale)),
                    MyHelper.ChangeColorIntensity(se.skillRestorationColor, 0.45f, se.expAlpha),
                    0f,
                    Vector2.Zero,
                    se.ExperienceTextScale(this.config.mainScale),
                    SpriteEffects.None, 1);
            }
        }

        private void ToggleUI()
        {
            this.config.ShowUI = !this.config.ShowUI;
            ConfigSave();
        }

        public void DrawLevel(SpriteBatch b, int level, int maxLevel, int x, int y, Color c, float scale)
        {
            // Max Level, show a star
            if (level == maxLevel)
                b.Draw(Game1.mouseCursors, new Rectangle(x, y, 8 * this.config.mainScale, 8 * this.config.mainScale), new Rectangle(346, 392, 8, 8), c);

            // Level 10 - 99
            else if (level >= 10)
            {
                // Left Digit
                int digitLeft = level / 10;
                int imageX1 = digitLeft <= 5 ? 512 + digitLeft * 8 : 512 + (digitLeft - 6) * 8;
                int imageY1 = digitLeft <= 5 ? 128 : 136;

                // Right Digit
                int digitRight = level % 10;
                int imageX2 = digitRight <= 5 ? 512 + digitRight * 8 : 512 + (digitRight - 6) * 8;
                int imageY2 = digitRight <= 5 ? 128 : 136;

                // Draw Numbers
                b.Draw(Game1.mouseCursors, new Vector2(x, y), new Rectangle(imageX1, imageY1, 8, 8), c, 0f, new Vector2(4f, 4f), 4f * scale, SpriteEffects.None, 0);
                b.Draw(Game1.mouseCursors, new Vector2(x + (6 * this.config.mainScale), y), new Rectangle(imageX2, imageY2, 8, 8), c, 0f, new Vector2(4f, 4f), 4f * scale, SpriteEffects.None, 0);
            }

            // Level 0 - 9
            else
            {
                // Single Digit Number
                int imageX = level <= 5 ? 512 + level * 8 : 512 + (level - 6) * 8;
                int imageY = level <= 5 ? 128 : 136;
                b.Draw(Game1.mouseCursors, new Rectangle(x, y, 8 * this.config.mainScale, 8 * this.config.mainScale), new Rectangle(imageX, imageY, 8, 8), c);
            }
        }

        private void RepositionExpInfo()
        {
            if (!Context.IsWorldReady) return;

            int rightPosX = this.config.mainPosX + backgroundTop.Width * this.config.mainScale;
            if (rightPosX >= Game1.uiViewport.Width - (backgroundExp.Width * this.config.mainScale))
                expAdvicePositionX = -(backgroundExp.Width * this.config.mainScale + (10 * this.config.mainScale));
            else
                expAdvicePositionX = (backgroundTop.Width * this.config.mainScale) + 1;
        }

        public Vector2 Animate(Vector2 boxPos, Vector2 dirTo, float velocity, string dirName)
        {
            if (dirName == "top")
                return boxPos.Y > dirTo.Y ? new Vector2(boxPos.X, boxPos.Y -= velocity) : new Vector2(boxPos.X, dirTo.Y);
            else if (dirName == "bottom")
                return boxPos.Y > dirTo.Y ? new Vector2(boxPos.X, boxPos.Y += velocity) : new Vector2(boxPos.X, dirTo.Y);
            else if (dirName == "left")
                return boxPos.X > dirTo.X ? new Vector2(boxPos.X -= velocity, boxPos.Y) : new Vector2(dirTo.X, dirTo.Y);
            else if (dirName == "right")
                return boxPos.X < dirTo.X ? new Vector2(boxPos.X += velocity, boxPos.Y) : new Vector2(dirTo.X, dirTo.Y);
            else
                return boxPos;
        }

        private void AnimateThings()
        {
            Vector2 levelUpPosHolder = new(0, levelUpPosY);

            if (animatingLevelUp)
            {
                if (levelUpPosHolder != animDestPosLevelUp)
                {
                    levelUpPosHolder = Animate(levelUpPosHolder, animDestPosLevelUp, 8f, animLevelUpDir);
                    levelUpPosY = (int)levelUpPosHolder.Y;
                }
                else
                {
                    animatingLevelUp = false;
                    canCountTimer = true;
                }
            }
        }

        private int AdjustBackgroundSize(int barQuantity, int barHeight, int barSpacement)
        {
            int size = (barSpacement + barHeight) * barQuantity;
            return size;
        }

        #endregion
    }
}
