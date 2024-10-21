using System.Runtime.InteropServices;
using RoR2.ConVar;
using UnityEngine;

namespace RoR2;

public static class WindowsConVars
{
	private class TimerResolutionConVar : BaseConVar
	{
		private static TimerResolutionConVar instance = new TimerResolutionConVar("timer_resolution", ConVarFlags.Engine, null, "The Windows timer resolution.");

		private TimerResolutionConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			NtSetTimerResolution(BaseConVar.ParseIntInvariant(newValue), setResolution: true, out var currentResolution);
			Debug.LogFormat("{0} set to {1}", name, currentResolution);
		}

		public override string GetString()
		{
			NtQueryTimerResolution(out var _, out var _, out var currentResolution);
			return TextSerialization.ToStringInvariant(currentResolution);
		}
	}

	[DllImport("ntdll.dll", SetLastError = true)]
	private static extern int NtSetTimerResolution(int desiredResolution, bool setResolution, out int currentResolution);

	[DllImport("ntdll.dll", SetLastError = true)]
	private static extern int NtQueryTimerResolution(out int minimumResolution, out int maximumResolution, out int currentResolution);
}
