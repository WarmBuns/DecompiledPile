using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class ExpBar : MonoBehaviour
{
	public CharacterMaster source;

	public RectTransform fillRectTransform;

	private RectTransform rectTransform;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	public void Update()
	{
		TeamIndex teamIndex = (source ? source.teamIndex : TeamIndex.Neutral);
		float x = 0f;
		if ((bool)source && (bool)TeamManager.instance)
		{
			x = Mathf.InverseLerp(TeamManager.instance.GetTeamCurrentLevelExperience(teamIndex), TeamManager.instance.GetTeamNextLevelExperience(teamIndex), TeamManager.instance.GetTeamExperience(teamIndex));
		}
		if ((bool)fillRectTransform)
		{
			_ = rectTransform.rect;
			_ = fillRectTransform.rect;
			fillRectTransform.anchorMin = new Vector2(0f, 0f);
			fillRectTransform.anchorMax = new Vector2(x, 1f);
			fillRectTransform.sizeDelta = new Vector2(1f, 1f);
		}
	}
}
