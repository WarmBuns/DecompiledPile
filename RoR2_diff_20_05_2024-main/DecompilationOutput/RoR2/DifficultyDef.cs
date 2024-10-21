using UnityEngine;

namespace RoR2;

public class DifficultyDef
{
	public readonly float scalingValue;

	public readonly string descriptionToken;

	public readonly string nameToken;

	public readonly string iconPath;

	public readonly Color color;

	public readonly string serverTag;

	private Sprite iconSprite;

	private bool foundIconSprite;

	public bool countsAsHardMode { get; private set; }

	public DifficultyDef(float scalingValue, string nameToken, string iconPath, string descriptionToken, Color color, string serverTag, bool countsAsHardMode)
	{
		this.scalingValue = scalingValue;
		this.descriptionToken = descriptionToken;
		this.nameToken = nameToken;
		this.iconPath = iconPath;
		this.color = color;
		this.serverTag = serverTag;
		this.countsAsHardMode = countsAsHardMode;
	}

	public Sprite GetIconSprite()
	{
		if (!foundIconSprite)
		{
			iconSprite = LegacyResourcesAPI.Load<Sprite>(iconPath);
			foundIconSprite = iconSprite;
		}
		return iconSprite;
	}
}
