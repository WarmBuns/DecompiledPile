using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(Image))]
public class SimpleSpriteAnimator : MonoBehaviour
{
	public SimpleSpriteAnimation animation;

	private Image target;

	private int frame;

	private int tick;

	private float tickStopwatch;

	private void Awake()
	{
		target = GetComponent<Image>();
	}

	private void FixedUpdate()
	{
		if ((bool)animation && animation.frames.Length != 0)
		{
			tickStopwatch += Time.fixedDeltaTime;
			float num = 1f / animation.frameRate;
			if (tickStopwatch > num)
			{
				tickStopwatch -= num;
				Tick();
			}
		}
	}

	private void Tick()
	{
		tick++;
		if (frame >= animation.frames.Length)
		{
			frame = 0;
			tick = 0;
		}
		if (animation.frames[frame].duration <= tick)
		{
			frame++;
			if (frame >= animation.frames.Length)
			{
				frame = 0;
				tick = 0;
			}
			ref SimpleSpriteAnimation.Frame reference = ref animation.frames[frame];
			target.sprite = reference.sprite;
		}
	}
}
