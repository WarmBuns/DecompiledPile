using System.Collections.Generic;
using System.Collections.ObjectModel;
using HG;
using RoR2;
using RoR2.Audio;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidRaidCrab;

public class VacuumAttack : BaseVacuumAttackState
{
	public static AnimationCurve killRadiusCurve;

	public static AnimationCurve pullMagnitudeCurve;

	public static float losObstructionFactor = 0.5f;

	public static GameObject killSphereVfxPrefab;

	public static GameObject environmentVfxPrefab;

	public static LoopSoundDef loopSound;

	private CharacterLosTracker losTracker;

	private VFXHelper killSphereVfxHelper;

	private VFXHelper environmentVfxHelper;

	private SphereSearch killSearch;

	private float killRadius = 1f;

	private BodyIndex jointBodyIndex = BodyIndex.None;

	private LoopSoundManager.SoundLoopPtr loopPtr;

	public override void OnEnter()
	{
		base.OnEnter();
		losTracker = new CharacterLosTracker();
		losTracker.enabled = true;
		killSphereVfxHelper = VFXHelper.Rent();
		killSphereVfxHelper.vfxPrefabReference = killSphereVfxPrefab;
		killSphereVfxHelper.followedTransform = base.vacuumOrigin;
		killSphereVfxHelper.useFollowedTransformScale = false;
		killSphereVfxHelper.enabled = true;
		UpdateKillSphereVfx();
		environmentVfxHelper = VFXHelper.Rent();
		environmentVfxHelper.vfxPrefabReference = environmentVfxPrefab;
		environmentVfxHelper.followedTransform = base.vacuumOrigin;
		environmentVfxHelper.useFollowedTransformScale = false;
		environmentVfxHelper.enabled = true;
		loopPtr = LoopSoundManager.PlaySoundLoopLocal(base.gameObject, loopSound);
		if (NetworkServer.active)
		{
			killSearch = new SphereSearch();
		}
		jointBodyIndex = BodyCatalog.FindBodyIndex("VoidRaidCrabJointBody");
	}

	public override void OnExit()
	{
		killSphereVfxHelper = VFXHelper.Return(killSphereVfxHelper);
		environmentVfxHelper = VFXHelper.Return(environmentVfxHelper);
		losTracker.enabled = false;
		losTracker.Dispose();
		losTracker = null;
		LoopSoundManager.StopSoundLoopLocal(loopPtr);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float time = Mathf.Clamp01(base.fixedAge / base.duration);
		killRadius = killRadiusCurve.Evaluate(time);
		UpdateKillSphereVfx();
		Vector3 position = base.vacuumOrigin.position;
		losTracker.origin = position;
		losTracker.Step();
		ReadOnlyCollection<CharacterBody> readOnlyInstancesList = CharacterBody.readOnlyInstancesList;
		float num = pullMagnitudeCurve.Evaluate(time);
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			CharacterBody characterBody = readOnlyInstancesList[i];
			if ((object)characterBody == base.characterBody)
			{
				continue;
			}
			bool flag = losTracker.CheckBodyHasLos(characterBody);
			if (characterBody.hasEffectiveAuthority)
			{
				IDisplacementReceiver component = characterBody.GetComponent<IDisplacementReceiver>();
				if (component != null)
				{
					float num2 = (flag ? 1f : losObstructionFactor);
					component.AddDisplacement((position - characterBody.transform.position).normalized * (num * num2 * GetDeltaTime()));
				}
			}
		}
		if (!NetworkServer.active)
		{
			return;
		}
		List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
		List<HealthComponent> list2 = CollectionPool<HealthComponent, List<HealthComponent>>.RentCollection();
		try
		{
			killSearch.radius = killRadius;
			killSearch.origin = position;
			killSearch.mask = LayerIndex.entityPrecise.mask;
			killSearch.RefreshCandidates();
			killSearch.OrderCandidatesByDistance();
			killSearch.FilterCandidatesByDistinctHurtBoxEntities();
			killSearch.GetHurtBoxes(list);
			for (int j = 0; j < list.Count; j++)
			{
				HurtBox hurtBox = list[j];
				if ((bool)hurtBox.healthComponent)
				{
					list2.Add(hurtBox.healthComponent);
				}
			}
			for (int k = 0; k < list2.Count; k++)
			{
				HealthComponent healthComponent = list2[k];
				if ((object)base.healthComponent != healthComponent && healthComponent.body.bodyIndex != jointBodyIndex)
				{
					healthComponent.Suicide(base.gameObject, base.gameObject, DamageType.VoidDeath);
				}
			}
		}
		finally
		{
			list2 = CollectionPool<HealthComponent, List<HealthComponent>>.ReturnCollection(list2);
			list = CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
		}
	}

	private void UpdateKillSphereVfx()
	{
		if ((bool)killSphereVfxHelper.vfxInstanceTransform)
		{
			killSphereVfxHelper.vfxInstanceTransform.localScale = Vector3.one * killRadius;
		}
	}

	protected override void OnLifetimeExpiredAuthority()
	{
		outer.SetNextState(new VacuumWindDown());
	}
}
