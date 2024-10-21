using System;
using RoR2.UI;
using UnityEngine.Networking;

namespace RoR2;

public static class QuitConfirmationHelper
{
	private enum NetworkStatus
	{
		None,
		SinglePlayer,
		Client,
		Host
	}

	private static bool IsQuitConfirmationRequired()
	{
		if ((bool)Run.instance && !GameOverController.instance)
		{
			return true;
		}
		return false;
	}

	public static void IssueQuitCommand(NetworkUser sender, string consoleCmd)
	{
		IssueQuitCommand(RunCmd);
		void RunCmd()
		{
			Console.instance.SubmitCmd(sender, consoleCmd);
		}
	}

	public static void IssueQuitCommand(Action action)
	{
		if (!IsQuitConfirmationRequired())
		{
			action();
			return;
		}
		NetworkStatus networkStatus = ((NetworkUser.readOnlyInstancesList.Count <= NetworkUser.readOnlyLocalPlayersList.Count) ? NetworkStatus.SinglePlayer : ((!NetworkServer.active) ? NetworkStatus.Client : NetworkStatus.Host));
		string text = "";
		text = networkStatus switch
		{
			NetworkStatus.None => "", 
			NetworkStatus.SinglePlayer => "QUIT_RUN_CONFIRM_DIALOG_BODY_SINGLEPLAYER", 
			NetworkStatus.Client => "QUIT_RUN_CONFIRM_DIALOG_BODY_CLIENT", 
			NetworkStatus.Host => "QUIT_RUN_CONFIRM_DIALOG_BODY_HOST", 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		MPEventSystem owner = null;
		foreach (MPEventSystem readOnlyInstances in MPEventSystem.readOnlyInstancesList)
		{
			if (readOnlyInstances.player.id == PauseManager.PausingPlayerID)
			{
				owner = readOnlyInstances;
				break;
			}
		}
		SimpleDialogBox simpleDialogBox = SimpleDialogBox.Create(owner);
		simpleDialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("QUIT_RUN_CONFIRM_DIALOG_TITLE");
		simpleDialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair(text);
		simpleDialogBox.AddActionButton(action.Invoke, "DIALOG_OPTION_YES", true);
		simpleDialogBox.AddCancelButton("CANCEL");
	}

	[ConCommand(commandName = "quit_confirmed_command", flags = ConVarFlags.None, helpText = "Runs the command provided in the argument only if the user confirms they want to quit the current game via dialog UI.")]
	private static void CCQuitConfirmedCommand(ConCommandArgs args)
	{
		NetworkUser sender = args.sender;
		string consoleCmd = args[0];
		IssueQuitCommand(RunCmd);
		void RunCmd()
		{
			Console.instance.SubmitCmd(sender, consoleCmd);
		}
	}
}
