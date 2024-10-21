using RoR2;
using RoR2.VoidRaidCrab;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidRaidCrab;

public class SpawnState : BaseState
{
	[SerializeField]
	public float duration = 4f;

	[SerializeField]
	public string spawnSoundString;

	[SerializeField]
	public GameObject spawnEffectPrefab;

	[SerializeField]
	public string spawnMuzzleName;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public bool doLegs;

	[SerializeField]
	public CharacterSpawnCard jointSpawnCard;

	[SerializeField]
	public string leg1Name;

	[SerializeField]
	public string leg2Name;

	[SerializeField]
	public string leg3Name;

	[SerializeField]
	public string leg4Name;

	[SerializeField]
	public string leg5Name;

	[SerializeField]
	public string leg6Name;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		Util.PlaySound(spawnSoundString, base.gameObject);
		if ((bool)spawnEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(spawnEffectPrefab, base.gameObject, spawnMuzzleName, transmit: false);
		}
		if (doLegs && NetworkServer.active)
		{
			ChildLocator modelChildLocator = GetModelChildLocator();
			if ((bool)jointSpawnCard && (bool)modelChildLocator)
			{
				DirectorPlacementRule placementRule = new DirectorPlacementRule
				{
					placementMode = DirectorPlacementRule.PlacementMode.Direct,
					spawnOnTarget = base.transform
				};
				SpawnJointBodyForLegServer(leg1Name, modelChildLocator, placementRule);
				SpawnJointBodyForLegServer(leg2Name, modelChildLocator, placementRule);
				SpawnJointBodyForLegServer(leg3Name, modelChildLocator, placementRule);
				SpawnJointBodyForLegServer(leg4Name, modelChildLocator, placementRule);
				SpawnJointBodyForLegServer(leg5Name, modelChildLocator, placementRule);
				SpawnJointBodyForLegServer(leg6Name, modelChildLocator, placementRule);
			}
		}
	}

	private void SpawnJointBodyForLegServer(string legName, ChildLocator childLocator, DirectorPlacementRule placementRule)
	{
		DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(jointSpawnCard, placementRule, Run.instance.stageRng);
		directorSpawnRequest.summonerBodyObject = base.gameObject;
		GameObject gameObject = DirectorCore.instance?.TrySpawnObject(directorSpawnRequest);
		Transform transform = childLocator.FindChild(legName);
		if (!gameObject || !transform)
		{
			return;
		}
		CharacterMaster component = gameObject.GetComponent<CharacterMaster>();
		if ((bool)component)
		{
			LegController component2 = transform.GetComponent<LegController>();
			if ((bool)component2)
			{
				component2.SetJointMaster(component, transform.GetComponent<ChildLocator>());
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
