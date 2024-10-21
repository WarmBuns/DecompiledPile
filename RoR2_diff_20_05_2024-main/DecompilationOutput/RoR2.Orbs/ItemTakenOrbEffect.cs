using System.Collections;
using UnityEngine;

namespace RoR2.Orbs;

[RequireComponent(typeof(EffectComponent))]
public class ItemTakenOrbEffect : MonoBehaviour
{
	public TrailRenderer trailToColor;

	public ParticleSystem[] particlesToColor;

	public SpriteRenderer[] spritesToColor;

	public SpriteRenderer iconSpriteRenderer;

	private void OnEnable()
	{
		StartCoroutine(DelayedUpdateSprite());
	}

	private IEnumerator DelayedUpdateSprite()
	{
		yield return 0;
		ItemDef itemDef = ItemCatalog.GetItemDef((ItemIndex)Util.UintToIntMinusOne(GetComponent<EffectComponent>().effectData.genericUInt));
		ColorCatalog.ColorIndex colorIndex = ColorCatalog.ColorIndex.Error;
		Sprite sprite = null;
		if (itemDef != null)
		{
			colorIndex = itemDef.colorIndex;
			sprite = itemDef.pickupIconSprite;
		}
		Color color = ColorCatalog.GetColor(colorIndex);
		if (trailToColor != null)
		{
			trailToColor.startColor *= color;
			trailToColor.endColor *= color;
		}
		for (int i = 0; i < particlesToColor.Length; i++)
		{
			ParticleSystem obj = particlesToColor[i];
			ParticleSystem.MainModule main = obj.main;
			main.startColor = color;
			obj.Play();
		}
		for (int j = 0; j < spritesToColor.Length; j++)
		{
			spritesToColor[j].color = color;
		}
		iconSpriteRenderer.sprite = sprite;
	}
}
