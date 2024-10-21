using RoR2.ConVar;
using UnityEngine;

namespace RoR2;

public static class VFXBudget
{
	public static int totalCost = 0;

	public static int framerateCost = 0;

	public static int realTotalCost = 0;

	public static int costPerLostFrame = 15;

	private static IntConVar lowPriorityCostThreshold = new IntConVar("vfxbudget_low_priority_cost_threshold", ConVarFlags.None, "50", "");

	private static IntConVar mediumPriorityCostThreshold = new IntConVar("vfxbudget_medium_priority_cost_threshold", ConVarFlags.None, "200", "");

	private static IntConVar particleCostBias = new IntConVar("vfxbudget_particle_cost_bias", ConVarFlags.Archive, "0", "");

	private static float chanceFailurePower = 1f;

	public static bool CanAffordSpawn(GameObject prefab)
	{
		return CanAffordSpawn(prefab.GetComponent<VFXAttributes>());
	}

	public static bool CanAffordSpawn(VFXAttributes vfxAttributes)
	{
		if ((object)vfxAttributes == null)
		{
			return true;
		}
		int intensityScore = vfxAttributes.GetIntensityScore();
		realTotalCost = totalCost + intensityScore + particleCostBias.value;
		return vfxAttributes.vfxPriority switch
		{
			VFXAttributes.VFXPriority.Low => Mathf.Pow((float)lowPriorityCostThreshold.value / (float)realTotalCost, chanceFailurePower) > Random.value, 
			VFXAttributes.VFXPriority.Medium => Mathf.Pow((float)mediumPriorityCostThreshold.value / (float)realTotalCost, chanceFailurePower) > Random.value, 
			VFXAttributes.VFXPriority.Always => true, 
			_ => true, 
		};
	}
}
