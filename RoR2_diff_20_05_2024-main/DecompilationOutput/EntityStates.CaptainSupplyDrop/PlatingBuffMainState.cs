using RoR2;
using UnityEngine;

namespace EntityStates.CaptainSupplyDrop;

public class PlatingBuffMainState : BaseMainState
{
	public static int maximumBuffStack;

	public static int buffCountToGrant;

	[SerializeField]
	public float activationCost;

	private static BuffIndex buff => JunkContent.Buffs.BodyArmor.buffIndex;

	protected override bool shouldShowEnergy => true;

	protected override string GetContextString(Interactor activator)
	{
		return Language.GetString("CAPTAIN_SUPPLY_DEFENSE_RESTOCK_INTERACTION");
	}

	protected override Interactability GetInteractability(Interactor activator)
	{
		CharacterBody component = activator.GetComponent<CharacterBody>();
		if (!component)
		{
			return Interactability.Disabled;
		}
		if (energyComponent.energy < activationCost)
		{
			return Interactability.ConditionsNotMet;
		}
		if (component.GetBuffCount(buff) >= maximumBuffStack)
		{
			return Interactability.ConditionsNotMet;
		}
		return Interactability.Available;
	}

	protected override void OnInteractionBegin(Interactor activator)
	{
		CharacterBody component = activator.GetComponent<CharacterBody>();
		for (int i = 0; i < buffCountToGrant; i++)
		{
			if (component.GetBuffCount(buff) >= maximumBuffStack)
			{
				break;
			}
			if (!energyComponent.TakeEnergy(activationCost))
			{
				break;
			}
			component.AddBuff(buff);
		}
	}
}
