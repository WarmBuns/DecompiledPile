using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(QuaternionPID))]
[RequireComponent(typeof(VectorPID))]
public class RigidbodyDirection : MonoBehaviour
{
	public Vector3 aimDirection = Vector3.one;

	public Rigidbody rigid;

	public QuaternionPID angularVelocityPID;

	public VectorPID torquePID;

	public bool freezeXRotation;

	public bool freezeYRotation;

	public bool freezeZRotation;

	private ModelLocator modelLocator;

	private Animator animator;

	public string animatorXCycle;

	public string animatorYCycle;

	public string animatorZCycle;

	private int animatorXCycleIndex;

	private int animatorYCycleIndex;

	private int animatorZCycleIndex;

	public float animatorTorqueScale;

	private InputBankTest inputBank;

	private Vector3 targetTorque;

	private RendererVisiblity visibility;

	private void Start()
	{
		inputBank = GetComponent<InputBankTest>();
		modelLocator = GetComponent<ModelLocator>();
		if ((bool)modelLocator)
		{
			visibility = modelLocator.modelVisibility;
			Transform modelTransform = modelLocator.modelTransform;
			if ((bool)modelTransform)
			{
				animator = modelTransform.GetComponent<Animator>();
				if ((bool)animator)
				{
					animatorXCycleIndex = Animator.StringToHash(animatorXCycle);
					animatorYCycleIndex = Animator.StringToHash(animatorYCycle);
					animatorZCycleIndex = Animator.StringToHash(animatorZCycle);
				}
			}
		}
		aimDirection = base.transform.forward;
	}

	private void Update()
	{
		if ((bool)animator)
		{
			if (animatorXCycle.Length > 0)
			{
				animator.SetFloat(animatorXCycleIndex, Mathf.Clamp(0.5f + targetTorque.x * 0.5f * animatorTorqueScale, -1f, 1f), 0.1f, Time.deltaTime);
			}
			if (animatorYCycle.Length > 0)
			{
				animator.SetFloat(animatorYCycleIndex, Mathf.Clamp(0.5f + targetTorque.y * 0.5f * animatorTorqueScale, -1f, 1f), 0.1f, Time.deltaTime);
			}
			if (animatorZCycle.Length > 0)
			{
				animator.SetFloat(animatorZCycleIndex, Mathf.Clamp(0.5f + targetTorque.z * 0.5f * animatorTorqueScale, -1f, 1f), 0.1f, Time.deltaTime);
			}
		}
	}

	private void FixedUpdate()
	{
		if ((bool)inputBank && (bool)rigid && (bool)angularVelocityPID && (bool)torquePID)
		{
			angularVelocityPID.inputQuat = rigid.rotation;
			Quaternion targetQuat = Util.QuaternionSafeLookRotation(aimDirection);
			if (freezeXRotation)
			{
				targetQuat.x = 0f;
			}
			if (freezeYRotation)
			{
				targetQuat.y = 0f;
			}
			if (freezeZRotation)
			{
				targetQuat.z = 0f;
			}
			angularVelocityPID.targetQuat = targetQuat;
			Vector3 targetVector = angularVelocityPID.UpdatePID();
			torquePID.inputVector = rigid.angularVelocity;
			torquePID.targetVector = targetVector;
			Vector3 torque = torquePID.UpdatePID();
			rigid.AddTorque(torque, ForceMode.Acceleration);
		}
	}
}
