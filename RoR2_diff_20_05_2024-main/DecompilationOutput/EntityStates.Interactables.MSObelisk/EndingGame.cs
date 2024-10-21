using System.Collections.ObjectModel;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Interactables.MSObelisk;

public class EndingGame : BaseState
{
	public static GameObject destroyEffectPrefab;

	public static float timeBetweenDestroy;

	public static float timeUntilEndGame;

	private float destroyTimer;

	private float endGameTimer;

	private bool beginEndingGame;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			FixedUpdateServer();
		}
	}

	private void FixedUpdateServer()
	{
		destroyTimer -= GetDeltaTime();
		if (!beginEndingGame)
		{
			if (!(destroyTimer <= 0f))
			{
				return;
			}
			destroyTimer = timeBetweenDestroy;
			ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Player);
			if (teamMembers.Count > 0)
			{
				GameObject gameObject = teamMembers[0].gameObject;
				CharacterBody component = gameObject.GetComponent<CharacterBody>();
				if ((bool)component)
				{
					EffectManager.SpawnEffect(destroyEffectPrefab, new EffectData
					{
						origin = component.corePosition,
						scale = component.radius
					}, transmit: true);
					EntityState.Destroy(gameObject.gameObject);
				}
			}
			else
			{
				beginEndingGame = true;
			}
		}
		else
		{
			endGameTimer += GetDeltaTime();
			if (endGameTimer >= timeUntilEndGame && (bool)Run.instance)
			{
				DoFinalAction();
			}
		}
	}

	private void DoFinalAction()
	{
		bool flag = false;
		for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
		{
			if (CharacterMaster.readOnlyInstancesList[i].inventory.GetItemCount(RoR2Content.Items.LunarTrinket) > 0)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			outer.SetNextState(new TransitionToNextStage());
			return;
		}
		Run.instance.BeginGameOver(RoR2Content.GameEndings.ObliterationEnding);
		outer.SetNextState(new Idle());
	}
}
