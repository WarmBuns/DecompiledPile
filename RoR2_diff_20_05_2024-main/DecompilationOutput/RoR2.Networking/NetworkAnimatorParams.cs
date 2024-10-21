using System;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Networking;

public class NetworkAnimatorParams : NetworkBehaviour
{
	public Animator targetAnimator;

	public string[] targetAnimatorParamNames;

	public float transmitInterval = 0.1f;

	private NetworkIdentity networkIdentity;

	private static readonly uint animatorParamsDirtyBit;

	private static readonly uint allDirtyBits;

	private int[] targetAnimatorParamHashes;

	private NetworkLerpedFloat[] targetAnimatorParamNetworkValues;

	private float transmitTimerAuthority;

	private bool animatorParamsDirtyAuthority;

	private static int kCmdCmdReceiveAnimatorParams;

	private void Awake()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
		targetAnimatorParamHashes = new int[targetAnimatorParamNames.Length];
		for (int i = 0; i < targetAnimatorParamNames.Length; i++)
		{
			targetAnimatorParamHashes[i] = Animator.StringToHash(targetAnimatorParamNames[i]);
		}
		targetAnimatorParamNetworkValues = new NetworkLerpedFloat[targetAnimatorParamNames.Length];
	}

	private void FixedUpdate()
	{
		if (Util.HasEffectiveAuthority(networkIdentity))
		{
			FixedUpdateAuthority();
		}
	}

	private void Update()
	{
		if (!Util.HasEffectiveAuthority(networkIdentity))
		{
			ApplyAnimatorParamsNonAuthority();
		}
	}

	private void FixedUpdateAuthority()
	{
		CollectAnimatorParamsAuthority(out var animatorParamsChanged);
		animatorParamsDirtyAuthority |= animatorParamsChanged;
		transmitTimerAuthority -= Time.fixedDeltaTime;
		if (!(transmitTimerAuthority <= 0f))
		{
			return;
		}
		transmitTimerAuthority = transmitInterval;
		if (!animatorParamsDirtyAuthority)
		{
			return;
		}
		animatorParamsDirtyAuthority = false;
		if (NetworkServer.active)
		{
			SetDirtyBit(animatorParamsDirtyBit);
			return;
		}
		float[] array = new float[targetAnimatorParamNetworkValues.Length];
		for (int i = 0; i < targetAnimatorParamNetworkValues.Length; i++)
		{
			array[i] = targetAnimatorParamNetworkValues[i].GetAuthoritativeValue();
		}
		CallCmdReceiveAnimatorParams(array);
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = (initialState ? allDirtyBits : base.syncVarDirtyBits);
		bool num2 = (num & animatorParamsDirtyBit) != 0;
		writer.WritePackedUInt32(num);
		if (num2)
		{
			for (int i = 0; i < targetAnimatorParamNetworkValues.Length; i++)
			{
				writer.Write(targetAnimatorParamNetworkValues[i].GetAuthoritativeValue());
			}
		}
		return num != 0;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if ((reader.ReadPackedUInt32() & animatorParamsDirtyBit) != 0)
		{
			for (int i = 0; i < targetAnimatorParamNetworkValues.Length; i++)
			{
				targetAnimatorParamNetworkValues[i].PushValue(reader.ReadSingle());
			}
		}
	}

	private void ApplyAnimatorParamsNonAuthority()
	{
		for (int i = 0; i < targetAnimatorParamHashes.Length; i++)
		{
			targetAnimator.SetFloat(targetAnimatorParamHashes[i], targetAnimatorParamNetworkValues[i].GetCurrentValue(hasAuthority: false));
		}
	}

	private void CollectAnimatorParamsAuthority(out bool animatorParamsChanged)
	{
		animatorParamsChanged = false;
		for (int i = 0; i < targetAnimatorParamHashes.Length; i++)
		{
			float @float = targetAnimator.GetFloat(targetAnimatorParamHashes[i]);
			ref NetworkLerpedFloat reference = ref targetAnimatorParamNetworkValues[i];
			float currentValue = reference.GetCurrentValue(hasAuthority: true);
			if (@float != currentValue)
			{
				animatorParamsChanged = true;
				reference.SetValueImmediate(@float);
			}
		}
	}

	[Command]
	private void CmdReceiveAnimatorParams(float[] animatorParamValues)
	{
		int i = 0;
		for (int num = Math.Min(animatorParamValues.Length, targetAnimatorParamNetworkValues.Length); i < num; i++)
		{
			targetAnimatorParamNetworkValues[i].PushValue(animatorParamValues[i]);
		}
		SetDirtyBit(animatorParamsDirtyBit);
	}

	static NetworkAnimatorParams()
	{
		animatorParamsDirtyBit = 1u;
		allDirtyBits = animatorParamsDirtyBit;
		kCmdCmdReceiveAnimatorParams = -1267054443;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkAnimatorParams), kCmdCmdReceiveAnimatorParams, InvokeCmdCmdReceiveAnimatorParams);
		NetworkCRC.RegisterBehaviour("NetworkAnimatorParams", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdReceiveAnimatorParams(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdReceiveAnimatorParams called on client.");
		}
		else
		{
			((NetworkAnimatorParams)obj).CmdReceiveAnimatorParams(GeneratedNetworkCode._ReadArraySingle_None(reader));
		}
	}

	public void CallCmdReceiveAnimatorParams(float[] animatorParamValues)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdReceiveAnimatorParams called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdReceiveAnimatorParams(animatorParamValues);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdReceiveAnimatorParams);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteArraySingle_None(networkWriter, animatorParamValues);
		SendCommandInternal(networkWriter, 0, "CmdReceiveAnimatorParams");
	}

	public override void PreStartClient()
	{
	}
}
