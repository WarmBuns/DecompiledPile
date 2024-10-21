using System.Collections.Generic;
using RoR2;
using RoR2.Navigation;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.FalseSonBoss;

public class FissureSlam : BaseCharacterMain
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public int totalFissures = 5;

	[SerializeField]
	public float delayBetweenFissures = 0.001f;

	public float charge;

	public static float minimumDuration;

	public static float blastRadius;

	public static float blastProcCoefficient;

	public static float blastDamageCoefficient;

	public static float blastForce;

	public static string enterSoundString;

	public static Vector3 blastBonusForce;

	public static GameObject blastImpactEffectPrefab;

	public static GameObject blastEffectPrefab;

	public static GameObject fistEffectPrefab;

	public static GameObject swingEffectPrefab;

	public static GameObject fissureSlamObject;

	private GameObject leftFistEffectInstance;

	private GameObject rightFistEffectInstance;

	private GameObject swingEffectInstance;

	private bool detonateNextFrame;

	private bool amServer;

	private bool slamComplete;

	private float fissureExplosionDelay = 0.7f;

	private float fissureExplosionTimer;

	private NodeGraph nodeGraph;

	private List<NodeGraph.NodeIndex> possibleNodesToTarget;

	private List<GameObject> playersToTarget;

	private List<Vector3> fissurePositions;

	private int totalFissuresFired;

	private float timeToGenerateNextFissure;

	public static GameObject crackWarningEffectPrefab;

	public static GameObject fissureExplosionEffectPrefab;

	public static GameObject pillarProjectilePrefab;

	private bool stateAborted;

	public override void OnEnter()
	{
		base.OnEnter();
		amServer = NetworkServer.active;
		baseDuration /= attackSpeedStat;
		PlayAnimation("FullBody, Override", "ChargeSwing", "ChargeSwing.playbackRate", baseDuration);
		Util.PlaySound(enterSoundString, base.gameObject);
		leftFistEffectInstance = Object.Instantiate(fistEffectPrefab, FindModelChild("HandR"));
		rightFistEffectInstance = Object.Instantiate(fistEffectPrefab, FindModelChild("HandL"));
		swingEffectInstance = Object.Instantiate(swingEffectPrefab, FindModelChild("OverHeadSwingPoint"));
		nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
		possibleNodesToTarget = nodeGraph.FindNodesInRange(base.characterBody.corePosition, 0f, 30f, HullMask.Human);
		playersToTarget = new List<GameObject>();
		fissurePositions = new List<Vector3>();
		totalFissuresFired = 0;
		timeToGenerateNextFissure = 0f;
		if (!amServer)
		{
			return;
		}
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			if (!(instance == null) && !(instance.master.GetBodyObject() == null))
			{
				playersToTarget.Add(instance.master.GetBodyObject());
			}
		}
		Util.ShuffleList(playersToTarget);
		RaycastToFindGroundPointOfNextPlayer();
	}

	private void RaycastToFindGroundPointOfNextPlayer()
	{
		if (fissurePositions.Count >= totalFissures)
		{
			return;
		}
		if (fissurePositions.Count < playersToTarget.Count && playersToTarget.Count > 0)
		{
			if (Physics.Raycast(new Ray(playersToTarget[fissurePositions.Count].transform.position + Vector3.up * 1f, Vector3.down), out var hitInfo, 200f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
			{
				fissurePositions.Add(hitInfo.point);
			}
			else
			{
				if (possibleNodesToTarget.Count < 1)
				{
					return;
				}
				int index = Random.Range(0, possibleNodesToTarget.Count);
				nodeGraph.GetNodePosition(possibleNodesToTarget[index], out var position);
				fissurePositions.Add(position);
				possibleNodesToTarget.RemoveAt(index);
			}
		}
		if (fissurePositions.Count >= playersToTarget.Count)
		{
			FillRemainingFissurePositions();
		}
	}

	private void FillRemainingFissurePositions()
	{
		if (fissurePositions.Count >= totalFissures)
		{
			return;
		}
		int num = Mathf.Max(totalFissures - fissurePositions.Count, 0);
		for (int i = 0; i < num; i++)
		{
			if (possibleNodesToTarget.Count < 1)
			{
				break;
			}
			int index = Random.Range(0, possibleNodesToTarget.Count);
			nodeGraph.GetNodePosition(possibleNodesToTarget[index], out var position);
			fissurePositions.Add(position);
			possibleNodesToTarget.RemoveAt(index);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (stateAborted)
		{
			return;
		}
		RaycastToFindGroundPointOfNextPlayer();
		if (!slamComplete && base.fixedAge >= baseDuration - baseDuration * 0.8f && amServer)
		{
			DetonateAuthority();
			slamComplete = true;
			FillRemainingFissurePositions();
			int num = Mathf.Min(totalFissures, fissurePositions.Count);
			for (int i = 0; i < num; i++)
			{
				EffectManager.SpawnEffect(crackWarningEffectPrefab, new EffectData
				{
					origin = fissurePositions[i] + Vector3.up * 0.01f
				}, transmit: true);
			}
		}
		if (!slamComplete || !amServer)
		{
			return;
		}
		fissureExplosionTimer += Time.deltaTime;
		if (fissureExplosionTimer > fissureExplosionDelay)
		{
			int num2 = Mathf.Min(totalFissures, fissurePositions.Count);
			for (int j = 0; j < num2; j++)
			{
				ProjectileManager.instance.FireProjectile(pillarProjectilePrefab, fissurePositions[j], Quaternion.identity, base.gameObject, base.characterBody.damage * (blastDamageCoefficient * 0.2f), 0f, Util.CheckRoll(base.characterBody.crit, base.characterBody.master));
			}
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if (!stateAborted)
		{
			EntityState.Destroy(leftFistEffectInstance);
			EntityState.Destroy(rightFistEffectInstance);
			base.OnExit();
		}
	}

	protected BlastAttack.Result DetonateAuthority()
	{
		Vector3 position = FindModelChild("ClubExplosionPoint").transform.position;
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
			radius = blastRadius + 3f,
			position = position,
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
