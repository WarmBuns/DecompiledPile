using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DestroyOnSoundEnd : MonoBehaviour
{
	private AudioSource audioSource;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	private void Update()
	{
		if (!audioSource.isPlaying)
		{
			Object.Destroy(base.gameObject);
		}
	}
}
