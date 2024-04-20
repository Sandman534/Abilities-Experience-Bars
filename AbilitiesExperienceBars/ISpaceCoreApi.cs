using StardewValley;

namespace AbilitiesExperienceBars
{
    // https://github.com/spacechase0/StardewValleyMods/blob/develop/SpaceCore/Api.cs
    public interface ISpaceCoreApi
    {
        int GetLevelForCustomSkill(Farmer farmer, string skill);
        int GetExperienceForCustomSkill(Farmer farmer, string skill);

    }
}
