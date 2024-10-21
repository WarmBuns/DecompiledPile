using RoR2.ConVar;
using UnityEngine;

namespace RoR2;

public static class EngineConVars
{
	private class SyncPhysicsConVar : BaseConVar
	{
		public static SyncPhysicsConVar instance = new SyncPhysicsConVar("sync_physics", ConVarFlags.None, "0", "Enable/disables Physics 'autosyncing' between moves.");

		private SyncPhysicsConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			Physics.autoSyncTransforms = BaseConVar.ParseBoolInvariant(newValue);
		}

		public override string GetString()
		{
			if (!Physics.autoSyncTransforms)
			{
				return "0";
			}
			return "1";
		}
	}

	private class AutoSimulatePhysicsConVar : BaseConVar
	{
		public static AutoSimulatePhysicsConVar instance = new AutoSimulatePhysicsConVar("auto_simulate_physics", ConVarFlags.None, "1", "Enable/disables Physics autosimulate.");

		private AutoSimulatePhysicsConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			Physics.autoSimulation = BaseConVar.ParseBoolInvariant(newValue);
		}

		public override string GetString()
		{
			if (!Physics.autoSimulation)
			{
				return "0";
			}
			return "1";
		}
	}

	private class TimeScaleConVar : BaseConVar
	{
		private static readonly TimeScaleConVar instance = new TimeScaleConVar("timescale", ConVarFlags.ExecuteOnServer | ConVarFlags.Cheat | ConVarFlags.Engine, null, "The timescale of the game.");

		public TimeScaleConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			Time.timeScale = BaseConVar.ParseFloatInvariant(newValue);
		}

		public override string GetString()
		{
			return TextSerialization.ToStringInvariant(Time.timeScale);
		}
	}

	private class TimeStepConVar : BaseConVar
	{
		private static readonly TimeStepConVar instance = new TimeStepConVar("timestep", ConVarFlags.ExecuteOnServer | ConVarFlags.Cheat | ConVarFlags.Engine, null, "The timestep of the game.");

		public TimeStepConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			Time.fixedDeltaTime = BaseConVar.ParseFloatInvariant(newValue);
		}

		public override string GetString()
		{
			return TextSerialization.ToStringInvariant(Time.fixedDeltaTime);
		}
	}
}
