using Generics.Dynamics;
using UnityEngine;

namespace RoR2;

public class IKSetWeightFromAnimatorFloat : MonoBehaviour
{
	public Animator animator;

	public string animatorFloat;

	public InverseKinematics ik;

	private void Update()
	{
		float @float = animator.GetFloat(animatorFloat);
		Core.Chain[] otherChains = ik.otherChains;
		for (int i = 0; i < otherChains.Length; i++)
		{
			otherChains[i].weight = @float;
		}
	}
}
