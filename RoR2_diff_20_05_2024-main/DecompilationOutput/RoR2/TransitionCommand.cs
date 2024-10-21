namespace RoR2;

public static class TransitionCommand
{
	private static float timer;

	private static string commandString;

	public static bool requestPending { get; private set; }

	private static void Update()
	{
		if (FadeToBlackManager.fullyFaded)
		{
			RoR2Application.onUpdate -= Update;
			requestPending = false;
			FadeToBlackManager.fadeCount--;
			string cmd = commandString;
			commandString = null;
			Console.instance.SubmitCmd(null, cmd);
		}
	}

	[ConCommand(commandName = "transition_command", flags = ConVarFlags.None, helpText = "Fade out and execute a command at the end of the fadeout.")]
	private static void CCTransitionCommand(ConCommandArgs args)
	{
		args.CheckArgumentCount(1);
		if (!requestPending)
		{
			requestPending = true;
			commandString = args[0];
			FadeToBlackManager.fadeCount++;
			RoR2Application.onUpdate += Update;
		}
	}

	public static void ForceClearFadeToBlack()
	{
		RoR2Application.onUpdate -= Update;
		requestPending = false;
		commandString = null;
	}
}
