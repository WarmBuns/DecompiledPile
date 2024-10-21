using UnityEngine;

namespace RoR2;

public class RTPCController : MonoBehaviour
{
	public string akSoundString;

	public string rtpcString;

	public float rtpcValue;

	public bool useCurveInstead;

	public AnimationCurve rtpcValueCurve;

	private float fixedAge;

	private void Start()
	{
		if (akSoundString.Length > 0)
		{
			Util.PlaySound(akSoundString, base.gameObject, rtpcString, rtpcValue);
		}
		else
		{
			AkSoundEngine.SetRTPCValue(rtpcString, rtpcValue, base.gameObject);
		}
	}

	private void FixedUpdate()
	{
		if (useCurveInstead)
		{
			fixedAge += Time.fixedDeltaTime;
			AkSoundEngine.SetRTPCValue(rtpcString, rtpcValueCurve.Evaluate(fixedAge), base.gameObject);
		}
	}
}
