using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

public class ChefController : NetworkBehaviour
{
	public delegate void YesChefEnded();

	[HideInInspector]
	public SkillStateOverrideData skillOverrides;

	private bool localCleaverFired;

	[SyncVar]
	public bool _cleaverAway;

	[SyncVar]
	public bool _recallCleaver;

	[NonSerialized]
	[SyncVar]
	public bool catchDirtied;

	[NonSerialized]
	public CleaverProjectile projectile;

	[NonSerialized]
	public CharacterBody characterBody;

	public Animator chefAnimator;

	internal float spreadBloom;

	public bool rolyPolyActive;

	private bool yesChefHeatActive;

	public YesChefEnded yesChefEndedDelegate;

	public bool blockOtherSkills;

	private bool cleaversAwayCached;

	public Transform handCleaverBone;

	public Transform[] cleaverStockBones;

	private Vector3[] cleaverStockBones_DefaultLocalPosition;

	private float[] cleaverStockBones_AnimateInTimer01;

	private uint soundIDMoving;

	private uint soundIDServo;

	private float walkVolume_Target;

	private float walkVolume_Smoothed;

	private float servoStrain_Timer;

	private float servoStrain_VolumeSmoothed;

	private const float servoStrainFadeInDuration = 0.15f;

	private const float servoStrainFadeOutDuration = 0.5f;

	private Queue<FireProjectileInfo> cachedCleaverProjectileInfo = new Queue<FireProjectileInfo>();

	private Dictionary<ushort, CleaverProjectile> cleaverDictionary = new Dictionary<ushort, CleaverProjectile>();

	private static int kCmdCmdSetCleaverAway;

	private static int kCmdCmdSetRecallCleaver;

	public bool cleaverAway
	{
		get
		{
			if (!localCleaverFired)
			{
				return _cleaverAway;
			}
			return true;
		}
		set
		{
			if (base.hasAuthority)
			{
				Network_cleaverAway = value;
				CallCmdSetCleaverAway(value);
			}
		}
	}

	public bool recallCleaver
	{
		get
		{
			return _recallCleaver;
		}
		set
		{
			if (base.hasAuthority)
			{
				Network_recallCleaver = value;
				CallCmdSetRecallCleaver(value);
			}
		}
	}

	public bool Network_cleaverAway
	{
		get
		{
			return _cleaverAway;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _cleaverAway, 1u);
		}
	}

	public bool Network_recallCleaver
	{
		get
		{
			return _recallCleaver;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _recallCleaver, 2u);
		}
	}

	public bool NetworkcatchDirtied
	{
		get
		{
			return catchDirtied;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref catchDirtied, 4u);
		}
	}

	[Command]
	public void CmdSetCleaverAway(bool b)
	{
		Network_cleaverAway = b;
	}

	[Command]
	public void CmdSetRecallCleaver(bool b)
	{
		Network_recallCleaver = b;
	}

	private void Awake()
	{
		int num = cleaverStockBones.Length;
		cleaverStockBones_DefaultLocalPosition = new Vector3[num];
		cleaverStockBones_AnimateInTimer01 = new float[num];
		for (int i = 0; i < num; i++)
		{
			cleaverStockBones_DefaultLocalPosition[i] = cleaverStockBones[i].localPosition;
		}
		soundIDMoving = Util.PlaySound("Play_chef_move_loop", base.gameObject);
		soundIDServo = Util.PlaySound("Play_chef_servo_loop", base.gameObject);
		if (characterBody == null)
		{
			characterBody = GetComponent<CharacterBody>();
		}
		chefAnimator = characterBody.modelLocator.modelTransform.GetComponent<Animator>();
	}

	private void Start()
	{
		CharacterBody obj = characterBody;
		obj.onJump = (CharacterBody.JumpDelegate)Delegate.Combine(obj.onJump, new CharacterBody.JumpDelegate(HandleJump));
	}

	private void OnDestroy()
	{
		CharacterBody obj = characterBody;
		obj.onJump = (CharacterBody.JumpDelegate)Delegate.Remove(obj.onJump, new CharacterBody.JumpDelegate(HandleJump));
	}

	public void SetYesChefHeatState(bool newYesChefHeatState)
	{
		yesChefHeatActive = newYesChefHeatState;
		if (!yesChefHeatActive)
		{
			yesChefEndedDelegate?.Invoke();
		}
	}

	private void HandleJump()
	{
		ActivateServoStrainSfxForDuration();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ActivateServoStrainSfxForDuration(float duration = 0.5f)
	{
		servoStrain_Timer = Mathf.Max(servoStrain_Timer, duration);
	}

	public void CacheCleaverProjectileFireInfo(FireProjectileInfo fireProjectileInfo)
	{
		cachedCleaverProjectileInfo.Enqueue(fireProjectileInfo);
	}

	public bool TryRetrieveCachedProjectileFireInfo(out FireProjectileInfo fireProjectileInfo)
	{
		fireProjectileInfo = default(FireProjectileInfo);
		if (cachedCleaverProjectileInfo.Count < 0 || !base.hasAuthority)
		{
			return false;
		}
		fireProjectileInfo = cachedCleaverProjectileInfo.Dequeue();
		return true;
	}

	private void Update()
	{
		if ((bool)characterBody && !recallCleaver && cleaverAway)
		{
			characterBody.SetSpreadBloom(spreadBloom);
		}
		CharacterMotor characterMotor = characterBody.characterMotor;
		Vector3 velocity = characterMotor.velocity;
		float sqrMagnitude = velocity.sqrMagnitude;
		bool isGrounded = characterMotor.isGrounded;
		bool @bool = chefAnimator.GetBool("isSprinting");
		Vector3 vector = (characterBody.inputBank ? characterBody.inputBank.moveVector : Vector3.zero);
		if (isGrounded && sqrMagnitude > Mathf.Epsilon)
		{
			if (rolyPolyActive)
			{
				walkVolume_Target = 1f;
			}
			else if (vector != Vector3.zero)
			{
				if (@bool)
				{
					walkVolume_Target = 0.8f;
				}
				else
				{
					walkVolume_Target = 0.5f;
				}
			}
		}
		else
		{
			walkVolume_Target = Mathf.MoveTowards(walkVolume_Target, 0f, Time.deltaTime / (1f / 3f));
		}
		walkVolume_Smoothed = Mathf.MoveTowards(walkVolume_Smoothed, Mathf.Clamp01(walkVolume_Target), Time.deltaTime / (4f / 15f));
		AkSoundEngine.SetRTPCValueByPlayingID("charChefSpeed", walkVolume_Smoothed * 100f, soundIDMoving);
		if (servoStrain_Timer > 0f)
		{
			servoStrain_Timer -= Time.deltaTime;
		}
		if (servoStrain_Timer > 0f)
		{
			float maxDelta = Time.deltaTime / 0.15f;
			servoStrain_VolumeSmoothed = Mathf.MoveTowards(servoStrain_VolumeSmoothed, 1f, maxDelta);
		}
		else
		{
			float maxDelta2 = Time.deltaTime / 0.5f;
			servoStrain_VolumeSmoothed = Mathf.MoveTowards(servoStrain_VolumeSmoothed, 0f, maxDelta2);
		}
	}

	private void LateUpdate()
	{
		bool flag = cleaverDictionary.Count > 0;
		if (flag != cleaversAwayCached || flag)
		{
			cleaversAwayCached = flag;
			handCleaverBone.localScale = (flag ? Vector3.zero : Vector3.one);
		}
		if (!(characterBody != null) || !(characterBody.skillLocator != null) || !(characterBody.skillLocator.primary != null))
		{
			return;
		}
		int stock = characterBody.skillLocator.primary.stock;
		int num = cleaverStockBones.Length;
		float deltaTime = Time.deltaTime;
		float num2 = 8f;
		float num3 = -0.2f;
		for (int i = 0; i < num; i++)
		{
			bool num4 = i + 1 <= stock;
			float current = cleaverStockBones_AnimateInTimer01[i];
			current = ((!num4) ? 0f : Mathf.MoveTowards(current, 1f, deltaTime * num2));
			cleaverStockBones_AnimateInTimer01[i] = current;
			if (current != 1f)
			{
				current = 1f - current;
				current *= current;
				cleaverStockBones[i].localScale = Vector3.one * (1f - current);
				cleaverStockBones[i].localPosition = cleaverStockBones_DefaultLocalPosition[i] + current * Vector3.up * num3;
			}
		}
	}

	internal void AddLocalProjectileReference(ushort predictionId, CleaverProjectile cleaverProjectile)
	{
		if (!cleaverDictionary.ContainsKey(predictionId) || !cleaverProjectile.projectileController.isPrediction)
		{
			cleaverDictionary[predictionId] = cleaverProjectile;
		}
	}

	internal void RemoveLocalProjectileReference(ushort predictionId)
	{
		if (cleaverDictionary.ContainsKey(predictionId))
		{
			cleaverDictionary.Remove(predictionId);
		}
		if (cleaverDictionary.Count < 1)
		{
			localCleaverFired = false;
			cleaverAway = false;
			recallCleaver = false;
		}
	}

	public void TransferSkillOverrides(SkillStateOverrideData skillOverrideData)
	{
		if (skillOverrides != null)
		{
			if (skillOverrides == skillOverrideData)
			{
				return;
			}
			skillOverrides.ClearOverrides();
		}
		skillOverrides = skillOverrideData;
	}

	public void ClearSkillOverrides()
	{
		if (skillOverrides != null)
		{
			skillOverrides.ClearOverrides();
		}
		skillOverrides = null;
	}

	[Server]
	public void DestroyCleavers()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ChefController::DestroyCleavers()' called on client");
			return;
		}
		foreach (CleaverProjectile value in cleaverDictionary.Values)
		{
			value.CallCmdDestroyAndCleanup();
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdSetCleaverAway(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetCleaverAway called on client.");
		}
		else
		{
			((ChefController)obj).CmdSetCleaverAway(reader.ReadBoolean());
		}
	}

	protected static void InvokeCmdCmdSetRecallCleaver(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetRecallCleaver called on client.");
		}
		else
		{
			((ChefController)obj).CmdSetRecallCleaver(reader.ReadBoolean());
		}
	}

	public void CallCmdSetCleaverAway(bool b)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSetCleaverAway called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSetCleaverAway(b);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSetCleaverAway);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(b);
		SendCommandInternal(networkWriter, 0, "CmdSetCleaverAway");
	}

	public void CallCmdSetRecallCleaver(bool b)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSetRecallCleaver called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSetRecallCleaver(b);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSetRecallCleaver);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(b);
		SendCommandInternal(networkWriter, 0, "CmdSetRecallCleaver");
	}

	static ChefController()
	{
		kCmdCmdSetCleaverAway = -1756854004;
		NetworkBehaviour.RegisterCommandDelegate(typeof(ChefController), kCmdCmdSetCleaverAway, InvokeCmdCmdSetCleaverAway);
		kCmdCmdSetRecallCleaver = 535604397;
		NetworkBehaviour.RegisterCommandDelegate(typeof(ChefController), kCmdCmdSetRecallCleaver, InvokeCmdCmdSetRecallCleaver);
		NetworkCRC.RegisterBehaviour("ChefController", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(_cleaverAway);
			writer.Write(_recallCleaver);
			writer.Write(catchDirtied);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_cleaverAway);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_recallCleaver);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(catchDirtied);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			_cleaverAway = reader.ReadBoolean();
			_recallCleaver = reader.ReadBoolean();
			catchDirtied = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			_cleaverAway = reader.ReadBoolean();
		}
		if (((uint)num & 2u) != 0)
		{
			_recallCleaver = reader.ReadBoolean();
		}
		if (((uint)num & 4u) != 0)
		{
			catchDirtied = reader.ReadBoolean();
		}
	}

	public override void PreStartClient()
	{
	}
}
