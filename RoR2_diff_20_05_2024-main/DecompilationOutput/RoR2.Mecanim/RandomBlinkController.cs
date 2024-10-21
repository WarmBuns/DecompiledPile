using UnityEngine;

namespace RoR2.Mecanim;

public class RandomBlinkController : MonoBehaviour
{
	public Animator animator;

	public string[] blinkTriggers;

	public float blinkChancePerUpdate;

	private float stopwatch;

	private const float updateFrequency = 4f;

	private void FixedUpdate()
	{
		stopwatch += Time.fixedDeltaTime;
		if (!(stopwatch >= 0.25f))
		{
			return;
		}
		stopwatch = 0f;
		for (int i = 0; i < blinkTriggers.Length; i++)
		{
			if (Util.CheckRoll(blinkChancePerUpdate))
			{
				animator.SetTrigger(blinkTriggers[i]);
			}
		}
	}
}
