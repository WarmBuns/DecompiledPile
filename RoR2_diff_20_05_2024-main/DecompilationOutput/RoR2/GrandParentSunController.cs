using System.Collections.Generic;
using RoR2.Audio;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(GenericOwnership))]
[RequireComponent(typeof(TeamFilter))]
public class GrandParentSunController : MonoBehaviour
{
	private TeamFilter teamFilter;

	private GenericOwnership ownership;

	public BuffDef buffDef;

	[Min(0.001f)]
	public float cycleInterval = 1f;

	[Min(0.001f)]
	public float nearBuffDuration = 1f;

	[Min(0.001f)]
	public float maxDistance = 1f;

	public int minimumStacksBeforeApplyingBurns = 4;

	public float burnDuration = 5f;

	public GameObject buffApplyEffect;

	[SerializeField]
	private LoopSoundDef activeLoopDef;

	[SerializeField]
	private LoopSoundDef damageLoopDef;

	[SerializeField]
	private string stopSoundName;

	private Run.FixedTimeStamp previousCycle = Run.FixedTimeStamp.negativeInfinity;

	private int cycleIndex;

	private List<HurtBox> cycleTargets = new List<HurtBox>();

	private BullseyeSearch bullseyeSearch = new BullseyeSearch();

	private bool isLocalPlayerDamaged;

	private void Awake()
	{
		teamFilter = GetComponent<TeamFilter>();
		ownership = GetComponent<GenericOwnership>();
	}

	private void Start()
	{
		if ((bool)activeLoopDef)
		{
			Util.PlaySound(activeLoopDef.startSoundName, base.gameObject);
		}
	}

	private void OnDestroy()
	{
		if ((bool)activeLoopDef)
		{
			Util.PlaySound(activeLoopDef.stopSoundName, base.gameObject);
		}
		if ((bool)damageLoopDef)
		{
			Util.PlaySound(damageLoopDef.stopSoundName, base.gameObject);
		}
		if (stopSoundName != null)
		{
			Util.PlaySound(stopSoundName, base.gameObject);
		}
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			ServerFixedUpdate();
		}
		if (!damageLoopDef)
		{
			return;
		}
		bool flag = isLocalPlayerDamaged;
		isLocalPlayerDamaged = false;
		foreach (HurtBox cycleTarget in cycleTargets)
		{
			CharacterBody characterBody = null;
			if ((bool)cycleTarget && (bool)cycleTarget.healthComponent)
			{
				characterBody = cycleTarget.healthComponent.body;
			}
			if ((bool)characterBody && (characterBody.bodyFlags & CharacterBody.BodyFlags.OverheatImmune) != 0 && characterBody.hasEffectiveAuthority)
			{
				Vector3 position = base.transform.position;
				Vector3 corePosition = characterBody.corePosition;
				if (!Physics.Linecast(position, corePosition, out var _, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
				{
					isLocalPlayerDamaged = true;
				}
			}
		}
		if (isLocalPlayerDamaged && !flag)
		{
			Util.PlaySound(damageLoopDef.startSoundName, base.gameObject);
		}
		else if (!isLocalPlayerDamaged && flag)
		{
			Util.PlaySound(damageLoopDef.stopSoundName, base.gameObject);
		}
	}

	private void ServerFixedUpdate()
	{
		float num = Mathf.Clamp01(previousCycle.timeSince / cycleInterval);
		int num2 = ((num == 1f) ? cycleTargets.Count : Mathf.FloorToInt((float)cycleTargets.Count * num));
		Vector3 position = base.transform.position;
		while (cycleIndex < num2)
		{
			HurtBox hurtBox = cycleTargets[cycleIndex];
			if ((bool)hurtBox && (bool)hurtBox.healthComponent)
			{
				CharacterBody body = hurtBox.healthComponent.body;
				if ((body.bodyFlags & CharacterBody.BodyFlags.OverheatImmune) == 0)
				{
					Vector3 corePosition = body.corePosition;
					Ray ray = new Ray(position, corePosition - position);
					if (!Physics.Linecast(position, corePosition, out var hitInfo, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
					{
						float num3 = Mathf.Max(1f, hitInfo.distance);
						body.AddTimedBuff(buffDef, nearBuffDuration / num3);
						if ((bool)buffApplyEffect)
						{
							EffectData effectData = new EffectData
							{
								origin = corePosition,
								rotation = Util.QuaternionSafeLookRotation(-ray.direction),
								scale = body.bestFitRadius
							};
							effectData.SetHurtBoxReference(hurtBox);
							EffectManager.SpawnEffect(buffApplyEffect, effectData, transmit: true);
						}
						int num4 = body.GetBuffCount(buffDef) - minimumStacksBeforeApplyingBurns;
						if (num4 > 0)
						{
							InflictDotInfo dotInfo = default(InflictDotInfo);
							dotInfo.dotIndex = DotController.DotIndex.Burn;
							dotInfo.attackerObject = ownership.ownerObject;
							dotInfo.victimObject = body.gameObject;
							dotInfo.damageMultiplier = 1f;
							CharacterBody characterBody = ownership?.ownerObject?.GetComponent<CharacterBody>();
							if ((bool)characterBody && (bool)characterBody.inventory)
							{
								dotInfo.totalDamage = 0.5f * characterBody.damage * burnDuration * (float)num4;
								StrengthenBurnUtils.CheckDotForUpgrade(characterBody.inventory, ref dotInfo);
							}
							DotController.InflictDot(ref dotInfo);
						}
					}
				}
			}
			cycleIndex++;
		}
		if (previousCycle.timeSince >= cycleInterval)
		{
			previousCycle = Run.FixedTimeStamp.now;
			cycleIndex = 0;
			cycleTargets.Clear();
			SearchForTargets(cycleTargets);
		}
	}

	private void SearchForTargets(List<HurtBox> dest)
	{
		bullseyeSearch.searchOrigin = base.transform.position;
		bullseyeSearch.minAngleFilter = 0f;
		bullseyeSearch.maxAngleFilter = 180f;
		bullseyeSearch.maxDistanceFilter = maxDistance;
		bullseyeSearch.filterByDistinctEntity = true;
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
		bullseyeSearch.viewer = null;
		bullseyeSearch.RefreshCandidates();
		dest.AddRange(bullseyeSearch.GetResults());
	}
}
