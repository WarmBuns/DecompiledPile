using System.Collections.Generic;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class OrbFireZone : MonoBehaviour
{
	public float baseDamage;

	public float procCoefficient;

	public float orbRemoveFromBottomOfListFrequency;

	public float orbResetListFrequency;

	private List<Collider> previousColliderList = new List<Collider>();

	private float resetStopwatch;

	private float removeFromBottomOfListStopwatch;

	private void Awake()
	{
	}

	private void FixedUpdate()
	{
		if (previousColliderList.Count > 0)
		{
			resetStopwatch += Time.fixedDeltaTime;
			removeFromBottomOfListStopwatch += Time.fixedDeltaTime;
			if (removeFromBottomOfListStopwatch > 1f / orbRemoveFromBottomOfListFrequency)
			{
				removeFromBottomOfListStopwatch -= 1f / orbRemoveFromBottomOfListFrequency;
				previousColliderList.RemoveAt(previousColliderList.Count - 1);
			}
			if (resetStopwatch > 1f / orbResetListFrequency)
			{
				resetStopwatch -= 1f / orbResetListFrequency;
				previousColliderList.Clear();
			}
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (!NetworkServer.active || previousColliderList.Contains(other))
		{
			return;
		}
		previousColliderList.Add(other);
		CharacterBody component = other.GetComponent<CharacterBody>();
		if ((bool)component && (bool)component.mainHurtBox)
		{
			DamageOrb damageOrb = new DamageOrb();
			damageOrb.attacker = null;
			damageOrb.damageOrbType = DamageOrb.DamageOrbType.ClayGooOrb;
			damageOrb.procCoefficient = procCoefficient;
			damageOrb.damageValue = baseDamage * Run.instance.teamlessDamageCoefficient;
			damageOrb.target = component.mainHurtBox;
			damageOrb.teamIndex = TeamIndex.None;
			if (Physics.Raycast(damageOrb.target.transform.position + Random.insideUnitSphere * 3f, Vector3.down, out var hitInfo, 1000f, LayerIndex.world.mask))
			{
				damageOrb.origin = hitInfo.point;
				OrbManager.instance.AddOrb(damageOrb);
			}
		}
	}
}
