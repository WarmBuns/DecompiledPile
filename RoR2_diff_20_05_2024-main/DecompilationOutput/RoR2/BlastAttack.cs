using System;
using HG;
using RoR2.Networking;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class BlastAttack
{
	public enum FalloffModel
	{
		None,
		Linear,
		SweetSpot
	}

	public enum LoSType
	{
		None,
		NearestHit
	}

	public struct HitPoint
	{
		public HurtBox hurtBox;

		public Vector3 hitPosition;

		public Vector3 hitNormal;

		public float distanceSqr;
	}

	public struct Result
	{
		public int hitCount;

		public HitPoint[] hitPoints;
	}

	private struct BlastAttackDamageInfo
	{
		public GameObject attacker;

		public GameObject inflictor;

		public bool crit;

		public float damage;

		public DamageColorIndex damageColorIndex;

		public HurtBox.DamageModifier damageModifier;

		public DamageTypeCombo damageType;

		public Vector3 force;

		public Vector3 position;

		public ProcChainMask procChainMask;

		public float procCoefficient;

		public HealthComponent hitHealthComponent;

		public bool canRejectForce;

		public void Write(NetworkWriter writer)
		{
			writer.Write(attacker);
			writer.Write(inflictor);
			writer.Write(crit);
			writer.Write(damage);
			writer.Write(damageColorIndex);
			writer.Write((byte)damageModifier);
			writer.WriteDamageType(damageType);
			writer.Write(force);
			writer.Write(position);
			writer.Write(procChainMask);
			writer.Write(procCoefficient);
			writer.Write(hitHealthComponent.netId);
			writer.Write(canRejectForce);
		}

		public void Read(NetworkReader reader)
		{
			attacker = reader.ReadGameObject();
			inflictor = reader.ReadGameObject();
			crit = reader.ReadBoolean();
			damage = reader.ReadSingle();
			damageColorIndex = reader.ReadDamageColorIndex();
			damageModifier = (HurtBox.DamageModifier)reader.ReadByte();
			damageType = reader.ReadDamageType();
			force = reader.ReadVector3();
			position = reader.ReadVector3();
			procChainMask = reader.ReadProcChainMask();
			procCoefficient = reader.ReadSingle();
			GameObject gameObject = reader.ReadGameObject();
			hitHealthComponent = (gameObject ? gameObject.GetComponent<HealthComponent>() : null);
			canRejectForce = reader.ReadBoolean();
		}
	}

	public GameObject attacker;

	public GameObject inflictor;

	public TeamIndex teamIndex;

	public AttackerFiltering attackerFiltering;

	public Vector3 position;

	public float radius;

	public FalloffModel falloffModel = FalloffModel.Linear;

	public float baseDamage;

	public float baseForce;

	public Vector3 bonusForce;

	public bool crit;

	public DamageTypeCombo damageType = DamageType.Generic;

	public DamageColorIndex damageColorIndex;

	public LoSType losType;

	public EffectIndex impactEffect = EffectIndex.Invalid;

	public bool canRejectForce = true;

	public ProcChainMask procChainMask;

	public float procCoefficient = 1f;

	private static readonly int initialBufferSize = 256;

	private static HitPoint[] hitPointsBuffer = new HitPoint[initialBufferSize];

	private static int[] hitOrderBuffer = new int[initialBufferSize];

	private static HealthComponent[] encounteredHealthComponentsBuffer = new HealthComponent[initialBufferSize];

	public static event Action<HitPoint[], GameObject> BlastAttackMultiHit;

	public Result Fire()
	{
		HitPoint[] array = CollectHits();
		HandleHits(array);
		Result result = default(Result);
		result.hitCount = array.Length;
		result.hitPoints = array;
		return result;
	}

	[NetworkMessageHandler(msgType = 75, client = false, server = true)]
	private static void HandleReportBlastAttackDamage(NetworkMessage netMsg)
	{
		NetworkReader reader = netMsg.reader;
		BlastAttackDamageInfo blastAttackDamageInfo = default(BlastAttackDamageInfo);
		blastAttackDamageInfo.Read(reader);
		PerformDamageServer(in blastAttackDamageInfo);
	}

	public void Reset()
	{
		attacker = null;
		inflictor = null;
		teamIndex = TeamIndex.Neutral;
		position = Vector3.zero;
		radius = 0f;
		falloffModel = FalloffModel.Linear;
		baseDamage = 0f;
		baseForce = 0f;
		bonusForce = Vector3.zero;
		crit = false;
		damageType = DamageType.Generic;
		damageColorIndex = DamageColorIndex.Default;
		procChainMask.mask = 0u;
		procCoefficient = 1f;
		canRejectForce = true;
	}

	private HitPoint[] CollectHits()
	{
		Vector3 vector = position;
		Collider[] array = Physics.OverlapSphere(vector, radius, LayerIndex.entityPrecise.mask);
		int num = array.Length;
		int count = 0;
		int encounteredHealthComponentsLength = 0;
		int hitOrderBufferLength = 0;
		ArrayUtils.EnsureCapacity(ref hitPointsBuffer, num);
		ArrayUtils.EnsureCapacity(ref hitOrderBuffer, num);
		ArrayUtils.EnsureCapacity(ref encounteredHealthComponentsBuffer, num);
		for (int i = 0; i < num; i++)
		{
			Collider collider = array[i];
			HurtBox component = collider.GetComponent<HurtBox>();
			if (!component)
			{
				continue;
			}
			HealthComponent healthComponent2 = component.healthComponent;
			if (!healthComponent2)
			{
				continue;
			}
			bool flag = true;
			switch (attackerFiltering)
			{
			case AttackerFiltering.Default:
				flag = true;
				break;
			case AttackerFiltering.AlwaysHitSelf:
				flag = true;
				if (healthComponent2.gameObject == attacker)
				{
					flag = false;
				}
				break;
			case AttackerFiltering.AlwaysHit:
				flag = false;
				break;
			case AttackerFiltering.NeverHitSelf:
				flag = true;
				if (healthComponent2.gameObject == attacker)
				{
					continue;
				}
				break;
			}
			if (!flag || FriendlyFireManager.ShouldSplashHitProceed(healthComponent2, teamIndex))
			{
				Vector3 vector2 = collider.transform.position;
				Vector3 hitNormal = vector - vector2;
				float sqrMagnitude = hitNormal.sqrMagnitude;
				hitPointsBuffer[count++] = new HitPoint
				{
					hurtBox = component,
					hitPosition = vector2,
					hitNormal = hitNormal,
					distanceSqr = sqrMagnitude
				};
			}
		}
		if (true)
		{
			for (int j = 0; j < count; j++)
			{
				ref HitPoint reference = ref hitPointsBuffer[j];
				if ((object)reference.hurtBox != null && reference.distanceSqr > 0f && reference.hurtBox.collider.Raycast(new Ray(vector, -reference.hitNormal), out var hitInfo, radius))
				{
					reference.distanceSqr = (vector - hitInfo.point).sqrMagnitude;
					reference.hitPosition = hitInfo.point;
					reference.hitNormal = hitInfo.normal;
				}
			}
		}
		hitOrderBufferLength = count;
		for (int k = 0; k < count; k++)
		{
			hitOrderBuffer[k] = k;
		}
		for (int l = 1; l < count; l++)
		{
			int num2 = l;
			while (num2 > 0 && hitPointsBuffer[hitOrderBuffer[num2 - 1]].distanceSqr > hitPointsBuffer[hitOrderBuffer[num2]].distanceSqr)
			{
				int num3 = hitOrderBuffer[num2 - 1];
				hitOrderBuffer[num2 - 1] = hitOrderBuffer[num2];
				hitOrderBuffer[num2] = num3;
				num2--;
			}
		}
		bool flag2 = losType == LoSType.None || losType == LoSType.NearestHit;
		for (int m = 0; m < hitOrderBufferLength; m++)
		{
			int num4 = hitOrderBuffer[m];
			ref HitPoint reference2 = ref hitPointsBuffer[num4];
			HealthComponent healthComponent3 = reference2.hurtBox.healthComponent;
			if (!EntityIsMarkedEncountered(healthComponent3))
			{
				MarkEntityAsEncountered(healthComponent3);
			}
			else if (flag2)
			{
				reference2.hurtBox = null;
			}
		}
		ClearEncounteredEntities();
		CondenseHitOrderBuffer();
		LoSType loSType = losType;
		if (loSType != 0 && loSType == LoSType.NearestHit)
		{
			NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(hitOrderBufferLength, Allocator.TempJob);
			NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(hitOrderBufferLength, Allocator.TempJob);
			int n = 0;
			int num5 = 0;
			for (; n < hitOrderBufferLength; n++)
			{
				int num6 = hitOrderBuffer[n];
				ref HitPoint reference3 = ref hitPointsBuffer[num6];
				commands[num5++] = new RaycastCommand(vector, reference3.hitPosition - vector, Mathf.Sqrt(reference3.distanceSqr), LayerIndex.world.mask);
			}
			bool queriesHitTriggers = Physics.queriesHitTriggers;
			Physics.queriesHitTriggers = true;
			RaycastCommand.ScheduleBatch(commands, results, 1).Complete();
			Physics.queriesHitTriggers = queriesHitTriggers;
			num = 0;
			int num2 = 0;
			for (; num < hitOrderBufferLength; num++)
			{
				int num3 = hitOrderBuffer[num];
				ref HitPoint reference4 = ref hitPointsBuffer[num3];
				if ((object)reference4.hurtBox != null && (bool)results[num2++].collider)
				{
					reference4.hurtBox = null;
				}
			}
			results.Dispose();
			commands.Dispose();
			CondenseHitOrderBuffer();
		}
		HitPoint[] array2 = new HitPoint[hitOrderBufferLength];
		for (num = 0; num < hitOrderBufferLength; num++)
		{
			int num2 = hitOrderBuffer[num];
			array2[num] = hitPointsBuffer[num2];
		}
		ArrayUtils.Clear(hitPointsBuffer, ref count);
		ClearEncounteredEntities();
		return array2;
		void ClearEncounteredEntities()
		{
			Array.Clear(encounteredHealthComponentsBuffer, 0, encounteredHealthComponentsLength);
			encounteredHealthComponentsLength = 0;
		}
		void CondenseHitOrderBuffer()
		{
			for (num = 0; num < hitOrderBufferLength; num++)
			{
				int num2 = 0;
				for (num = num; num < hitOrderBufferLength; num++)
				{
					num2 = hitOrderBuffer[num];
					if ((object)hitPointsBuffer[num2].hurtBox != null)
					{
						break;
					}
					num2++;
				}
				if (num2 > 0)
				{
					ArrayUtils.ArrayRemoveAt(hitOrderBuffer, ref hitOrderBufferLength, num, num2);
				}
			}
		}
		bool EntityIsMarkedEncountered(HealthComponent healthComponent)
		{
			for (num = 0; num < encounteredHealthComponentsLength; num++)
			{
				if ((object)encounteredHealthComponentsBuffer[num] == healthComponent)
				{
					return true;
				}
			}
			return false;
		}
		void MarkEntityAsEncountered(HealthComponent healthComponent)
		{
			encounteredHealthComponentsBuffer[encounteredHealthComponentsLength++] = healthComponent;
		}
	}

	private void HandleHits(HitPoint[] hitPoints)
	{
		Vector3 vector = position;
		for (int i = 0; i < hitPoints.Length; i++)
		{
			HitPoint hitPoint = hitPoints[i];
			float num = Mathf.Sqrt(hitPoint.distanceSqr);
			float num2 = 0f;
			Vector3 vector2 = ((num > 0f) ? ((hitPoint.hitPosition - vector) / num) : Vector3.zero);
			HealthComponent healthComponent = (hitPoint.hurtBox ? hitPoint.hurtBox.healthComponent : null);
			if ((bool)healthComponent)
			{
				switch (falloffModel)
				{
				case FalloffModel.None:
					num2 = 1f;
					break;
				case FalloffModel.Linear:
					num2 = 1f - Mathf.Clamp01(num / radius);
					break;
				case FalloffModel.SweetSpot:
					num2 = 1f - ((num > radius / 2f) ? 0.75f : 0f);
					break;
				}
				BlastAttackDamageInfo blastAttackDamageInfo = default(BlastAttackDamageInfo);
				blastAttackDamageInfo.attacker = attacker;
				blastAttackDamageInfo.inflictor = inflictor;
				blastAttackDamageInfo.crit = crit;
				blastAttackDamageInfo.damage = baseDamage * num2;
				blastAttackDamageInfo.damageColorIndex = damageColorIndex;
				blastAttackDamageInfo.damageModifier = hitPoint.hurtBox.damageModifier;
				blastAttackDamageInfo.damageType = damageType | DamageType.AOE;
				blastAttackDamageInfo.force = bonusForce * num2 + baseForce * num2 * vector2;
				blastAttackDamageInfo.position = hitPoint.hitPosition;
				blastAttackDamageInfo.procChainMask = procChainMask;
				blastAttackDamageInfo.procCoefficient = procCoefficient;
				blastAttackDamageInfo.hitHealthComponent = healthComponent;
				blastAttackDamageInfo.canRejectForce = canRejectForce;
				BlastAttackDamageInfo blastAttackDamageInfo2 = blastAttackDamageInfo;
				if (NetworkServer.active)
				{
					PerformDamageServer(in blastAttackDamageInfo2);
				}
				else
				{
					ClientReportDamage(in blastAttackDamageInfo2);
				}
				if (impactEffect != EffectIndex.Invalid)
				{
					EffectData effectData = new EffectData();
					effectData.origin = hitPoint.hitPosition;
					effectData.rotation = Quaternion.LookRotation(-vector2);
					EffectManager.SpawnEffect(impactEffect, effectData, transmit: true);
				}
			}
		}
		BlastAttack.BlastAttackMultiHit?.Invoke(hitPoints, attacker);
	}

	private static void ClientReportDamage(in BlastAttackDamageInfo blastAttackDamageInfo)
	{
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.StartMessage(75);
		blastAttackDamageInfo.Write(networkWriter);
		networkWriter.FinishMessage();
		PlatformSystems.networkManager.client.connection.SendWriter(networkWriter, QosChannelIndex.defaultReliable.intVal);
	}

	private static void PerformDamageServer(in BlastAttackDamageInfo blastAttackDamageInfo)
	{
		if ((bool)blastAttackDamageInfo.hitHealthComponent)
		{
			DamageInfo damageInfo = new DamageInfo();
			damageInfo.attacker = blastAttackDamageInfo.attacker;
			damageInfo.inflictor = blastAttackDamageInfo.inflictor;
			damageInfo.damage = blastAttackDamageInfo.damage;
			damageInfo.crit = blastAttackDamageInfo.crit;
			damageInfo.force = blastAttackDamageInfo.force;
			damageInfo.procChainMask = blastAttackDamageInfo.procChainMask;
			damageInfo.procCoefficient = blastAttackDamageInfo.procCoefficient;
			damageInfo.damageType = blastAttackDamageInfo.damageType;
			damageInfo.damageColorIndex = blastAttackDamageInfo.damageColorIndex;
			damageInfo.position = blastAttackDamageInfo.position;
			damageInfo.canRejectForce = blastAttackDamageInfo.canRejectForce;
			damageInfo.ModifyDamageInfo(blastAttackDamageInfo.damageModifier);
			blastAttackDamageInfo.hitHealthComponent.TakeDamage(damageInfo);
			GlobalEventManager.instance.OnHitEnemy(damageInfo, blastAttackDamageInfo.hitHealthComponent.gameObject);
			GlobalEventManager.instance.OnHitAll(damageInfo, blastAttackDamageInfo.hitHealthComponent.gameObject);
		}
	}
}
