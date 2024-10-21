using System;
using System.Collections.Generic;
using RoR2;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.FalseSonBoss;

public class LunarRain : BaseState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public float warningDuration;

	[SerializeField]
	public float delayBetweenDrops = 0.2f;

	[SerializeField]
	public float lunarRadius;

	[SerializeField]
	public int totalLunarDrops = 7;

	[SerializeField]
	public float percentVarianceRotation = 0.25f;

	[SerializeField]
	public float percentVarianceInRadius = 0.2f;

	public static GameObject warningFXPrefab;

	public static GameObject rainCrashEffectPrefab;

	public static GameObject lunarRainPrefab;

	public static GameObject blastImpactEffectPrefab;

	[SerializeField]
	public GameObject blastEffectPrefab;

	[SerializeField]
	public float blastRadius = 2f;

	public static float blastProcCoefficient;

	public static float blastDamageCoefficient;

	public static float blastForce;

	public static Vector3 blastBonusForce;

	private NodeGraph nodeGraph;

	private List<NodeGraph.NodeIndex> nodesToTeleportTo;

	private List<NodeGraph.NodeIndex> fissureLocs;

	private Vector3 fissureLocation;

	private bool lunarRainStarted;

	private float lunarRainStartTime;

	private int totalWarned;

	private int totalFired;

	private int totalLunarDropsToFire;

	private float nextDropTime;

	public override void OnEnter()
	{
		base.OnEnter();
		new System.Random();
		nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
		fissureLocs = new List<NodeGraph.NodeIndex>();
		totalLunarDrops = ((totalLunarDrops > 0) ? totalLunarDrops : 7);
		float num = ((lunarRadius > 0f) ? lunarRadius : 30f);
		Transform transform = base.characterBody.transform;
		Vector3 position = transform.position;
		Vector3 angleOffset2 = Quaternion.AngleAxis(-90f, Vector3.up) * transform.forward;
		angleOffset2.y = 0f;
		NodeGraph.NodeIndex nodeIndex = nodeGraph.FindClosestNode(position, HullClassification.Human);
		for (int j = 0; j < totalLunarDrops; j++)
		{
			Vector3 position2 = GeneratePositionAroundCircle(j, totalLunarDrops, position, num * UnityEngine.Random.Range(0.7f, 1.3f), angleOffset2);
			NodeGraph.NodeIndex nodeIndex2 = SceneInfo.instance.groundNodes.FindClosestNode(position2, HullClassification.Human);
			if (!fissureLocs.Contains(nodeIndex2) && nodeIndex2 != nodeIndex)
			{
				fissureLocs.Add(nodeIndex2);
			}
			else
			{
				num *= 1.5f;
			}
		}
		Util.ShuffleList(fissureLocs);
		totalFired = 0;
		nextDropTime = base.fixedAge + delayBetweenDrops;
		totalLunarDropsToFire = Mathf.Min(totalLunarDrops, fissureLocs.Count);
		PlayAnimation("FullBody, Override", "LunarRain", "LunarRain.playbackRate", duration);
		Vector3 GeneratePositionAroundCircle(int i, int total, Vector3 center, float radius, Vector3 angleOffset)
		{
			float num2 = MathF.PI * 2f / (float)total;
			float f = num2 * (float)i + num2 * UnityEngine.Random.Range(1f - percentVarianceRotation, 1f + percentVarianceRotation);
			float x = Mathf.Cos(f);
			float z = Mathf.Sin(f);
			Vector3 vector = new Vector3(x, 0f, z);
			radius *= UnityEngine.Random.Range(1f - percentVarianceInRadius, 1f + percentVarianceInRadius);
			return vector * radius + center;
		}
	}

	private void SpawnRainWarning(NodeGraph.NodeIndex node)
	{
		nodeGraph.GetNodePosition(node, out fissureLocation);
		EffectManager.SpawnEffect(warningFXPrefab, new EffectData
		{
			origin = fissureLocation
		}, transmit: true);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (totalWarned < totalLunarDropsToFire)
		{
			SpawnRainWarning(fissureLocs[totalWarned++]);
			if (totalWarned >= totalLunarDropsToFire)
			{
				lunarRainStartTime = base.fixedAge + warningDuration + duration * 0.7f;
			}
			return;
		}
		if (!lunarRainStarted && base.fixedAge > lunarRainStartTime)
		{
			lunarRainStarted = true;
			DetonateAuthority();
		}
		if (!lunarRainStarted || base.fixedAge < nextDropTime)
		{
			return;
		}
		if (totalFired >= totalLunarDropsToFire)
		{
			outer.SetNextStateToMain();
			return;
		}
		FireRain(fissureLocs[totalFired++]);
		if (totalFired < totalLunarDropsToFire)
		{
			FireRain(fissureLocs[totalFired++]);
		}
	}

	private void FireRain(NodeGraph.NodeIndex node)
	{
		if (NetworkServer.active)
		{
			nodeGraph.GetNodePosition(node, out fissureLocation);
			nextDropTime = base.fixedAge + delayBetweenDrops;
			EffectManager.SpawnEffect(rainCrashEffectPrefab, new EffectData
			{
				origin = fissureLocation
			}, transmit: true);
			NetworkServer.Spawn(UnityEngine.Object.Instantiate(lunarRainPrefab, fissureLocation, Quaternion.identity));
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	protected BlastAttack.Result DetonateAuthority()
	{
		Vector3 position = FindModelChild("Root").transform.position;
		EffectManager.SpawnEffect(blastEffectPrefab, new EffectData
		{
			origin = position,
			scale = blastRadius
		}, transmit: true);
		return new BlastAttack
		{
			attacker = base.gameObject,
			baseDamage = damageStat * blastDamageCoefficient,
			baseForce = blastForce,
			bonusForce = blastBonusForce,
			crit = RollCrit(),
			falloffModel = BlastAttack.FalloffModel.None,
			procCoefficient = blastProcCoefficient,
			radius = blastRadius,
			position = base.gameObject.transform.position,
			attackerFiltering = AttackerFiltering.NeverHitSelf,
			impactEffect = EffectCatalog.FindEffectIndexFromPrefab(blastImpactEffectPrefab),
			teamIndex = base.teamComponent.teamIndex
		}.Fire();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
