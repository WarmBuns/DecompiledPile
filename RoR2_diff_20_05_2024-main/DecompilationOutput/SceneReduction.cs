using UnityEngine;

public class SceneReduction : MonoBehaviour
{
	public enum ReductionSeverity
	{
		LOW,
		MEDIUM,
		HIGH
	}

	public static ReductionSeverity TargetReduction;

	public ReductionSeverity reductionSeverity;
}
