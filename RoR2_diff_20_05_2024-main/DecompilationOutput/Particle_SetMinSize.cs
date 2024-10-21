using UnityEngine;

public class Particle_SetMinSize : MonoBehaviour
{
	public float minimumPixelCoverage = 2f;

	public float defaultMinimumParticleSize;

	[SerializeField]
	private bool _isEnabled = true;

	private void Start()
	{
		UpdateMinSize();
	}

	public void SetEnabled(bool isEnabled)
	{
		_isEnabled = isEnabled;
		UpdateMinSize();
	}

	private void UpdateMinSize()
	{
		GetComponent<ParticleSystemRenderer>().minParticleSize = (_isEnabled ? (minimumPixelCoverage / (float)Screen.width) : defaultMinimumParticleSize);
	}
}
