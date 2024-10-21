using UnityEngine;

namespace RoR2;

public class EnableOnMecanimFloat : MonoBehaviour
{
	public Animator animator;

	[Tooltip("The name of the mecanim variable to compare against")]
	public string animatorString;

	[Tooltip("The minimum value at which the objects are enabled")]
	public float minFloatValue;

	[Tooltip("The maximum value at which the objects are enabled")]
	public float maxFloatValue;

	public GameObject[] objectsToEnable;

	public GameObject[] objectsToDisable;

	private bool wasWithinRange;

	private void Update()
	{
		if (!animator)
		{
			return;
		}
		float @float = animator.GetFloat(animatorString);
		bool flag = Mathf.Clamp(@float, minFloatValue, maxFloatValue) == @float;
		if (flag != wasWithinRange)
		{
			GameObject[] array = objectsToEnable;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(flag);
			}
			array = objectsToDisable;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(!flag);
			}
			wasWithinRange = flag;
		}
	}
}
