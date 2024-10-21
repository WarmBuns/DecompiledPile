using System;
using System.Collections.Generic;
using System.Linq;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class FissureSlamCracksController : MonoBehaviour
{
	private class Meteor
	{
		public Vector3 impactPosition;

		public float startTime;

		public bool didTravelEffect;

		public bool valid = true;
	}

	private class MeteorWave
	{
		private readonly CharacterBody[] targets;

		private int currentStep;

		private float hitChance = 0.4f;

		private readonly Vector3 center;

		public float timer;

		private NodeGraphSpider nodeGraphSpider;

		public MeteorWave(CharacterBody[] targets, Vector3 center)
		{
			this.targets = new CharacterBody[targets.Length];
			targets.CopyTo(this.targets, 0);
			Util.ShuffleArray(targets);
			this.center = center;
			nodeGraphSpider = new NodeGraphSpider(SceneInfo.instance.groundNodes, HullMask.Human);
			nodeGraphSpider.AddNodeForNextStep(SceneInfo.instance.groundNodes.FindClosestNode(center, HullClassification.Human));
			int i = 0;
			for (int num = 10; i < num; i++)
			{
				if (!nodeGraphSpider.PerformStep())
				{
					break;
				}
			}
		}

		public Meteor GetNextMeteor()
		{
			if (currentStep >= targets.Length)
			{
				return null;
			}
			CharacterBody characterBody = targets[currentStep];
			Meteor meteor = new Meteor();
			if ((bool)characterBody && UnityEngine.Random.value < hitChance)
			{
				meteor.impactPosition = characterBody.corePosition;
				Vector3 origin = meteor.impactPosition + Vector3.up * 6f;
				Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
				onUnitSphere.y = -1f;
				if (Physics.Raycast(origin, onUnitSphere, out var hitInfo, 12f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
				{
					meteor.impactPosition = hitInfo.point;
				}
				else if (Physics.Raycast(meteor.impactPosition, Vector3.down, out hitInfo, float.PositiveInfinity, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
				{
					meteor.impactPosition = hitInfo.point;
				}
			}
			else if (nodeGraphSpider.collectedSteps.Count != 0)
			{
				int index = UnityEngine.Random.Range(0, nodeGraphSpider.collectedSteps.Count);
				SceneInfo.instance.groundNodes.GetNodePosition(nodeGraphSpider.collectedSteps[index].node, out meteor.impactPosition);
			}
			else
			{
				meteor.valid = false;
			}
			meteor.startTime = Run.instance.time;
			currentStep++;
			return meteor;
		}
	}

	public int waveCount;

	public float waveMinInterval;

	public float waveMaxInterval;

	public GameObject warningEffectPrefab;

	public GameObject travelEffectPrefab;

	public float travelEffectDuration;

	public GameObject impactEffectPrefab;

	public float impactDelay;

	public float blastDamageCoefficient;

	public float blastRadius;

	public float blastForce;

	[NonSerialized]
	public GameObject owner;

	[NonSerialized]
	public float ownerDamage;

	[NonSerialized]
	public bool isCrit;

	public UnityEvent onDestroyEvents;

	private List<Meteor> meteorList;

	private List<MeteorWave> waveList;

	private int wavesPerformed;

	private float waveTimer;

	private void Start()
	{
		if (NetworkServer.active)
		{
			meteorList = new List<Meteor>();
			waveList = new List<MeteorWave>();
		}
	}

	private void FixedUpdate()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		waveTimer -= Time.fixedDeltaTime;
		if (waveTimer <= 0f && wavesPerformed < waveCount)
		{
			wavesPerformed++;
			waveTimer = UnityEngine.Random.Range(waveMinInterval, waveMaxInterval);
			MeteorWave item = new MeteorWave(CharacterBody.readOnlyInstancesList.ToArray(), base.transform.position);
			waveList.Add(item);
		}
		for (int num = waveList.Count - 1; num >= 0; num--)
		{
			MeteorWave meteorWave = waveList[num];
			meteorWave.timer -= Time.fixedDeltaTime;
			if (meteorWave.timer <= 0f)
			{
				meteorWave.timer = UnityEngine.Random.Range(0.05f, 0.075f);
				Meteor nextMeteor = meteorWave.GetNextMeteor();
				if (nextMeteor == null)
				{
					waveList.RemoveAt(num);
				}
				else if (nextMeteor.valid)
				{
					meteorList.Add(nextMeteor);
					EffectManager.SpawnEffect(warningEffectPrefab, new EffectData
					{
						origin = nextMeteor.impactPosition,
						scale = blastRadius
					}, transmit: true);
				}
			}
		}
		float num2 = Run.instance.time - impactDelay;
		float num3 = num2 - travelEffectDuration;
		for (int num4 = meteorList.Count - 1; num4 >= 0; num4--)
		{
			Meteor meteor = meteorList[num4];
			if (meteor.startTime < num3 && !meteor.didTravelEffect)
			{
				DoMeteorEffect(meteor);
			}
			if (meteor.startTime < num2)
			{
				meteorList.RemoveAt(num4);
				DetonateMeteor(meteor);
			}
		}
		if (wavesPerformed == waveCount && meteorList.Count == 0)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void OnDestroy()
	{
		onDestroyEvents.Invoke();
	}

	private void DoMeteorEffect(Meteor meteor)
	{
		meteor.didTravelEffect = true;
		if ((bool)travelEffectPrefab)
		{
			EffectManager.SpawnEffect(travelEffectPrefab, new EffectData
			{
				origin = meteor.impactPosition,
				scale = blastRadius
			}, transmit: true);
		}
	}

	private void DetonateMeteor(Meteor meteor)
	{
		EffectData effectData = new EffectData
		{
			origin = meteor.impactPosition,
			scale = blastRadius
		};
		EffectManager.SpawnEffect(impactEffectPrefab, effectData, transmit: true);
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.inflictor = base.gameObject;
		blastAttack.baseDamage = blastDamageCoefficient * ownerDamage;
		blastAttack.baseForce = blastForce;
		blastAttack.attackerFiltering = AttackerFiltering.AlwaysHit;
		blastAttack.crit = isCrit;
		blastAttack.falloffModel = BlastAttack.FalloffModel.Linear;
		blastAttack.attacker = owner;
		blastAttack.bonusForce = Vector3.zero;
		blastAttack.damageColorIndex = DamageColorIndex.Item;
		blastAttack.position = meteor.impactPosition;
		blastAttack.procChainMask = default(ProcChainMask);
		blastAttack.procCoefficient = 1f;
		blastAttack.radius = blastRadius;
		blastAttack.Fire();
	}
}
