using UnityEngine;

public class BeginRapidlyActivatingAndDeactivating : MonoBehaviour
{
	public float blinkFrequency = 10f;

	public float delayBeforeBeginningBlinking = 30f;

	public GameObject blinkingRootObject;

	private float fixedAge;

	private float blinkAge;

	private void FixedUpdate()
	{
		fixedAge += Time.fixedDeltaTime;
		if (fixedAge >= delayBeforeBeginningBlinking)
		{
			blinkAge += Time.fixedDeltaTime;
			if (blinkAge >= 1f / blinkFrequency)
			{
				blinkAge -= 1f / blinkFrequency;
				blinkingRootObject.SetActive(!blinkingRootObject.activeSelf);
			}
		}
	}
}
