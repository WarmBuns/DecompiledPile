using UnityEngine;

namespace RoR2;

public class TriggerIntroMusicStart : MonoBehaviour
{
	private void OnEnable()
	{
		MusicController.Instance.StartIntroMusic();
		Debug.Log("<color=green>Triggered MusicController</color>");
	}
}
