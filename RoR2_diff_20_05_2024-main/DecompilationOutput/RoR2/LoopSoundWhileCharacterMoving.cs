using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
[RequireComponent(typeof(CharacterMotor))]
public class LoopSoundWhileCharacterMoving : MonoBehaviour
{
	[SerializeField]
	private string startSoundName;

	[SerializeField]
	private string stopSoundName;

	[SerializeField]
	private float minSpeed;

	[SerializeField]
	private bool requireGrounded;

	[SerializeField]
	private bool disableWhileSprinting;

	[SerializeField]
	private bool applyScale;

	private CharacterBody body;

	private CharacterMotor motor;

	private bool isLooping;

	private uint soundId;

	private void Start()
	{
		isLooping = false;
		body = GetComponent<CharacterBody>();
		motor = GetComponent<CharacterMotor>();
	}

	private void OnDestroy()
	{
		if (isLooping)
		{
			StopLoop();
		}
	}

	private void FixedUpdate()
	{
		if (!body || !motor)
		{
			return;
		}
		if (isLooping)
		{
			float magnitude = motor.velocity.magnitude;
			if (applyScale)
			{
				float num = body.moveSpeed;
				float num2 = num;
				if (!body.isSprinting)
				{
					num *= body.sprintingSpeedMultiplier;
				}
				else
				{
					num2 /= body.sprintingSpeedMultiplier;
				}
				float in_value = Mathf.Lerp(0f, 50f, magnitude / num2) + Mathf.Lerp(0f, 50f, (magnitude - num2) / (num - num2));
				AkSoundEngine.SetRTPCValueByPlayingID("charMultSpeed", in_value, soundId);
			}
			if ((body.isSprinting && disableWhileSprinting) || (requireGrounded && !motor.isGrounded) || magnitude < minSpeed)
			{
				StopLoop();
			}
		}
		else if ((!body.isSprinting || !disableWhileSprinting) && (!requireGrounded || motor.isGrounded) && motor.velocity.sqrMagnitude >= minSpeed)
		{
			StartLoop();
		}
	}

	private void StartLoop()
	{
		soundId = Util.PlaySound(startSoundName, base.gameObject);
		isLooping = true;
	}

	private void StopLoop()
	{
		Util.PlaySound(stopSoundName, base.gameObject);
		isLooping = false;
	}
}
