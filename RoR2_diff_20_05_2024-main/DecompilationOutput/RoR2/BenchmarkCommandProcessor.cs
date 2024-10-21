namespace RoR2;

public static class BenchmarkCommandProcessor
{
	private static string targetScene;

	[ConCommand(commandName = "benchmark", flags = (ConVarFlags.ExecuteOnServer | ConVarFlags.Cheat), helpText = "Load first argument as scene, disables baddies, and spawns test prefab for baddies.")]
	public static void CCBenchmark(ConCommandArgs args)
	{
	}
}
