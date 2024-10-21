using System.Collections.Generic;
using System.Runtime.InteropServices;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(NetworkedBodyAttachment))]
public class HealNearbyController : NetworkBehaviour
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
		SearchForTargets(list);
		float amount = damagePerSecondCoefficient * networkedBodyAttachment.attachedBody.damage / tickRate;
		List<Transform> list2 = CollectionPool<Transform, List<Transform>>.RentCollection();
		for (int i = 0; i < list.Count; i++)
		{
			HurtBox hurtBox = list[i];
			if ((bool)hurtBox && (bool)hurtBox.healthComponent && networkedBodyAttachment.attachedBody.healthComponent.alive && hurtBox.healthComponent.health < hurtBox.healthComponent.fullHealth && !hurtBox.healthComponent.body.HasBuff(DLC1Content.Buffs.EliteEarth))
			{
				HealthComponent healthComponent = hurtBox.healthComponent;
				if (hurtBox.healthComponent.body == networkedBodyAttachment.attachedBody)
				{
					continue;
				}
				Transform item = healthComponent.body?.coreTransform ?? hurtBox.transform;
				list2.Add(item);
				if (NetworkServer.active)
				{
					healthComponent.Heal(amount, default(ProcChainMask));
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
		TeamMask none = TeamMask.none;
		none.AddTeam(networkedBodyAttachment.attachedBody.teamComponent.teamIndex);
		sphereSearch.mask = LayerIndex.entityPrecise.mask;
		sphereSearch.origin = transform.position;
		sphereSearch.radius = radius + networkedBodyAttachment.attachedBody.radius;
		sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
		sphereSearch.RefreshCandidates();
		sphereSearch.FilterCandidatesByHurtBoxTeam(none);
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
