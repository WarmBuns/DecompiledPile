using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[DefaultExecutionOrder(-1)]
public class NetworkRuleChoiceMask : NetworkBehaviour
{
	private const uint maskDirtyBit = 1u;

	public RuleChoiceMask ruleChoiceMask { get; private set; }

	private void Awake()
	{
		ruleChoiceMask = new RuleChoiceMask();
	}

	[Server]
	public void SetRuleChoiceMask([NotNull] RuleChoiceMask newRuleChoiceMask)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.NetworkRuleChoiceMask::SetRuleChoiceMask(RoR2.RuleChoiceMask)' called on client");
		}
		else if (!ruleChoiceMask.Equals(newRuleChoiceMask))
		{
			SetDirtyBit(1u);
			ruleChoiceMask.Copy(newRuleChoiceMask);
		}
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = base.syncVarDirtyBits;
		if (initialState)
		{
			num = 1u;
		}
		bool num2 = (num & 1) != 0;
		writer.Write((byte)num);
		if (num2)
		{
			writer.Write(ruleChoiceMask);
		}
		if (!initialState)
		{
			return num != 0;
		}
		return false;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (((uint)reader.ReadByte() & (true ? 1u : 0u)) != 0)
		{
			reader.ReadRuleChoiceMask(ruleChoiceMask);
		}
	}

	private void UNetVersion()
	{
	}

	public override void PreStartClient()
	{
	}
}
