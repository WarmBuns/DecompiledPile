using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Merc;

public class Evis : BaseState
{
	private Transform modelTransform;

	public static GameObject blinkPrefab;

	public static float duration = 2f;

	public static float damageCoefficient;

	public static float damageFrequency;

	public static float procCoefficient;

	public static string beginSoundString;

	public static string endSoundString;

	public static float maxRadius;

	public static GameObject hitEffectPrefab;

	public static string slashSoundString;

	public static string impactSoundString;

	public static string dashSoundString;

	public static float slashPitch;

	public static float smallHopVelocity;

	public static float lingeringInvincibilityDuration;

	private Animator animator;

	private CharacterModel characterModel;

	private float stopwatch;

	private float attackStopwatch;

	private bool crit;

	private static float minimumDuration = 0.5f;

	private CameraTargetParams.AimRequest aimRequest;

	public override void OnEnter()
	{
		base.OnEnter();
		CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
		Util.PlayAttackSpeedSound(beginSoundString, base.gameObject, 1.2f);
		crit = Util.CheckRoll(critStat, base.characterBody.master);
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			animator = modelTransform.GetComponent<Animator>();
			characterModel = modelTransform.GetComponent<CharacterModel>();
		}
		if ((bool)characterModel)
		{
			characterModel.invisibilityCount++;
		}
		if ((bool)base.cameraTargetParams)
		{
			aimRequest = base.cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
		}
		if (NetworkServer.active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float deltaTime = GetDeltaTime();
		stopwatch += deltaTime;
		attackStopwatch += deltaTime;
		float num = 1f / damageFrequency / attackSpeedStat;
		if (attackStopwatch >= num)
		{
			attackStopwatch -= num;
			HurtBox hurtBox = SearchForTarget();
			if ((bool)hurtBox)
			{
				Util.PlayAttackSpeedSound(slashSoundString, base.gameObject, slashPitch);
				Util.PlaySound(dashSoundString, base.gameObject);
				Util.PlaySound(impactSoundString, base.gameObject);
				HurtBoxGroup hurtBoxGroup = hurtBox.hurtBoxGroup;
				HurtBox hurtBox2 = hurtBoxGroup.hurtBoxes[Random.Range(0, hurtBoxGroup.hurtBoxes.Length - 1)];
				if ((bool)hurtBox2)
				{
					Vector3 position = hurtBox2.transform.position;
					Vector2 normalized = Random.insideUnitCircle.normalized;
					EffectManager.SimpleImpactEffect(normal: new Vector3(normalized.x, 0f, normalized.y), effectPrefab: hitEffectPrefab, hitPos: position, transmit: false);
					Transform transform = hurtBox.hurtBoxGroup.transform;
					TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(transform.gameObject);
					temporaryOverlayInstance.duration = num;
					temporaryOverlayInstance.animateShaderAlpha = true;
					temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
					temporaryOverlayInstance.destroyComponentOnEnd = true;
					temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matMercEvisTarget");
					temporaryOverlayInstance.AddToCharacterModel(transform.GetComponent<CharacterModel>());
					if (NetworkServer.active)
					{
						DamageInfo damageInfo = new DamageInfo();
						damageInfo.damage = damageCoefficient * damageStat;
						damageInfo.attacker = base.gameObject;
						damageInfo.procCoefficient = procCoefficient;
						damageInfo.position = hurtBox2.transform.position;
						damageInfo.crit = crit;
						hurtBox2.healthComponent.TakeDamage(damageInfo);
						GlobalEventManager.instance.OnHitEnemy(damageInfo, hurtBox2.healthComponent.gameObject);
						GlobalEventManager.instance.OnHitAll(damageInfo, hurtBox2.healthComponent.gameObject);
					}
				}
			}
			else if (base.isAuthority && stopwatch > minimumDuration)
			{
				outer.SetNextStateToMain();
			}
		}
		if ((bool)base.characterMotor)
		{
			base.characterMotor.velocity = Vector3.zero;
		}
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private HurtBox SearchForTarget()
	{
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.searchOrigin = base.transform.position;
		bullseyeSearch.searchDirection = Random.onUnitSphere;
		bullseyeSearch.maxDistanceFilter = maxRadius;
		bullseyeSearch.teamMaskFilter = TeamMask.GetUnprotectedTeams(GetTeam());
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
		bullseyeSearch.RefreshCandidates();
		bullseyeSearch.FilterOutGameObject(base.gameObject);
		return bullseyeSearch.GetResults().FirstOrDefault();
	}

	private void CreateBlinkEffect(Vector3 origin)
	{
		EffectData effectData = new EffectData();
		effectData.rotation = Util.QuaternionSafeLookRotation(Vector3.up);
		effectData.origin = origin;
		EffectManager.SpawnEffect(blinkPrefab, effectData, transmit: false);
	}

	public override void OnExit()
	{
		Util.PlaySound(endSoundString, base.gameObject);
		CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
			temporaryOverlayInstance.duration = 0.6f;
			temporaryOverlayInstance.animateShaderAlpha = true;
			temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			temporaryOverlayInstance.destroyComponentOnEnd = true;
			temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matMercEvisTarget");
			temporaryOverlayInstance.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
			TemporaryOverlayInstance temporaryOverlayInstance2 = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
			temporaryOverlayInstance2.duration = 0.7f;
			temporaryOverlayInstance2.animateShaderAlpha = true;
			temporaryOverlayInstance2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			temporaryOverlayInstance2.destroyComponentOnEnd = true;
			temporaryOverlayInstance2.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashExpanded");
			temporaryOverlayInstance2.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
		}
		if ((bool)characterModel)
		{
			characterModel.invisibilityCount--;
		}
		aimRequest?.Dispose();
		if (NetworkServer.active)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
			base.characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, lingeringInvincibilityDuration);
		}
		Util.PlaySound(endSoundString, base.gameObject);
		SmallHop(base.characterMotor, smallHopVelocity);
		base.OnExit();
	}
}
