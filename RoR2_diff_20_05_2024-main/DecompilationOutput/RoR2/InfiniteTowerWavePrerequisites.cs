using UnityEngine;

namespace RoR2;

public abstract class InfiniteTowerWavePrerequisites : ScriptableObject
{
	public abstract bool AreMet(InfiniteTowerRun run);
}
