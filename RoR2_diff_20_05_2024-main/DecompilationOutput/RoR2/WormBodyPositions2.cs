using System;
using System.Collections.Generic;
using EntityStates.MagmaWorm;
using RoR2.Projectile;
using Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class WormBodyPositions2 : NetworkBehaviour, ITeleportHandler, IEventSystemHandler, ILifeBehavior, IPainAnimationHandler
{
	private struct PositionRotationPair
	{
		public Vector3 position;

		public Quaternion rotation;
	}

	[Serializable]
	public struct KeyFrame
	{
		public CubicBezier3 curve;

		public float length;

		public float time;
	}

	public delegate void BurrowExpectedCallback(float expectedTime, Vector3 hitPosition, Vector3 hitNormal);

	public delegate void BreachExpectedCallback(float expectedTime, Vector3 hitPosition, Vector3 hitNormal);

	public struct TravelCallback
	{
		public float time;

		public Action callback;
	}

	public Transform[] bones;

	public float[] segmentLengths;

	[Tooltip("How far behind the chaser the head is, in seconds.")]
	public float followDelay = 2f;

	[Tooltip("Whether or not the jaw will close/open.")]
	public bool animateJaws;

	public Animator animator;

	public string jawMecanimCycleParameter;

	public float jawMecanimDampTime;

	public float jawClosedDistance;

	public float jawOpenDistance;

	public GameObject warningEffectPrefab;

	public GameObject burrowEffectPrefab;

	public float maxPainDisplacementMagnitude = 2f;

	public float painDisplacementRecoverySpeed = 8f;

	public bool shouldFireMeatballsOnImpact = true;

	public bool shouldFireBlastAttackOnImpact = true;

	public bool enableSurfaceTests = true;

	public float undergroundTestYOffset;

	private CharacterBody characterBody;

	private CharacterDirection characterDirection;

	private PositionRotationPair[] boneTransformationBuffer;

	private Vector3[] boneDisplacements;

	private float headDistance;

	private float totalLength;

	private const float endBonusLength = 4f;

	private const float fakeEndSegmentLength = 2f;

	private readonly List<KeyFrame> keyFrames = new List<KeyFrame>();

	private float keyFramesTotalLength;

	public float spawnDepth = 30f;

	private Collider entranceCollider;

	private Collider exitCollider;

	private Vector3 previousSurfaceTestEnd;

	private List<TravelCallback> travelCallbacks;

	private const float impactSoundPrestartDuration = 0.5f;

	public float impactCooldownDuration = 0.1f;

	private float enterTriggerCooldownTimer;

	private float exitTriggerCooldownTimer;

	private bool shouldTriggerDeathEffectOnNextImpact;

	private float deathTime = float.NegativeInfinity;

	public GameObject meatballProjectile;

	public GameObject blastAttackEffect;

	public int meatballCount;

	public float meatballAngle;

	public float meatballDamageCoefficient;

	public float meatballProcCoefficient;

	public float meatballForce;

	public float blastAttackDamageCoefficient;

	public float blastAttackProcCoefficient;

	public float blastAttackRadius;

	public float blastAttackForce;

	public float blastAttackBonusVerticalForce;

	public float speedMultiplier = 2f;

	private bool _playingBurrowSound;

	private static int kRpcRpcSendKeyFrame;

	private static int kRpcRpcPlaySurfaceImpactSound;

	public KeyFrame newestKeyFrame { get; private set; }

	public bool underground { get; private set; }

	private bool playingBurrowSound
	{
		get
		{
			return _playingBurrowSound;
		}
		set
		{
			if (value != _playingBurrowSound)
			{
				_playingBurrowSound = value;
				Util.PlaySound(value ? "Play_magmaWorm_burrowed_loop" : "Stop_magmaWorm_burrowed_loop", bones[0].gameObject);
			}
		}
	}

	public event BurrowExpectedCallback onPredictedBurrowDiscovered;

	public event BreachExpectedCallback onPredictedBreachDiscovered;

	private void Awake()
	{
		characterBody = GetComponent<CharacterBody>();
		characterDirection = GetComponent<CharacterDirection>();
		boneTransformationBuffer = new PositionRotationPair[bones.Length + 1];
		totalLength = 0f;
		for (int i = 0; i < segmentLengths.Length; i++)
		{
			totalLength += segmentLengths[i];
		}
		if (NetworkServer.active)
		{
			travelCallbacks = new List<TravelCallback>();
		}
		boneDisplacements = new Vector3[bones.Length];
	}

	private void Start()
	{
		if (NetworkServer.active)
		{
			PopulateInitialKeyFrames();
			previousSurfaceTestEnd = newestKeyFrame.curve.Evaluate(1f);
		}
	}

	private void OnDestroy()
	{
		travelCallbacks = null;
		_playingBurrowSound = false;
	}

	private void BakeSegmentLengths()
	{
		segmentLengths = new float[bones.Length];
		Vector3 vector = bones[0].position;
		for (int i = 1; i < bones.Length; i++)
		{
			Vector3 position = bones[i].position;
			float magnitude = (vector - position).magnitude;
			segmentLengths[i - 1] = magnitude;
			vector = position;
		}
		segmentLengths[bones.Length - 1] = 2f;
	}

	[Server]
	private void PopulateInitialKeyFrames()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.WormBodyPositions2::PopulateInitialKeyFrames()' called on client");
			return;
		}
		bool flag = enableSurfaceTests;
		enableSurfaceTests = false;
		Vector3 vector = base.transform.position + Vector3.down * spawnDepth;
		float synchronizedTimeStamp = GetSynchronizedTimeStamp();
		newestKeyFrame = new KeyFrame
		{
			curve = CubicBezier3.FromVelocities(vector + Vector3.down * 2f, Vector3.up, vector + Vector3.down * 1f, Vector3.down),
			time = synchronizedTimeStamp - 2f,
			length = 1f
		};
		AttemptToGenerateKeyFrame(synchronizedTimeStamp - 1f, vector + Vector3.down, Vector3.up);
		AttemptToGenerateKeyFrame(synchronizedTimeStamp, vector, Vector3.up);
		headDistance = 0f;
		enableSurfaceTests = flag;
	}

	private Vector3 EvaluatePositionAlongCurve(float positionDownBody)
	{
		float num = 0f;
		foreach (KeyFrame keyFrame in keyFrames)
		{
			float b = num;
			num += keyFrame.length;
			if (num >= positionDownBody)
			{
				float t = Mathf.InverseLerp(num, b, positionDownBody);
				CubicBezier3 curve = keyFrame.curve;
				return curve.Evaluate(t);
			}
		}
		if (keyFrames.Count > 0)
		{
			return keyFrames[keyFrames.Count - 1].curve.Evaluate(1f);
		}
		return Vector3.zero;
	}

	private void UpdateBones()
	{
		float num = totalLength;
		boneTransformationBuffer[boneTransformationBuffer.Length - 1] = new PositionRotationPair
		{
			position = EvaluatePositionAlongCurve(headDistance + num),
			rotation = Quaternion.identity
		};
		for (int num2 = boneTransformationBuffer.Length - 2; num2 >= 0; num2--)
		{
			num -= segmentLengths[num2];
			Vector3 vector = EvaluatePositionAlongCurve(headDistance + num);
			Quaternion rotation = Util.QuaternionSafeLookRotation(vector - boneTransformationBuffer[num2 + 1].position, Vector3.up);
			boneTransformationBuffer[num2] = new PositionRotationPair
			{
				position = vector,
				rotation = rotation
			};
		}
		if (bones.Length == 0 || !bones[0])
		{
			return;
		}
		Vector3 forward = bones[0].forward;
		for (int i = 0; i < bones.Length; i++)
		{
			Transform transform = bones[i];
			if ((bool)transform)
			{
				transform.position = boneTransformationBuffer[i].position + boneDisplacements[i];
				transform.forward = forward;
				transform.up = boneTransformationBuffer[i].rotation * -Vector3.forward;
				forward = transform.forward;
			}
		}
	}

	public void AddKeyFrame(in KeyFrame newKeyFrame)
	{
		newestKeyFrame = newKeyFrame;
		keyFrames.Insert(0, newKeyFrame);
		keyFramesTotalLength += newKeyFrame.length;
		headDistance += newKeyFrame.length;
		bool flag = false;
		float num = keyFramesTotalLength;
		float num2 = totalLength + headDistance + 4f;
		while (keyFrames.Count > 0 && (num -= keyFrames[keyFrames.Count - 1].length) > num2)
		{
			keyFrames.RemoveAt(keyFrames.Count - 1);
			flag = true;
		}
		if (flag)
		{
			keyFramesTotalLength = 0f;
			foreach (KeyFrame keyFrame in keyFrames)
			{
				keyFramesTotalLength += keyFrame.length;
			}
		}
		if (NetworkServer.active)
		{
			CallRpcSendKeyFrame(newKeyFrame);
			if (enableSurfaceTests)
			{
				SurfaceTest(newKeyFrame.curve.p1, ref previousSurfaceTestEnd, newKeyFrame.time, OnPredictedBurrowDiscovered, OnPredictedBreachDiscovered);
			}
		}
	}

	[Server]
	public void AttemptToGenerateKeyFrame(float arrivalTime, Vector3 position, Vector3 velocity)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.WormBodyPositions2::AttemptToGenerateKeyFrame(System.Single,UnityEngine.Vector3,UnityEngine.Vector3)' called on client");
			return;
		}
		KeyFrame keyFrame = newestKeyFrame;
		float num = arrivalTime - keyFrame.time;
		CubicBezier3 curve = CubicBezier3.FromVelocities(keyFrame.curve.p1, -keyFrame.curve.v1, position, -velocity * (num * 0.25f));
		float length = curve.ApproximateLength(50);
		KeyFrame keyFrame2 = default(KeyFrame);
		keyFrame2.curve = curve;
		keyFrame2.length = length;
		keyFrame2.time = arrivalTime;
		KeyFrame newKeyFrame = keyFrame2;
		if (newKeyFrame.length >= 0f)
		{
			AddKeyFrame(in newKeyFrame);
		}
	}

	[ClientRpc]
	private void RpcSendKeyFrame(KeyFrame newKeyFrame)
	{
		if (!NetworkServer.active)
		{
			AddKeyFrame(in newKeyFrame);
		}
	}

	private void Update()
	{
		UpdateBoneDisplacements(Time.deltaTime);
		UpdateHeadOffset();
		if (animateJaws)
		{
			UpdateJaws();
		}
	}

	private void UpdateJaws()
	{
		if ((bool)animator)
		{
			float value = Mathf.Clamp01(Util.Remap((bones[0].position - base.transform.position).magnitude, jawClosedDistance, jawOpenDistance, 0f, 1f));
			animator.SetFloat(jawMecanimCycleParameter, value, jawMecanimDampTime, Time.deltaTime);
		}
	}

	private void UpdateHeadOffset()
	{
		float num = headDistance;
		int num2 = keyFrames.Count - 1;
		float num3 = 0f;
		float synchronizedTimeStamp = GetSynchronizedTimeStamp();
		for (int i = 0; i < num2; i++)
		{
			float time = keyFrames[i + 1].time;
			float length = keyFrames[i].length;
			if (time < synchronizedTimeStamp)
			{
				num = num3 + length * Mathf.InverseLerp(keyFrames[i].time, time, synchronizedTimeStamp);
				break;
			}
			num3 += length;
		}
		OnTravel(headDistance - num);
	}

	private void OnTravel(float distance)
	{
		headDistance -= distance;
		UpdateBones();
	}

	private void OnPredictedBurrowDiscovered(float expectedTime, Vector3 point, Vector3 surfaceNormal)
	{
		AddTravelCallback(new TravelCallback
		{
			time = expectedTime,
			callback = delegate
			{
				OnEnterSurface(point, surfaceNormal);
			}
		});
		AddTravelCallback(new TravelCallback
		{
			time = expectedTime - 0.5f,
			callback = RpcPlaySurfaceImpactSound
		});
		this.onPredictedBurrowDiscovered?.Invoke(expectedTime, point, surfaceNormal);
	}

	private void OnPredictedBreachDiscovered(float expectedTime, Vector3 point, Vector3 surfaceNormal)
	{
		if ((bool)warningEffectPrefab)
		{
			EffectManager.SpawnEffect(warningEffectPrefab, new EffectData
			{
				origin = point,
				rotation = Util.QuaternionSafeLookRotation(surfaceNormal)
			}, transmit: true);
		}
		AddTravelCallback(new TravelCallback
		{
			time = expectedTime,
			callback = delegate
			{
				OnExitSurface(point, surfaceNormal);
			}
		});
		AddTravelCallback(new TravelCallback
		{
			time = expectedTime - 0.5f,
			callback = RpcPlaySurfaceImpactSound
		});
		this.onPredictedBreachDiscovered?.Invoke(expectedTime, point, surfaceNormal);
	}

	[Server]
	private static void SurfaceTest(Vector3 currentPosition, ref Vector3 previousPosition, float arrivalTime, BurrowExpectedCallback onPredictedBurrowDiscovered, BreachExpectedCallback onPredictedBreachDiscovered)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.WormBodyPositions2::SurfaceTest(UnityEngine.Vector3,UnityEngine.Vector3&,System.Single,RoR2.WormBodyPositions2/BurrowExpectedCallback,RoR2.WormBodyPositions2/BreachExpectedCallback)' called on client");
			return;
		}
		Vector3 vector = currentPosition - previousPosition;
		float magnitude = vector.magnitude;
		if (Physics.Raycast(previousPosition, vector, out var hitInfo, magnitude, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
		{
			onPredictedBurrowDiscovered?.Invoke(arrivalTime, hitInfo.point, hitInfo.normal);
		}
		if (Physics.Raycast(currentPosition, -vector, out hitInfo, magnitude, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
		{
			onPredictedBreachDiscovered?.Invoke(arrivalTime, hitInfo.point, hitInfo.normal);
		}
		previousPosition = currentPosition;
	}

	public void AddTravelCallback(TravelCallback newTravelCallback)
	{
		int index = travelCallbacks.Count;
		float time = newTravelCallback.time;
		for (int i = 0; i < travelCallbacks.Count; i++)
		{
			if (time < travelCallbacks[i].time)
			{
				index = i;
				break;
			}
		}
		travelCallbacks.Insert(index, newTravelCallback);
	}

	[ClientRpc]
	private void RpcPlaySurfaceImpactSound()
	{
	}

	[Server]
	private void OnEnterSurface(Vector3 point, Vector3 surfaceNormal)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.WormBodyPositions2::OnEnterSurface(UnityEngine.Vector3,UnityEngine.Vector3)' called on client");
		}
		else
		{
			if (enterTriggerCooldownTimer > 0f)
			{
				return;
			}
			if (shouldTriggerDeathEffectOnNextImpact && Run.instance.fixedTime - deathTime >= DeathState.duration - 3f)
			{
				shouldTriggerDeathEffectOnNextImpact = false;
				return;
			}
			enterTriggerCooldownTimer = impactCooldownDuration;
			EffectManager.SpawnEffect(burrowEffectPrefab, new EffectData
			{
				origin = point,
				rotation = Util.QuaternionSafeLookRotation(surfaceNormal),
				scale = 1f
			}, transmit: true);
			if (shouldFireMeatballsOnImpact)
			{
				FireMeatballs(surfaceNormal, point + surfaceNormal * 3f, characterDirection.forward, meatballCount, meatballAngle, meatballForce);
			}
			if (shouldFireBlastAttackOnImpact)
			{
				FireImpactBlastAttack(point + surfaceNormal);
			}
		}
	}

	public void OnDeathStart()
	{
		deathTime = Run.instance.fixedTime;
		shouldTriggerDeathEffectOnNextImpact = true;
	}

	[Server]
	private void PerformDeath()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.WormBodyPositions2::PerformDeath()' called on client");
			return;
		}
		for (int i = 0; i < bones.Length; i++)
		{
			if ((bool)bones[i])
			{
				EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MagmaWormDeathDust"), new EffectData
				{
					origin = bones[i].position,
					rotation = UnityEngine.Random.rotation,
					scale = 1f
				}, transmit: true);
			}
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	[Server]
	private void OnExitSurface(Vector3 point, Vector3 surfaceNormal)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.WormBodyPositions2::OnExitSurface(UnityEngine.Vector3,UnityEngine.Vector3)' called on client");
		}
		else if (!(exitTriggerCooldownTimer > 0f))
		{
			exitTriggerCooldownTimer = impactCooldownDuration;
			EffectManager.SpawnEffect(burrowEffectPrefab, new EffectData
			{
				origin = point,
				rotation = Util.QuaternionSafeLookRotation(surfaceNormal),
				scale = 1f
			}, transmit: true);
			if (shouldFireMeatballsOnImpact)
			{
				FireMeatballs(surfaceNormal, point + surfaceNormal * 3f, characterDirection.forward, meatballCount, meatballAngle, meatballForce);
			}
		}
	}

	private void FireMeatballs(Vector3 impactNormal, Vector3 impactPosition, Vector3 forward, int meatballCount, float meatballAngle, float meatballForce)
	{
		float num = 360f / (float)meatballCount;
		Vector3 normalized = Vector3.ProjectOnPlane(forward, impactNormal).normalized;
		Vector3 vector = Vector3.RotateTowards(impactNormal, normalized, meatballAngle * (MathF.PI / 180f), float.PositiveInfinity);
		for (int i = 0; i < meatballCount; i++)
		{
			Vector3 forward2 = Quaternion.AngleAxis(num * (float)i, impactNormal) * vector;
			ProjectileManager.instance.FireProjectile(meatballProjectile, impactPosition, Util.QuaternionSafeLookRotation(forward2), base.gameObject, characterBody.damage * meatballDamageCoefficient, meatballForce, Util.CheckRoll(characterBody.crit, characterBody.master));
		}
	}

	private void FireImpactBlastAttack(Vector3 impactPosition)
	{
		BlastAttack obj = new BlastAttack
		{
			baseDamage = characterBody.damage * blastAttackDamageCoefficient,
			procCoefficient = blastAttackProcCoefficient,
			baseForce = blastAttackForce,
			bonusForce = Vector3.up * blastAttackBonusVerticalForce,
			crit = Util.CheckRoll(characterBody.crit, characterBody.master),
			radius = blastAttackRadius,
			damageType = DamageType.IgniteOnHit,
			falloffModel = BlastAttack.FalloffModel.SweetSpot,
			attacker = base.gameObject
		};
		obj.teamIndex = TeamComponent.GetObjectTeam(obj.attacker);
		obj.position = impactPosition;
		obj.attackerFiltering = AttackerFiltering.NeverHitSelf;
		obj.Fire();
		if (NetworkServer.active)
		{
			EffectManager.SpawnEffect(blastAttackEffect, new EffectData
			{
				origin = impactPosition,
				scale = blastAttackRadius
			}, transmit: true);
		}
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			enterTriggerCooldownTimer -= Time.fixedDeltaTime;
			exitTriggerCooldownTimer -= Time.fixedDeltaTime;
			float synchronizedTimeStamp = GetSynchronizedTimeStamp();
			while (travelCallbacks.Count > 0 && travelCallbacks[0].time <= synchronizedTimeStamp)
			{
				TravelCallback travelCallback = travelCallbacks[0];
				travelCallbacks.RemoveAt(0);
				travelCallback.callback();
			}
		}
		if (bones.Length != 0 && (bool)bones[0] && (bool)bones[0].transform && (bool)base.transform)
		{
			bool flag = bones[0].transform.position.y - base.transform.position.y < undergroundTestYOffset;
			playingBurrowSound = flag;
		}
	}

	private void DrawKeyFrame(KeyFrame keyFrame)
	{
		Gizmos.color = Color.Lerp(Color.green, Color.black, 0.5f);
		Gizmos.DrawRay(keyFrame.curve.p0, keyFrame.curve.v0);
		Gizmos.color = Color.Lerp(Color.red, Color.black, 0.5f);
		Gizmos.DrawRay(keyFrame.curve.p1, keyFrame.curve.v1);
		for (int i = 1; i <= 20; i++)
		{
			float num = (float)i * 0.05f;
			Gizmos.color = Color.Lerp(Color.red, Color.green, num);
			Vector3 vector = keyFrame.curve.Evaluate(num - 0.05f);
			Vector3 vector2 = keyFrame.curve.Evaluate(num);
			Gizmos.DrawRay(vector, vector2 - vector);
		}
	}

	private void OnDrawGizmos()
	{
		foreach (KeyFrame keyFrame in keyFrames)
		{
			DrawKeyFrame(keyFrame);
		}
		for (int i = 0; i < boneTransformationBuffer.Length; i++)
		{
			Gizmos.matrix = Matrix4x4.TRS(boneTransformationBuffer[i].position, boneTransformationBuffer[i].rotation, Vector3.one * 3f);
			Gizmos.DrawRay(-Vector3.forward, Vector3.forward * 2f);
			Gizmos.DrawRay(-Vector3.right, Vector3.right * 2f);
			Gizmos.DrawRay(-Vector3.up, Vector3.up * 2f);
		}
	}

	public void OnTeleport(Vector3 oldPosition, Vector3 newPosition)
	{
		Vector3 vector = newPosition - oldPosition;
		for (int i = 0; i < keyFrames.Count; i++)
		{
			KeyFrame value = keyFrames[i];
			CubicBezier3 curve = value.curve;
			curve.a += vector;
			curve.b += vector;
			curve.c += vector;
			curve.d += vector;
			value.curve = curve;
			keyFrames[i] = value;
		}
		previousSurfaceTestEnd += vector;
	}

	private int FindNearestBone(Vector3 worldPosition)
	{
		int result = -1;
		float num = float.PositiveInfinity;
		for (int i = 0; i < bones.Length; i++)
		{
			float sqrMagnitude = (bones[i].transform.position - worldPosition).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = i;
			}
		}
		return result;
	}

	private void UpdateBoneDisplacements(float deltaTime)
	{
		int i = 0;
		for (int num = boneDisplacements.Length; i < num; i++)
		{
			boneDisplacements[i] = Vector3.MoveTowards(boneDisplacements[i], Vector3.zero, painDisplacementRecoverySpeed * deltaTime);
		}
	}

	void IPainAnimationHandler.HandlePain(float damage, Vector3 damagePosition)
	{
		int num = FindNearestBone(damagePosition);
		if (num != -1)
		{
			boneDisplacements[num] = UnityEngine.Random.onUnitSphere * maxPainDisplacementMagnitude;
		}
	}

	public float GetSynchronizedTimeStamp()
	{
		return Run.instance.time;
	}

	private static void WriteKeyFrame(NetworkWriter writer, KeyFrame keyFrame)
	{
		writer.Write(keyFrame.curve.a);
		writer.Write(keyFrame.curve.b);
		writer.Write(keyFrame.curve.c);
		writer.Write(keyFrame.curve.d);
		writer.Write(keyFrame.length);
		writer.Write(keyFrame.time);
	}

	private static KeyFrame ReadKeyFrame(NetworkReader reader)
	{
		KeyFrame result = default(KeyFrame);
		result.curve.a = reader.ReadVector3();
		result.curve.b = reader.ReadVector3();
		result.curve.c = reader.ReadVector3();
		result.curve.d = reader.ReadVector3();
		result.length = reader.ReadSingle();
		result.time = reader.ReadSingle();
		return result;
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = base.syncVarDirtyBits;
		if (initialState)
		{
			writer.Write((ushort)keyFrames.Count);
			for (int i = 0; i < keyFrames.Count; i++)
			{
				WriteKeyFrame(writer, keyFrames[i]);
			}
		}
		if (!initialState)
		{
			return num != 0;
		}
		return false;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			keyFrames.Clear();
			int num = reader.ReadUInt16();
			for (int i = 0; i < num; i++)
			{
				keyFrames.Add(ReadKeyFrame(reader));
			}
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcSendKeyFrame(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSendKeyFrame called on server.");
		}
		else
		{
			((WormBodyPositions2)obj).RpcSendKeyFrame(GeneratedNetworkCode._ReadKeyFrame_WormBodyPositions2(reader));
		}
	}

	protected static void InvokeRpcRpcPlaySurfaceImpactSound(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlaySurfaceImpactSound called on server.");
		}
		else
		{
			((WormBodyPositions2)obj).RpcPlaySurfaceImpactSound();
		}
	}

	public void CallRpcSendKeyFrame(KeyFrame newKeyFrame)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSendKeyFrame called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSendKeyFrame);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteKeyFrame_WormBodyPositions2(networkWriter, newKeyFrame);
		SendRPCInternal(networkWriter, 0, "RpcSendKeyFrame");
	}

	public void CallRpcPlaySurfaceImpactSound()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcPlaySurfaceImpactSound called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcPlaySurfaceImpactSound);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcPlaySurfaceImpactSound");
	}

	static WormBodyPositions2()
	{
		kRpcRpcSendKeyFrame = 874152969;
		NetworkBehaviour.RegisterRpcDelegate(typeof(WormBodyPositions2), kRpcRpcSendKeyFrame, InvokeRpcRpcSendKeyFrame);
		kRpcRpcPlaySurfaceImpactSound = 2010133795;
		NetworkBehaviour.RegisterRpcDelegate(typeof(WormBodyPositions2), kRpcRpcPlaySurfaceImpactSound, InvokeRpcRpcPlaySurfaceImpactSound);
		NetworkCRC.RegisterBehaviour("WormBodyPositions2", 0);
	}

	public override void PreStartClient()
	{
	}
}
