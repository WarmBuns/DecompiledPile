using UnityEngine;

namespace RoR2;

public class SubmitCommandCall : MonoBehaviour
{
	public string cmdPrimary;

	public bool onlyExecutePrimaryOnLocalPlayer;

	public string cmdAlternate;

	public bool onlyExecuteAlternateOnLocalPlayer;

	public string cmdTertiary;

	public bool onlyExecuteTertiaryOnLocalPlayer;

	public bool doForEveryActiveTarget = true;

	public void SubmitPrimaryCmd()
	{
		if (!string.IsNullOrEmpty(cmdPrimary))
		{
			Console.instance.SubmitCmd(null, cmdPrimary);
		}
	}

	public void SubmitAlternateCmd()
	{
		if (!string.IsNullOrEmpty(cmdAlternate))
		{
			Console.instance.SubmitCmd(null, cmdPrimary);
		}
	}

	public void SubmitTertiaryCmd()
	{
		if (!string.IsNullOrEmpty(cmdTertiary))
		{
			Console.instance.SubmitCmd(null, cmdTertiary);
		}
	}

	public void SubmitPrimaryCmdOnLocalPlayer()
	{
		if (string.IsNullOrEmpty(cmdPrimary))
		{
			return;
		}
		if (!onlyExecutePrimaryOnLocalPlayer && doForEveryActiveTarget)
		{
			bool flag = false;
			foreach (NetworkUser readOnlyLocalPlayers in NetworkUser.readOnlyLocalPlayersList)
			{
				if ((bool)readOnlyLocalPlayers)
				{
					Console.instance.SubmitCmd(readOnlyLocalPlayers, cmdPrimary);
					flag = true;
				}
			}
			if (!flag)
			{
				Console.instance.SubmitCmd(NetworkUser.readOnlyLocalPlayersList[0], cmdPrimary);
			}
		}
		else
		{
			Console.instance.SubmitCmd(NetworkUser.readOnlyLocalPlayersList[0], cmdPrimary);
		}
	}

	public void SubmitAlternateCmdOnLocalPlayer()
	{
		if (string.IsNullOrEmpty(cmdAlternate))
		{
			return;
		}
		if (!onlyExecuteAlternateOnLocalPlayer && doForEveryActiveTarget)
		{
			bool flag = false;
			foreach (NetworkUser readOnlyLocalPlayers in NetworkUser.readOnlyLocalPlayersList)
			{
				if ((bool)readOnlyLocalPlayers)
				{
					Console.instance.SubmitCmd(readOnlyLocalPlayers, cmdAlternate);
					flag = true;
				}
			}
			if (!flag)
			{
				Console.instance.SubmitCmd(NetworkUser.readOnlyLocalPlayersList[0], cmdAlternate);
			}
		}
		else
		{
			Console.instance.SubmitCmd(NetworkUser.readOnlyLocalPlayersList[0], cmdAlternate);
		}
	}

	public void SubmitTertiaryCmdOnLocalPlayer()
	{
		if (string.IsNullOrEmpty(cmdTertiary))
		{
			return;
		}
		if (!onlyExecuteTertiaryOnLocalPlayer && doForEveryActiveTarget)
		{
			bool flag = false;
			foreach (NetworkUser readOnlyLocalPlayers in NetworkUser.readOnlyLocalPlayersList)
			{
				if ((bool)readOnlyLocalPlayers)
				{
					Console.instance.SubmitCmd(readOnlyLocalPlayers, cmdTertiary);
					flag = true;
				}
			}
			if (!flag)
			{
				Console.instance.SubmitCmd(NetworkUser.readOnlyLocalPlayersList[0], cmdTertiary);
			}
		}
		else
		{
			Console.instance.SubmitCmd(NetworkUser.readOnlyLocalPlayersList[0], cmdTertiary);
		}
	}
}
