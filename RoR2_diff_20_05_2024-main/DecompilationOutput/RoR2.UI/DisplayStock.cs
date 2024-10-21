using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class DisplayStock : MonoBehaviour
{
	public SkillSlot skillSlot;

	public Image[] stockImages;

	public Sprite fullStockSprite;

	public Color fullStockColor;

	public Sprite emptyStockSprite;

	public Color emptyStockColor;

	private HudElement hudElement;

	private SkillLocator skillLocator;

	private void Awake()
	{
		hudElement = GetComponent<HudElement>();
	}

	private void Update()
	{
		if (!hudElement.targetCharacterBody)
		{
			return;
		}
		if (!skillLocator)
		{
			skillLocator = hudElement.targetCharacterBody.GetComponent<SkillLocator>();
		}
		if (!skillLocator)
		{
			return;
		}
		GenericSkill skill = skillLocator.GetSkill(skillSlot);
		if (!skill)
		{
			return;
		}
		for (int i = 0; i < stockImages.Length; i++)
		{
			if (skill.stock > i)
			{
				stockImages[i].sprite = fullStockSprite;
				stockImages[i].color = fullStockColor;
			}
			else
			{
				stockImages[i].sprite = emptyStockSprite;
				stockImages[i].color = emptyStockColor;
			}
		}
	}
}
