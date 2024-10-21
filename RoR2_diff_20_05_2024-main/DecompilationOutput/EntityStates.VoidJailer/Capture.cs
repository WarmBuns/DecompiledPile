using System.Collections.Generic;
using System.Linq;
using EntityStates.VoidJailer.Weapon;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidJailer;

public class Capture : BaseState
{
	public static string animationLayerName;

	public static string animationStateName;

	public static string animationPlaybackRateName;

	public static GameObject chargeVfxPrefab;

	[SerializeField]
	public float duration;

	public static GameObject tetherPrefab;

	public static GameObject captureRangeEffect;

	public static float effectScaleCoefficient;

	public static float effectScaleAddition;

	public static string muzzleName;

	public static string tetherOriginName;

	public static float maxTetherDistance;

	public static float tetherReelSpeed;

	public static float damagePerSecond;

	public static float damageTickFrequency;

	public static BuffDef tetherDebuff;

	public static float innerRange;

	public static BuffDef innerRangeDebuff;

	public static float innerRangeDebuffDuration;

	public static GameObject innerRangeDebuffEffect;

	public static bool singleDurationReset;

	public static string captureLoopSoundString;

	private Transform muzzleTransform;

	private List<JailerTetherController> tetherControllers = new List<JailerTetherController>();

	private uint soundID;

	private bool initialized;

	private bool shouldModifyTetherDuration = true;

	private GameObject rangeEffect;

	private GameObject _chargeVfxInstance;

	public override void OnEnter()
	{
		muzzleTransform = FindModelChild(muzzleName);
		if (NetworkServer.active)
		{
			InitializeTethers();
		}
		if (NetworkServer.active && base.isAuthority && tetherControllers.Count == 0)
		{
			outer.SetNextState(new ExitCapture());
			return;
		}
		base.OnEnter();
		duration /= attackSpeedStat;
		FireTethers();
	}

	private void InitializeTethers()
	{
		Vector3 position = muzzleTransform.position;
		float breakDistanceSqr = maxTetherDistance * maxTetherDistance;
		tetherControllers = new List<JailerTetherController>();
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.searchOrigin = position;
		bullseyeSearch.maxDistanceFilter = maxTetherDistance;
		bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(base.teamComponent.teamIndex);
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
		bullseyeSearch.filterByLoS = true;
		bullseyeSearch.searchDirection = Vector3.up;
		bullseyeSearch.RefreshCandidates();
		bullseyeSearch.FilterOutGameObject(base.gameObject);
		List<HurtBox> list = bullseyeSearch.GetResults().ToList();
		for (int i = 0; i < list.Count; i++)
		{
			GameObject gameObject = list[i].healthComponent.gameObject;
			if ((bool)gameObject && !TargetIsTethered(gameObject.GetComponent<CharacterBody>()))
			{
				GameObject obj = Object.Instantiate(tetherPrefab, position, Quaternion.identity, muzzleTransform);
				JailerTetherController component = obj.GetComponent<JailerTetherController>();
				component.NetworkownerRoot = base.gameObject;
				component.Networkorigin = muzzleTransform.gameObject;
				component.NetworktargetRoot = gameObject;
				component.breakDistanceSqr = breakDistanceSqr;
				component.damageCoefficientPerTick = damagePerSecond / damageTickFrequency;
				component.tickInterval = 1f / damageTickFrequency;
				component.tickTimer = (float)i * 0.1f;
				component.NetworkreelSpeed = tetherReelSpeed;
				component.SetTetheredBuff(tetherDebuff);
				tetherControllers.Add(component);
				NetworkServer.Spawn(obj);
			}
		}
		initialized = true;
	}

	private static bool TargetIsTethered(CharacterBody characterBody)
	{
		if (characterBody != null)
		{
			return characterBody.HasBuff(tetherDebuff);
		}
		return false;
	}

	private void FireTethers()
	{
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateName, duration);
		soundID = Util.PlayAttackSpeedSound(captureLoopSoundString, base.gameObject, attackSpeedStat);
		if (!muzzleTransform)
		{
			return;
		}
		if ((bool)captureRangeEffect)
		{
			rangeEffect = Object.Instantiate(captureRangeEffect, base.characterBody.transform);
			rangeEffect.transform.localScale = effectScaleCoefficient * (maxTetherDistance + effectScaleAddition) * Vector3.one;
			ScaleParticleSystemDuration component = rangeEffect.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = duration;
			}
		}
		if ((bool)chargeVfxPrefab)
		{
			_chargeVfxInstance = Object.Instantiate(chargeVfxPrefab, muzzleTransform.position, muzzleTransform.rotation, muzzleTransform);
			ScaleParticleSystemDuration component2 = _chargeVfxInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component2)
			{
				component2.newDuration = duration;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (initialized)
		{
			UpdateTethers(base.gameObject.transform.position);
			if (tetherControllers.Count == 0 || base.fixedAge >= duration)
			{
				outer.SetNextState(new ExitCapture());
			}
		}
	}

	private void UpdateTethers(Vector3 origin)
	{
		for (int num = tetherControllers.Count - 1; num >= 0; num--)
		{
			JailerTetherController jailerTetherController = tetherControllers[num];
			if (!jailerTetherController)
			{
				tetherControllers.RemoveAt(num);
			}
			else
			{
				CharacterBody targetBody = jailerTetherController.GetTargetBody();
				if (!(targetBody == null) && !targetBody.HasBuff(innerRangeDebuff) && Vector3.Distance(origin, targetBody.transform.position) < innerRange)
				{
					float num2 = duration - base.fixedAge;
					targetBody.AddTimedBuff(innerRangeDebuff, innerRangeDebuffDuration);
					if (shouldModifyTetherDuration)
					{
						duration = ((num2 > innerRangeDebuffDuration) ? innerRangeDebuffDuration : (innerRangeDebuffDuration + base.fixedAge));
						PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateName, innerRangeDebuffDuration);
						shouldModifyTetherDuration = !singleDurationReset;
						if ((bool)_chargeVfxInstance)
						{
							ScaleParticleSystemDuration component = _chargeVfxInstance.GetComponent<ScaleParticleSystemDuration>();
							if ((bool)component)
							{
								component.newDuration = duration;
							}
						}
					}
					jailerTetherController.NetworkreelSpeed = 0f;
				}
			}
		}
	}

	public override void OnExit()
	{
		DestroyTethers();
		if ((bool)rangeEffect)
		{
			EntityState.Destroy(rangeEffect);
		}
		if ((bool)_chargeVfxInstance)
		{
			EntityState.Destroy(_chargeVfxInstance);
		}
		AkSoundEngine.StopPlayingID(soundID);
		base.OnExit();
	}

	private void DestroyTethers()
	{
		if (tetherControllers == null)
		{
			return;
		}
		for (int num = tetherControllers.Count - 1; num >= 0; num--)
		{
			if ((bool)tetherControllers[num])
			{
				EntityState.Destroy(tetherControllers[num].gameObject);
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
