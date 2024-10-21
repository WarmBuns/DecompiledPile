using System.Collections.Generic;
using System.Runtime.InteropServices;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(NetworkedBodyAttachment))]
public class SiphonNearbyController : NetworkBehaviour
{
	[SyncVar]
	public float radius;

	public float damagePerSecondCoefficient;

	[Min(float.Epsilon)]
	public float tickRate = 1f;

	[SyncVar]
	public int maxTargets;

	public TetherVfxOrigin tetherVfxOrigin;

	public GameObject activeVfx;

	protected new Transform transform;

	protected NetworkedBodyAttachment networkedBodyAttachment;

	protected SphereSearch sphereSearch;

	protected float timer;

	private bool isTetheredToAtLeastOneObject;

	public float Networkradius
	{
		get
		{
			return radius;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref radius, 1u);
		}
	}

	public int NetworkmaxTargets
	{
		get
		{
			return maxTargets;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref maxTargets, 2u);
		}
	}

	protected void Awake()
	{
		transform = base.transform;
		networkedBodyAttachment = GetComponent<NetworkedBodyAttachment>();
		sphereSearch = new SphereSearch();
		timer = 0f;
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

	protected void Tick()
	{
		if (!networkedBodyAttachment || !networkedBodyAttachment.attachedBody || !networkedBodyAttachment.attachedBodyObject)
		{
			return;
		}
		List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
		if (!networkedBodyAttachment.attachedBody.outOfCombat)
		{
			SearchForTargets(list);
		}
		float damage = damagePerSecondCoefficient * networkedBodyAttachment.attachedBody.damage / tickRate;
		float num = 0f;
		List<Transform> list2 = CollectionPool<Transform, List<Transform>>.RentCollection();
		for (int i = 0; i < list.Count; i++)
		{
			HurtBox hurtBox = list[i];
			if ((bool)hurtBox && (bool)hurtBox.healthComponent && hurtBox.healthComponent.alive)
			{
				HealthComponent healthComponent = hurtBox.healthComponent;
				if (hurtBox.healthComponent.body == networkedBodyAttachment.attachedBody)
				{
					continue;
				}
				Transform transform = healthComponent.body?.coreTransform ?? hurtBox.transform;
				list2.Add(transform);
				if (NetworkServer.active)
				{
					float combinedHealth = healthComponent.combinedHealth;
					DamageInfo damageInfo = new DamageInfo();
					damageInfo.attacker = networkedBodyAttachment.attachedBodyObject;
					damageInfo.inflictor = base.gameObject;
					damageInfo.position = transform.position;
					damageInfo.crit = networkedBodyAttachment.attachedBody.RollCrit();
					damageInfo.damage = damage;
					damageInfo.damageColorIndex = DamageColorIndex.Item;
					damageInfo.force = Vector3.zero;
					damageInfo.procCoefficient = 0f;
					damageInfo.damageType = DamageType.ClayGoo;
					damageInfo.procChainMask = default(ProcChainMask);
					healthComponent.TakeDamage(damageInfo);
					if (!damageInfo.rejected)
					{
						float num2 = Mathf.Max(healthComponent.combinedHealth, 0f);
						num += Mathf.Max(combinedHealth - num2);
					}
				}
			}
			if (list2.Count >= maxTargets)
			{
				break;
			}
		}
		isTetheredToAtLeastOneObject = (float)list2.Count > 0f;
		if (NetworkServer.active && num > 0f && (bool)networkedBodyAttachment.attachedBody.healthComponent)
		{
			networkedBodyAttachment.attachedBody.healthComponent.Heal(num, default(ProcChainMask));
		}
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
		sphereSearch.mask = LayerIndex.entityPrecise.mask;
		sphereSearch.origin = transform.position;
		sphereSearch.radius = radius + networkedBodyAttachment.attachedBody.radius;
		sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
		sphereSearch.RefreshCandidates();
		sphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(networkedBodyAttachment.attachedBody.teamComponent.teamIndex));
		sphereSearch.OrderCandidatesByDistance();
		sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
		sphereSearch.GetHurtBoxes(dest);
		sphereSearch.ClearCandidates();
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(radius);
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
			writer.Write(radius);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
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
			radius = reader.ReadSingle();
			maxTargets = (int)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			radius = reader.ReadSingle();
		}
		if (((uint)num & 2u) != 0)
		{
			maxTargets = (int)reader.ReadPackedUInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}
