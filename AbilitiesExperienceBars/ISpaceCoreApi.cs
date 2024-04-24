using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AbilitiesExperienceBars
{
    // https://github.com/spacechase0/StardewValleyMods/blob/develop/SpaceCore/Api.cs
    public interface ISpaceCoreApi
    {
        string[] GetCustomSkills();
        int GetLevelForCustomSkill(Farmer farmer, string skill);
        int GetExperienceForCustomSkill(Farmer farmer, string skill);
        Texture2D GetSkillIconForCustomSkill(string skill);
        Texture2D GetSkillPageIconForCustomSkill(string skill);
    }
}
