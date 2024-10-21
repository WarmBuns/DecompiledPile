using System.Collections.Generic;
using System.Runtime.InteropServices;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class BloodSiphonNearbyController : NetworkBehaviour
{
	[Min(float.Epsilon)]
	public float minHealthFractionCoefficient;

	public float maxHealthFractionCoefficient;

	public float tickRate = 1f;

	public DamageTypeCombo damageType;

	[SyncVar]
	public int maxTargets;

	public TetherVfxOrigin tetherVfxOrigin;

	public GameObject activeVfx;

	protected new Transform transform;

	private HoldoutZoneController holdoutZone;

	protected SphereSearch sphereSearch;

	protected float timer;

	protected float currentRadius;

	private bool isTetheredToAtLeastOneObject;

	public int NetworkmaxTargets
	{
		get
		{
			return maxTargets;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref maxTargets, 1u);
		}
	}

	protected void Awake()
	{
		transform = base.transform;
		sphereSearch = new SphereSearch();
		timer = 0f;
		holdoutZone = GetComponentInParent<HoldoutZoneController>();
	}

	protected void FixedUpdate()
	{
		timer -= Time.fixedDeltaTime;
		if (timer <= 0f)
		{
			timer += 1f / tickRate;
			Tick();
		}
	}

	private void OnTransformParentChanged()
	{
		holdoutZone = GetComponentInParent<HoldoutZoneController>();
	}

	protected void Tick()
	{
		if ((bool)holdoutZone)
		{
			currentRadius = holdoutZone.currentRadius;
		}
		List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
		SearchForTargets(list);
		float num = minHealthFractionCoefficient;
		if ((bool)holdoutZone)
		{
			num = Mathf.Lerp(minHealthFractionCoefficient, maxHealthFractionCoefficient, holdoutZone.charge);
		}
		float num2 = num / tickRate;
		List<Transform> list2 = CollectionPool<Transform, List<Transform>>.RentCollection();
		for (int i = 0; i < list.Count; i++)
		{
			HurtBox hurtBox = list[i];
			if ((bool)hurtBox && (bool)hurtBox.healthComponent && hurtBox.healthComponent.alive)
			{
				HealthComponent healthComponent = hurtBox.healthComponent;
				Transform transform = healthComponent.body.coreTransform ?? hurtBox.transform;
				list2.Add(transform);
				if (NetworkServer.active)
				{
					DamageInfo damageInfo = new DamageInfo();
					damageInfo.attacker = null;
					damageInfo.inflictor = base.gameObject;
					damageInfo.position = transform.position;
					damageInfo.crit = false;
					damageInfo.damage = num2 * healthComponent.fullCombinedHealth;
					damageInfo.damageColorIndex = DamageColorIndex.Bleed;
					damageInfo.damageType = damageType;
					damageInfo.force = Vector3.zero;
					damageInfo.procCoefficient = 0f;
					damageInfo.procChainMask = default(ProcChainMask);
					hurtBox.healthComponent.TakeDamage(damageInfo);
				}
			}
			if (list2.Count >= maxTargets)
			{
				break;
			}
		}
		isTetheredToAtLeastOneObject = (float)list2.Count > 0f;
		if ((bool)tetherVfxOrigin)
		{
			tetherVfxOrigin.SetTetheredTransforms(list2);
		}
		if ((bool)activeVfx)
		{
			activeVfx.SetActive(isTetheredToAtLeastOneObject);
		}
		CollectionPool<Transform, List<Transform>>.ReturnCollection(list2);
		CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
	}

	protected void SearchForTargets(List<HurtBox> dest)
	{
		if (currentRadius > 0f)
		{
			TeamMask mask = default(TeamMask);
			mask.AddTeam(TeamIndex.Player);
			sphereSearch.mask = LayerIndex.entityPrecise.mask;
			sphereSearch.origin = transform.position;
			sphereSearch.radius = currentRadius;
			sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
			sphereSearch.RefreshCandidates();
			sphereSearch.FilterCandidatesByHurtBoxTeam(mask);
			sphereSearch.OrderCandidatesByDistance();
			sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
			sphereSearch.GetHurtBoxes(dest);
			sphereSearch.ClearCandidates();
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt32((uint)maxTargets);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)maxTargets);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			maxTargets = (int)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			maxTargets = (int)reader.ReadPackedUInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}
