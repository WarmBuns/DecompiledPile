using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using UnityEngine;

namespace EntityStates.ChildMonster;

public class Frolic : BaseState
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

	public static float frolicCooldownDuration = 18f;

	private float duration;

	private bool frolicFireFired;

	private bool tpFired;

	private bool cantFrolic;

	public override void OnEnter()
	{
		base.OnEnter();
		ChildMonsterController component = base.characterBody.GetComponent<ChildMonsterController>();
		cantFrolic = !component.CheckTeleportAvailable();
		if (!cantFrolic)
		{
			component.RegisterTeleport();
			fireFrolicDuration += tpDuration;
			duration = baseDuration / attackSpeedStat;
			PlayAnimation("Gesture, Override", "FrolicEnter", "FrolicEnter.playbackRate", 1f);
			Util.PlaySound(attackString, base.gameObject);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (cantFrolic)
		{
			outer.SetNextStateToMain();
			return;
		}
		if (base.fixedAge > tpDuration && !tpFired)
		{
			FireTPEffect();
			tpFired = true;
			TeleportAroundPlayer();
		}
		if (base.fixedAge > fireFrolicDuration && !frolicFireFired)
		{
			frolicFireFired = true;
			FireTPEffect();
			Transform transform = base.characterBody.master.GetComponent<BaseAI>().currentEnemy.characterBody.transform;
			base.characterBody.transform.LookAt(transform.position);
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	public void TeleportAroundPlayer()
	{
		GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>();
		_ = base.characterBody.corePosition;
		NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
		Vector3 position = base.characterBody.master.GetComponent<BaseAI>().currentEnemy.characterBody.coreTransform.position;
		List<NodeGraph.NodeIndex> list = nodeGraph.FindNodesInRange(position, 25f, 37f, HullMask.Human);
		Vector3 position2 = default(Vector3);
		bool flag = false;
		int num = 35;
		while (!flag)
		{
			NodeGraph.NodeIndex nodeIndex = list.ElementAt(Random.Range(1, list.Count));
			nodeGraph.GetNodePosition(nodeIndex, out position2);
			float num2 = Vector3.Distance(base.characterBody.coreTransform.position, position2);
			num--;
			if (num2 > 35f || num < 0)
			{
				flag = true;
			}
		}
		if (num < 0)
		{
			Debug.LogWarning("Child.Frolic state entered a loop where it ran more than 35 times without getting out - check what it's doing");
		}
		position2 += Vector3.up * 1.5f;
		TeleportHelper.TeleportBody(base.characterBody, position2);
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
}
