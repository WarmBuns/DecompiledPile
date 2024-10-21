using Tayx.Graphy;
using Tayx.Graphy.Utils;
using UnityEngine;

namespace RoR2;

public class RoRGraphyControl
{
	private static RoRGraphyControl instance;

	private bool specsActive;

	private bool fpsActive;

	[ConCommand(commandName = "graphy_specs", flags = ConVarFlags.None, helpText = "Graphy testing.")]
	public static void graphySpecs(ConCommandArgs args)
	{
		if (instance.specsActive)
		{
			G_Singleton<GraphyManager>.Instance.SetModuleMode(GraphyManager.ModuleType.ADVANCED, GraphyManager.ModuleState.OFF);
			instance.specsActive = false;
		}
		else
		{
			G_Singleton<GraphyManager>.Instance.SetModuleMode(GraphyManager.ModuleType.ADVANCED, GraphyManager.ModuleState.FULL);
			instance.specsActive = true;
		}
		G_Singleton<GraphyManager>.Instance.SetModuleMode(GraphyManager.ModuleType.AUDIO, GraphyManager.ModuleState.OFF);
	}

	[ConCommand(commandName = "graphy_init", flags = ConVarFlags.None, helpText = "Graphy testing.")]
	public static void verifyInst(ConCommandArgs args)
	{
		if (instance == null)
		{
			instance = new RoRGraphyControl();
			Object.DontDestroyOnLoad(Object.Instantiate(Resources.Load("ror2Graphy") as GameObject));
			instance.specsActive = false;
			instance.fpsActive = false;
		}
	}

	[ConCommand(commandName = "graphy_fps", flags = ConVarFlags.None, helpText = "Graphy testing.")]
	public static void graphyFPS(ConCommandArgs args)
	{
		if (instance.fpsActive)
		{
			G_Singleton<GraphyManager>.Instance.SetModuleMode(GraphyManager.ModuleType.FPS, GraphyManager.ModuleState.OFF);
			G_Singleton<GraphyManager>.Instance.SetModuleMode(GraphyManager.ModuleType.RAM, GraphyManager.ModuleState.OFF);
			instance.fpsActive = false;
		}
		else
		{
			G_Singleton<GraphyManager>.Instance.SetModuleMode(GraphyManager.ModuleType.FPS, GraphyManager.ModuleState.FULL);
			G_Singleton<GraphyManager>.Instance.SetModuleMode(GraphyManager.ModuleType.RAM, GraphyManager.ModuleState.FULL);
			instance.fpsActive = true;
		}
		G_Singleton<GraphyManager>.Instance.SetModuleMode(GraphyManager.ModuleType.AUDIO, GraphyManager.ModuleState.OFF);
	}
}
