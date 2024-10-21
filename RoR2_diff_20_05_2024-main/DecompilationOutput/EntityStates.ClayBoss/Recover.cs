using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ClayBoss;

public class Recover : BaseState
{
	private enum SubState
	{
		Entry,
		Tethers
	}

	public static float duration = 15f;

	public static float maxTetherDistance = 40f;

	public static float tetherMulchDistance = 5f;

	public static float tetherMulchDamageScale = 2f;

	public static float tetherMulchTickIntervalScale = 0.5f;

	public static float damagePerSecond = 2f;

	public static float damageTickFrequency = 3f;

	public static float entryDuration = 1f;

	public static GameObject mulchEffectPrefab;

	public static string enterSoundString;

	public static string beginMulchSoundString;

	public static string stopMulchSoundString;

	private GameObject mulchEffect;

	private Transform muzzleTransform;

	private List<TarTetherController> tetherControllers;

	private List<GameObject> victimsList;

	private BullseyeSearch search;

	private float stopwatch;

	private uint soundID;

	private EffectManagerHelper _emh_mulchEffect;

	private SubState subState;

	private static int ChannelSiphonStateHash = Animator.StringToHash("ChannelSiphon");

	public override void Reset()
	{
		base.Reset();
		mulchEffect = null;
		muzzleTransform = null;
		if (tetherControllers != null)
		{
			tetherControllers.Clear();
		}
		if (victimsList != null)
		{
			victimsList.Clear();
		}
		if (search != null)
		{
			search.Reset();
		}
		soundID = 0u;
		subState = SubState.Entry;
		_emh_mulchEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		if (NetworkServer.active && (bool)base.characterBody)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.ArmorBoost);
		}
		if ((bool)base.modelLocator)
		{
			ChildLocator component = base.modelLocator.modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				muzzleTransform = component.FindChild("MuzzleMulch");
			}
		}
		subState = SubState.Entry;
		PlayCrossfade("Body", "PrepSiphon", "PrepSiphon.playbackRate", entryDuration, 0.1f);
		soundID = Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, attackSpeedStat);
	}

	private void FireTethers()
	{
		Vector3 position = muzzleTransform.position;
		float breakDistanceSqr = maxTetherDistance * maxTetherDistance;
		if (victimsList == null)
		{
			victimsList = new List<GameObject>();
		}
		tetherControllers = new List<TarTetherController>();
		if (search == null)
		{
			search = new BullseyeSearch();
		}
		search.searchOrigin = position;
		search.maxDistanceFilter = maxTetherDistance;
		search.teamMaskFilter = TeamMask.allButNeutral;
		search.sortMode = BullseyeSearch.SortMode.Distance;
		search.filterByLoS = true;
		search.searchDirection = Vector3.up;
		search.RefreshCandidates();
		search.FilterOutGameObject(base.gameObject);
		List<HurtBox> list = search.GetResults().ToList();
		for (int i = 0; i < list.Count; i++)
		{
			GameObject gameObject = list[i].healthComponent.gameObject;
			if ((bool)gameObject)
			{
				victimsList.Add(gameObject);
			}
		}
		float tickInterval = 1f / damageTickFrequency;
		float damageCoefficientPerTick = damagePerSecond / damageTickFrequency;
		float mulchDistanceSqr = tetherMulchDistance * tetherMulchDistance;
		GameObject original = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/TarTether");
		for (int j = 0; j < victimsList.Count; j++)
		{
			GameObject obj = Object.Instantiate(original, position, Quaternion.identity);
			TarTetherController component = obj.GetComponent<TarTetherController>();
			component.NetworkownerRoot = base.gameObject;
			component.NetworktargetRoot = victimsList[j];
			component.breakDistanceSqr = breakDistanceSqr;
			component.damageCoefficientPerTick = damageCoefficientPerTick;
			component.tickInterval = tickInterval;
			component.tickTimer = (float)j * 0.1f;
			component.mulchDistanceSqr = mulchDistanceSqr;
			component.mulchDamageScale = tetherMulchDamageScale;
			component.mulchTickIntervalScale = tetherMulchTickIntervalScale;
			tetherControllers.Add(component);
			NetworkServer.Spawn(obj);
		}
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

	public override void OnExit()
	{
		DestroyTethers();
		if ((bool)mulchEffect)
		{
			if ((bool)_emh_mulchEffect && _emh_mulchEffect.OwningPool != null)
			{
				_emh_mulchEffect.ReturnToPool();
				_emh_mulchEffect = null;
			}
			else
			{
				EntityState.Destroy(mulchEffect);
			}
		}
		AkSoundEngine.StopPlayingID(soundID);
		Util.PlaySound(stopMulchSoundString, base.gameObject);
		if (NetworkServer.active && (bool)base.characterBody)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.ArmorBoost);
		}
		base.OnExit();
	}

	private static void RemoveDeadTethersFromList(List<TarTetherController> tethersList)
	{
		for (int num = tethersList.Count - 1; num >= 0; num--)
		{
			if (!tethersList[num])
			{
				tethersList.RemoveAt(num);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (subState == SubState.Entry)
		{
			if (stopwatch >= entryDuration)
			{
				subState = SubState.Tethers;
				stopwatch = 0f;
				PlayAnimation("Body", ChannelSiphonStateHash);
				Util.PlaySound(beginMulchSoundString, base.gameObject);
				SpawnMulcherEffect();
				if (NetworkServer.active)
				{
					FireTethers();
				}
			}
		}
		else if (subState == SubState.Tethers && NetworkServer.active)
		{
			RemoveDeadTethersFromList(tetherControllers);
			if ((stopwatch >= duration || tetherControllers.Count == 0) && base.isAuthority)
			{
				outer.SetNextState(new RecoverExit());
			}
		}
	}

	private void SpawnMulcherEffect()
	{
		if (!EffectManager.ShouldUsePooledEffect(mulchEffectPrefab))
		{
			mulchEffect = Object.Instantiate(mulchEffectPrefab, muzzleTransform.position, Quaternion.identity);
		}
		else
		{
			_emh_mulchEffect = EffectManager.GetAndActivatePooledEffect(mulchEffectPrefab, muzzleTransform.position, Quaternion.identity);
			mulchEffect = _emh_mulchEffect.gameObject;
		}
		ChildLocator component = mulchEffect.gameObject.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			Transform transform = component.FindChild("AreaIndicator");
			if ((bool)transform)
			{
				transform.localScale = new Vector3(maxTetherDistance * 2f, maxTetherDistance * 2f, maxTetherDistance * 2f);
			}
		}
		mulchEffect.transform.parent = muzzleTransform;
	}
}
