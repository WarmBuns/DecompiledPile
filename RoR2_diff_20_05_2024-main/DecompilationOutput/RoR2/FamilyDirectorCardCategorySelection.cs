using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/DCCS/FamilyDirectorCardCategorySelection")]
public class FamilyDirectorCardCategorySelection : DirectorCardCategorySelection
{
	private const float chatMessageDelaySeconds = 1f;

	[SerializeField]
	private string selectionChatString;

	[SerializeField]
	[Tooltip("The minimum (inclusive) number of stages COMPLETED (not reached) before this family event becomes available.")]
	private int minimumStageCompletion = 1;

	[SerializeField]
	[Tooltip("The maximum (exclusive) number of stages COMPLETED (not reached) before this family event becomes unavailable.")]
	private int maximumStageCompletion = int.MaxValue;

	public override bool IsAvailable()
	{
		if ((bool)Run.instance && Run.instance.canFamilyEventTrigger && minimumStageCompletion <= Run.instance.stageClearCount)
		{
			return maximumStageCompletion > Run.instance.stageClearCount;
		}
		return false;
	}

	public override void OnSelected(ClassicStageInfo stageInfo)
	{
		if (!string.IsNullOrEmpty(selectionChatString))
		{
			stageInfo.StartCoroutine(stageInfo.BroadcastFamilySelection(selectionChatString));
		}
	}
}
