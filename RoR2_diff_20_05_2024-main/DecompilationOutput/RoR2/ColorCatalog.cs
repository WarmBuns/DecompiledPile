using UnityEngine;

namespace RoR2;

public class ColorCatalog
{
	public enum ColorIndex
	{
		None,
		Tier1Item,
		Tier2Item,
		Tier3Item,
		LunarItem,
		Equipment,
		Interactable,
		Teleporter,
		Money,
		Blood,
		Unaffordable,
		Unlockable,
		LunarCoin,
		BossItem,
		Error,
		EasyDifficulty,
		NormalDifficulty,
		HardDifficulty,
		Tier1ItemDark,
		Tier2ItemDark,
		Tier3ItemDark,
		LunarItemDark,
		BossItemDark,
		WIP,
		Artifact,
		VoidItem,
		VoidItemDark,
		VoidCoin,
		Count
	}

	private static readonly Color32[] indexToColor32;

	private static readonly string[] indexToHexString;

	private static readonly Color[] multiplayerColors;

	static ColorCatalog()
	{
		indexToColor32 = new Color32[28];
		indexToHexString = new string[28];
		multiplayerColors = new Color[4]
		{
			new Color32(252, 62, 62, byte.MaxValue),
			new Color32(62, 109, 252, byte.MaxValue),
			new Color32(129, 252, 62, byte.MaxValue),
			new Color32(252, 241, 62, byte.MaxValue)
		};
		indexToColor32[1] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		indexToColor32[2] = new Color32(119, byte.MaxValue, 23, byte.MaxValue);
		indexToColor32[3] = new Color32(231, 84, 58, byte.MaxValue);
		indexToColor32[4] = new Color32(48, 127, byte.MaxValue, byte.MaxValue);
		indexToColor32[5] = new Color32(byte.MaxValue, 128, 0, byte.MaxValue);
		indexToColor32[6] = new Color32(235, 232, 122, byte.MaxValue);
		indexToColor32[7] = new Color32(231, 84, 58, byte.MaxValue);
		indexToColor32[8] = new Color32(239, 235, 26, byte.MaxValue);
		indexToColor32[9] = new Color32(206, 41, 41, byte.MaxValue);
		indexToColor32[10] = new Color32(100, 100, 100, byte.MaxValue);
		indexToColor32[11] = Color32.Lerp(new Color32(142, 56, 206, byte.MaxValue), new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.575f);
		indexToColor32[12] = new Color32(173, 189, 250, byte.MaxValue);
		indexToColor32[13] = Color.yellow;
		indexToColor32[14] = new Color32(byte.MaxValue, 0, byte.MaxValue, byte.MaxValue);
		indexToColor32[15] = new Color32(106, 170, 95, byte.MaxValue);
		indexToColor32[16] = new Color32(173, 117, 80, byte.MaxValue);
		indexToColor32[17] = new Color32(142, 49, 49, byte.MaxValue);
		indexToColor32[18] = new Color32(193, 193, 193, byte.MaxValue);
		indexToColor32[19] = new Color32(88, 149, 88, byte.MaxValue);
		indexToColor32[20] = new Color32(142, 49, 49, byte.MaxValue);
		indexToColor32[21] = new Color32(76, 84, 144, byte.MaxValue);
		indexToColor32[22] = new Color32(189, 180, 60, byte.MaxValue);
		indexToColor32[23] = new Color32(200, 80, 0, byte.MaxValue);
		indexToColor32[24] = new Color32(140, 114, 219, byte.MaxValue);
		indexToColor32[25] = new Color32(237, 127, 205, byte.MaxValue);
		indexToColor32[26] = new Color32(163, 77, 132, byte.MaxValue);
		indexToColor32[27] = new Color32(244, 173, 250, byte.MaxValue);
		for (ColorIndex colorIndex = ColorIndex.None; colorIndex < ColorIndex.Count; colorIndex++)
		{
			indexToHexString[(int)colorIndex] = Util.RGBToHex(indexToColor32[(int)colorIndex]);
		}
	}

	public static Color32 GetColor(ColorIndex colorIndex)
	{
		if (colorIndex < ColorIndex.None || colorIndex >= ColorIndex.Count)
		{
			colorIndex = ColorIndex.Error;
		}
		return indexToColor32[(int)colorIndex];
	}

	public static string GetColorHexString(ColorIndex colorIndex)
	{
		if (colorIndex < ColorIndex.None || colorIndex >= ColorIndex.Count)
		{
			colorIndex = ColorIndex.Error;
		}
		return indexToHexString[(int)colorIndex];
	}

	public static Color GetMultiplayerColor(int playerSlot)
	{
		if (playerSlot >= 0 && playerSlot < multiplayerColors.Length)
		{
			return multiplayerColors[playerSlot];
		}
		return Color.black;
	}
}
