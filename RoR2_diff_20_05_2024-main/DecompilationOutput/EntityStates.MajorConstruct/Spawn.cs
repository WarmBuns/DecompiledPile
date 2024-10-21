using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.MajorConstruct;

public class Spawn : BaseState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public GameObject muzzleEffectPrefab;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public int numPads;

	[SerializeField]
	public float padRingRadius;

	[SerializeField]
	public float maxRaycastDistance;

	[SerializeField]
	public GameObject padPrefab;

	[SerializeField]
	public float maxPadDistance;

	[SerializeField]
	public GameObject padEffectPrefab;

	[SerializeField]
	public bool alignPadsToNormal;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public bool depleteStocksPrimary;

	[SerializeField]
	public bool depleteStocksSecondary;

	[SerializeField]
	public bool depleteStocksUtility;

	[SerializeField]
	public bool depleteStocksSpecial;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		Util.PlaySound(enterSoundString, base.gameObject);
		MasterSpawnSlotController component = GetComponent<MasterSpawnSlotController>();
		if (!NetworkServer.active || !padPrefab || !component)
		{
			return;
		}
		RaycastHit hitInfo = default(RaycastHit);
		for (int i = 0; i < numPads; i++)
		{
			Quaternion quaternion = Quaternion.Euler(0f, 360f * ((float)i / (float)numPads), 0f);
			Vector3 vector = base.characterBody.corePosition + quaternion * Vector3.forward * padRingRadius;
			Vector3 origin = vector + Vector3.up * maxRaycastDistance;
			if (Physics.Raycast(vector, Vector3.up, out hitInfo, maxRaycastDistance, LayerIndex.world.mask))
			{
				origin = hitInfo.point;
			}
			if (Physics.Raycast(origin, Vector3.down, out hitInfo, maxRaycastDistance * 2f, LayerIndex.world.mask) && Vector3.Distance(base.characterBody.corePosition, hitInfo.point) < maxPadDistance)
			{
				if (alignPadsToNormal)
				{
					quaternion = Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * quaternion;
				}
				GameObject obj = Object.Instantiate(padPrefab, hitInfo.point, quaternion);
				NetworkedBodySpawnSlot component2 = obj.GetComponent<NetworkedBodySpawnSlot>();
				if ((bool)component2)
				{
					component.slots.Add(component2);
				}
				NetworkServer.Spawn(obj);
				if ((bool)padEffectPrefab)
				{
					EffectData effectData = new EffectData
					{
						origin = hitInfo.point,
						rotation = quaternion
					};
					EffectManager.SpawnEffect(padEffectPrefab, effectData, transmit: true);
				}
			}
		}
	}

	private void CheckForDepleteStocks(SkillSlot slot, bool deplete)
	{
		GenericSkill skill = base.skillLocator.GetSkill(slot);
		if (deplete && (bool)skill)
		{
			skill.RemoveAllStocks();
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

	public override void OnExit()
	{
		if (base.isAuthority)
		{
			CheckForDepleteStocks(SkillSlot.Primary, depleteStocksPrimary);
			CheckForDepleteStocks(SkillSlot.Secondary, depleteStocksSecondary);
			CheckForDepleteStocks(SkillSlot.Utility, depleteStocksUtility);
			CheckForDepleteStocks(SkillSlot.Special, depleteStocksSpecial);
		}
		base.OnExit();
	}
}
