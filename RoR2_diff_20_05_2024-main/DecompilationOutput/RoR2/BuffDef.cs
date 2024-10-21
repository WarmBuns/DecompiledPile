using System;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/BuffDef")]
public class BuffDef : ScriptableObject
{
	[Obsolete("BuffDef.iconPath is deprecated and no longer functions. Use .iconSprite instead.", false)]
	public string iconPath = "Textures/ItemIcons/texNullIcon";

	public Sprite iconSprite;

	public Color buffColor = Color.white;

	public bool canStack;

	public EliteDef eliteDef;

	public bool isDebuff;

	public bool ignoreGrowthNectar;

	public bool isCooldown;

	public bool isHidden;

	public NetworkSoundEventDef startSfx;

	public BuffIndex buffIndex { get; set; } = BuffIndex.None;

	public bool isElite => (object)eliteDef != null;

	protected void OnValidate()
	{
		ReplaceIconFromPathWithDirectReference();
	}

	[ContextMenu("Update iconPath to iconSprite")]
	private void ReplaceIconFromPathWithDirectReference()
	{
		string text = iconPath;
		if (!string.IsNullOrEmpty(text))
		{
			Sprite sprite = LegacyResourcesAPI.Load<Sprite>(text);
			if ((bool)sprite)
			{
				iconSprite = sprite;
				iconPath = string.Empty;
				EditorUtil.SetDirty(this);
			}
		}
	}
}
