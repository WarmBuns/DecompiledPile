using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace EntityStates.ChildMonster;

public class FrolicAway : BaseState
{
	public static GameObject projectilePrefab;

	public static GameObject effectPrefab;

	public static GameObject tpEffectPrefab;

	public static float baseDuration = 2f;

	public static float damageCoefficient = 1.2f;

	public static float force = 20f;

	public static string attackString;

	public static float tpDuration = 0.5f;

	public static float fireFrolicDuration = 0.3f;

	private float duration;

	private bool frolicFireFired;

	private bool tpFired;

	public override void OnEnter()
	{
		base.OnEnter();
		fireFrolicDuration += tpDuration;
		base.characterBody.GetComponent<ChildMonsterController>()?.RegisterTeleport();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture, Override", "FrolicEnter", "FrolicEnter.playbackRate", 1f);
		Util.PlaySound(attackString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > tpDuration && !tpFired)
		{
			FireTPEffect();
			tpFired = true;
			TeleportAway();
		}
		if (base.fixedAge > fireFrolicDuration && !frolicFireFired)
		{
			frolicFireFired = true;
			FireTPEffect();
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public void TeleportAway()
	{
		CharacterModel component = GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>();
		_ = base.characterBody.corePosition;
		NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
		List<NodeGraph.NodeIndex> source = nodeGraph.FindNodesInRange(base.characterBody.corePosition, 100f, 200f, HullMask.Human);
		nodeGraph.GetNodePosition(source.First(), out var position);
		TeleportHelper.TeleportBody(base.characterBody, position);
		TeleportOutController.AddTPOutEffect(component, 1f, 0f, 1f);
		if ((bool)tpEffectPrefab)
		{
			FireTPEffect();
		}
	}

	public void FireTPEffect()
	{
		Vector3 position = FindModelChild("Chest").transform.position;
		EffectManager.SpawnEffect(tpEffectPrefab, new EffectData
		{
			origin = position,
			scale = 1f
		}, transmit: true);
		Util.PlaySound("Play_child_attack2_reappear", base.gameObject);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
