using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/Infinite Tower/Infinite Tower Wave Count Prerequisites")]
public class InfiniteTowerWaveCountPrerequisites : InfiniteTowerWavePrerequisites
{
	[SerializeField]
	private int minimumWaveCount;

	public override bool AreMet(InfiniteTowerRun run)
	{
		return run.waveIndex >= minimumWaveCount;
	}
}
