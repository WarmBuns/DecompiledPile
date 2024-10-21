using HG;

namespace RoR2;

public static class DifficultyCatalog
{
	private static readonly DifficultyDef[] difficultyDefs;

	public static int standardDifficultyCount;

	static DifficultyCatalog()
	{
		standardDifficultyCount = 3;
		difficultyDefs = new DifficultyDef[11];
		difficultyDefs[0] = new DifficultyDef(1f, "DIFFICULTY_EASY_NAME", "Textures/DifficultyIcons/texDifficultyEasyIcon", "DIFFICULTY_EASY_DESCRIPTION", ColorCatalog.GetColor(ColorCatalog.ColorIndex.EasyDifficulty), "dz", countsAsHardMode: false);
		difficultyDefs[1] = new DifficultyDef(2f, "DIFFICULTY_NORMAL_NAME", "Textures/DifficultyIcons/texDifficultyNormalIcon", "DIFFICULTY_NORMAL_DESCRIPTION", ColorCatalog.GetColor(ColorCatalog.ColorIndex.NormalDifficulty), "rs", countsAsHardMode: false);
		difficultyDefs[2] = new DifficultyDef(3f, "DIFFICULTY_HARD_NAME", "Textures/DifficultyIcons/texDifficultyHardIcon", "DIFFICULTY_HARD_DESCRIPTION", ColorCatalog.GetColor(ColorCatalog.ColorIndex.HardDifficulty), "mn", countsAsHardMode: true);
		difficultyDefs[3] = new DifficultyDef(3f, "ECLIPSE_1_NAME", "Textures/DifficultyIcons/texDifficultyEclipse1Icon", "ECLIPSE_1_DESCRIPTION", ColorCatalog.GetColor(ColorCatalog.ColorIndex.HardDifficulty), "mn", countsAsHardMode: true);
		difficultyDefs[4] = new DifficultyDef(3f, "ECLIPSE_2_NAME", "Textures/DifficultyIcons/texDifficultyEclipse2Icon", "ECLIPSE_2_DESCRIPTION", ColorCatalog.GetColor(ColorCatalog.ColorIndex.HardDifficulty), "mn", countsAsHardMode: true);
		difficultyDefs[5] = new DifficultyDef(3f, "ECLIPSE_3_NAME", "Textures/DifficultyIcons/texDifficultyEclipse3Icon", "ECLIPSE_3_DESCRIPTION", ColorCatalog.GetColor(ColorCatalog.ColorIndex.HardDifficulty), "mn", countsAsHardMode: true);
		difficultyDefs[6] = new DifficultyDef(3f, "ECLIPSE_4_NAME", "Textures/DifficultyIcons/texDifficultyEclipse4Icon", "ECLIPSE_4_DESCRIPTION", ColorCatalog.GetColor(ColorCatalog.ColorIndex.HardDifficulty), "mn", countsAsHardMode: true);
		difficultyDefs[7] = new DifficultyDef(3f, "ECLIPSE_5_NAME", "Textures/DifficultyIcons/texDifficultyEclipse5Icon", "ECLIPSE_5_DESCRIPTION", ColorCatalog.GetColor(ColorCatalog.ColorIndex.HardDifficulty), "mn", countsAsHardMode: true);
		difficultyDefs[8] = new DifficultyDef(3f, "ECLIPSE_6_NAME", "Textures/DifficultyIcons/texDifficultyEclipse6Icon", "ECLIPSE_6_DESCRIPTION", ColorCatalog.GetColor(ColorCatalog.ColorIndex.HardDifficulty), "mn", countsAsHardMode: true);
		difficultyDefs[9] = new DifficultyDef(3f, "ECLIPSE_7_NAME", "Textures/DifficultyIcons/texDifficultyEclipse7Icon", "ECLIPSE_7_DESCRIPTION", ColorCatalog.GetColor(ColorCatalog.ColorIndex.HardDifficulty), "mn", countsAsHardMode: true);
		difficultyDefs[10] = new DifficultyDef(3f, "ECLIPSE_8_NAME", "Textures/DifficultyIcons/texDifficultyEclipse8Icon", "ECLIPSE_8_DESCRIPTION", ColorCatalog.GetColor(ColorCatalog.ColorIndex.HardDifficulty), "mn", countsAsHardMode: true);
	}

	public static DifficultyDef GetDifficultyDef(DifficultyIndex difficultyIndex)
	{
		return ArrayUtils.GetSafe(difficultyDefs, (int)difficultyIndex);
	}
}
