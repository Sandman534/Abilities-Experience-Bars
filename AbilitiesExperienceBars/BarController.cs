using Microsoft.Xna.Framework;

namespace AbilitiesExperienceBars
{
    public static class BarController
    {
        //Control Vars
        private static readonly int[] expPerLevel = new int[] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };
        private static readonly int[] expPerMasteryLevel = new int[] { 10000, 25000, 45000, 70000, 100000 };

        //Functions
        public static Rectangle GetExperienceBar(Vector2 barPosition, Vector2 barSize, int actualExp, int level, int maxPossibleLevel, int scale, bool isMastery)
        {
            int[] levelRange = expPerLevel;
            if (isMastery) levelRange = expPerMasteryLevel;

            float percentage;
            if (level >= maxPossibleLevel || actualExp > levelRange[level])
                percentage = barSize.X;
            else if (level == 0)
                percentage = ((float)actualExp / (float)levelRange[level]) * barSize.X;
            else
                percentage = ((float)actualExp - (float)levelRange[level - 1]) / ((float)levelRange[level] - (float)levelRange[level - 1]) * barSize.X;

            Rectangle barRect = new((int)barPosition.X, (int)barPosition.Y, (int)percentage * scale, (int)barSize.Y * scale);
            return barRect;
        }
        public static string GetExperienceText(int actualExp, int level, int maxPossibleLevel, bool isMastery)
        {
            int[] levelRange = expPerLevel;
            if (isMastery) levelRange = expPerMasteryLevel;

            string expText;

            if (level == 0)
                expText = $"{actualExp}/{levelRange[level]}";
            else if (level >= maxPossibleLevel)
                expText = $"{actualExp} exp.";
            else
                expText = $"{actualExp - levelRange[level - 1]}/{levelRange[level] - levelRange[level - 1]}";

            return expText;
        }
        public static Vector2 GetMouseHoveringBar(Vector2 mousePos, Vector2 initialPos, int barQuantity, Vector2 barSize, float barSpacement)
        {
            Vector2 infoPosition = Vector2.Zero;

            for (var i = 0; i < barQuantity - 1; i++)
            {
                if (mousePos.X >= initialPos.X && mousePos.X <= barSize.X && mousePos.Y >= initialPos.Y + (barSpacement * i) && mousePos.Y <= barSize.Y)
                    infoPosition = new Vector2(initialPos.X, initialPos.Y + (barSpacement * i));
            }

            return infoPosition;
        }
        public static int AdjustBackgroundSize(int barQuantity, int barHeight, int barSpacement)
        {
            int size = (barSpacement + barHeight) * barQuantity;

            return size;
        }
        public static float AdjustLevelScale(int scale, int actualLevel, int maxLevel)
        {
            float t = 1;
            if (actualLevel < maxLevel)
            {
                switch (scale)
                {
                    case 0:
                        t = 0f;
                        break;
                    case 1:
                        t = 0.50f;
                        break;
                    case 2:
                        t = 0.75f;
                        break;
                    case 3:
                        t = 1f;
                        break;
                    case 4:
                        t = 1.25f;
                        break;
                    case 5:
                        t = 1.50f;
                        break;
                }
            }
            else
            {
                switch (scale)
                {
                    case 0:
                        t = 0f;
                        break;
                    case 1:
                        t = 0.25f;
                        break;
                    case 2:
                        t = 0.50f;
                        break;
                    case 3:
                        t = 0.75f;
                        break;
                    case 4:
                        t = 1f;
                        break;
                    case 5:
                        t = 1.25f;
                        break;
                }
            }

            float levelScale = t;
            return levelScale;
        }
        public static float AdjustExperienceScale(int scale)
        {
            float t = 0.7f;
            switch (scale)
            {
                case 0:
                    t = 0f;
                    break;
                case 1:
                    t = 0.3f;
                    break;
                case 2:
                    t = 0.5f;
                    break;
                case 3:
                    t = 0.7f;
                    break;
                case 4:
                    t = 0.9f;
                    break;
                case 5:
                    t = 1.1f;
                    break;
            }

            float levelScale = t;
            return levelScale;
        }
    }
}
