using UnityEngine;

namespace RoR2;

public class MeridianReportBossFinalPosition : MonoBehaviour
{
	private void Start()
	{
		MeridianEventTriggerInteraction.bossTransform = base.transform;
	}
}
