using EntityStates.Sniper.Scope;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(HudElement))]
public class SniperScopeChargeIndicatorController : MonoBehaviour
{
	private GameObject sourceGameObject;

	private HudElement hudElement;

	public Image image;

	private void Awake()
	{
		hudElement = GetComponent<HudElement>();
	}

	private void FixedUpdate()
	{
		float fillAmount = 0f;
		if ((bool)hudElement.targetCharacterBody)
		{
			SkillLocator component = hudElement.targetCharacterBody.GetComponent<SkillLocator>();
			if ((bool)component && (bool)component.secondary)
			{
				EntityStateMachine stateMachine = component.secondary.stateMachine;
				if ((bool)stateMachine && stateMachine.state is ScopeSniper scopeSniper)
				{
					fillAmount = scopeSniper.charge;
				}
			}
		}
		if ((bool)image)
		{
			image.fillAmount = fillAmount;
		}
	}
}
