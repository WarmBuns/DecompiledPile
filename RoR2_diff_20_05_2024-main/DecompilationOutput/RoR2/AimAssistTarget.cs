using System.Collections.Generic;
using RoR2.ConVar;
using UnityEngine;

namespace RoR2;

public class AimAssistTarget : MonoBehaviour
{
	public Transform point0;

	public Transform point1;

	public float assistScale = 1f;

	public HealthComponent healthComponent;

	public TeamComponent teamComponent;

	public static List<AimAssistTarget> instancesList = new List<AimAssistTarget>();

	public static FloatConVar debugAimAssistVisualCoefficient = new FloatConVar("debug_aim_assist_visual_coefficient", ConVarFlags.None, "2", "Magic for debug visuals. Don't touch.");

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	private void FixedUpdate()
	{
		MyFixedUpdate(Time.fixedDeltaTime);
	}

	private void MyFixedUpdate(float deltaTime)
	{
		if ((bool)healthComponent && !healthComponent.alive)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void OnDrawGizmos()
	{
		if ((bool)point0)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(point0.position, 1f * assistScale * CameraRigController.aimStickAssistMinSize.value * debugAimAssistVisualCoefficient.value);
			Gizmos.color = Color.white;
			Gizmos.DrawWireSphere(point0.position, 1f * assistScale * CameraRigController.aimStickAssistMaxSize.value * CameraRigController.aimStickAssistMinSize.value * debugAimAssistVisualCoefficient.value);
		}
		if ((bool)point1)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(point1.position, 1f * assistScale * CameraRigController.aimStickAssistMinSize.value * debugAimAssistVisualCoefficient.value);
			Gizmos.color = Color.white;
			Gizmos.DrawWireSphere(point1.position, 1f * assistScale * CameraRigController.aimStickAssistMaxSize.value * CameraRigController.aimStickAssistMinSize.value * debugAimAssistVisualCoefficient.value);
		}
		if ((bool)point0 && (bool)point1)
		{
			Gizmos.DrawLine(point0.position, point1.position);
		}
	}
}
