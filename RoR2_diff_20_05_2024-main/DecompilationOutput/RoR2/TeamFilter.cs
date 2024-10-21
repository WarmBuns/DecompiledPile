using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class TeamFilter : NetworkBehaviour
{
	[SyncVar]
	private int teamIndexInternal;

	[SerializeField]
	private TeamIndex defaultTeam = TeamIndex.None;

	public TeamIndex teamIndex
	{
		get
		{
			return (TeamIndex)teamIndexInternal;
		}
		set
		{
			NetworkteamIndexInternal = (int)value;
			defaultTeam = value;
		}
	}

	public int NetworkteamIndexInternal
	{
		get
		{
			return teamIndexInternal;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref teamIndexInternal, 1u);
		}
	}

	public void Awake()
	{
		if (NetworkServer.active)
		{
			teamIndex = defaultTeam;
		}
	}

	[Server]
	public void SetTeamServer(string teamName)
	{
		TeamIndex result;
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.TeamFilter::SetTeamServer(System.String)' called on client");
		}
		else if (Enum.TryParse<TeamIndex>(teamName, out result))
		{
			teamIndex = result;
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt32((uint)teamIndexInternal);
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
			writer.WritePackedUInt32((uint)teamIndexInternal);
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
			teamIndexInternal = (int)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			teamIndexInternal = (int)reader.ReadPackedUInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}
