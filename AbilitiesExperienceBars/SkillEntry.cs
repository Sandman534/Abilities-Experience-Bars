using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace AbilitiesExperienceBars
{
    public class SkillEntry
    {
        // Skill ID
        public string skillID;

        // Skill Vectores
        public Rectangle smallIcon;
        public Rectangle bigIcon;

        // Skill Colors
        public Color skillColor;
        public Color skillRestorationColor;
        public Color skillFinalColor = new(150, 175, 55);
        public Color skillGoldColor = new(150, 175, 55);

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
        readonly ISpaceCoreApi _spaceCoreAPI;

        public SkillEntry(IModHelper Helper, string ID, int skillIndex, Color skillColorCode)
        {
            // Set the skill ID
            skillID = ID;

            // Setup API
            _spaceCoreAPI = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");

            // Load Skill Icon
            SetSkillIcon(skillIndex);

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
            SetSkillData(true);
        }

        public void SetSkillIcon(int skillIndex)
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

        public void SetSkillData(bool isCurrent)
        {
            // Stardew Base Skills
            if (skillID == "farming")
                SetData(Game1.player.farmingLevel.Value, Game1.player.experiencePoints[0], isCurrent);
            else if (skillID == "fishing")
                SetData(Game1.player.fishingLevel.Value, Game1.player.experiencePoints[1], isCurrent);
            else if (skillID == "foraging")
                SetData(Game1.player.foragingLevel.Value, Game1.player.experiencePoints[2], isCurrent);
            else if (skillID == "mining")
                SetData(Game1.player.miningLevel.Value, Game1.player.experiencePoints[3], isCurrent);
            else if (skillID == "combat")
                SetData(Game1.player.combatLevel.Value, Game1.player.experiencePoints[4], isCurrent);
            else if (skillID == "luck")
                SetData(Game1.player.luckLevel.Value, Game1.player.experiencePoints[5], isCurrent);
            else if (skillID == "mastery")
                SetData((int)Game1.stats.Get("masteryLevelsSpent"), (int)Game1.stats.Get("MasteryExp"), isCurrent);

            // Mod Added Skills
            else if (_spaceCoreAPI != null)
                SetData(_spaceCoreAPI.GetLevelForCustomSkill(Game1.player, skillID), _spaceCoreAPI.GetExperienceForCustomSkill(Game1.player, skillID), isCurrent);
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

        private void SetData(int iLevel, int iExp, bool bCurrent)
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
    }
}
