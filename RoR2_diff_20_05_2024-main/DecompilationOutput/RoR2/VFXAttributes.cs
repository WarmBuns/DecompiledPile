using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RoR2;

public class VFXAttributes : MonoBehaviour
{
	public enum VFXPriority
	{
		Low,
		Medium,
		Always
	}

	public enum VFXIntensity
	{
		Low,
		Medium,
		High
	}

	private static List<VFXAttributes> vfxList = new List<VFXAttributes>();

	private static ReadOnlyCollection<VFXAttributes> _readonlyVFXList = new ReadOnlyCollection<VFXAttributes>(vfxList);

	[Tooltip("Controls whether or not a VFX appears at all - consider if you would notice if this entire VFX never appeared. Also means it has a networking consequence.")]
	public VFXPriority vfxPriority;

	[Tooltip("Define how expensive a particle system is IF it appears.")]
	public VFXIntensity vfxIntensity;

	[Tooltip("If true, do not use pooling for this effect.")]
	public bool DoNotPool;

	[Tooltip("If true, do not allow the system to cull this effect's pool.")]
	public bool DoNotCullPool;

	public Light[] optionalLights;

	[Tooltip("Particle systems that may be deactivated without impacting gameplay.")]
	public ParticleSystem[] secondaryParticleSystem;

	public static ReadOnlyCollection<VFXAttributes> readonlyVFXList => _readonlyVFXList;

	public int GetIntensityScore()
	{
		return vfxIntensity switch
		{
			VFXIntensity.Low => 1, 
			VFXIntensity.Medium => 5, 
			VFXIntensity.High => 25, 
			_ => 0, 
		};
	}

	public void OnEnable()
	{
		vfxList.Add(this);
		VFXBudget.totalCost += GetIntensityScore();
	}

	public void OnDisable()
	{
		vfxList.Remove(this);
		VFXBudget.totalCost -= GetIntensityScore();
	}
}
