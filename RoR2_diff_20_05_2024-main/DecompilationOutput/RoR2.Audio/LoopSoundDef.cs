using UnityEngine;

namespace RoR2.Audio;

[CreateAssetMenu(menuName = "RoR2/LoopSoundDef")]
public class LoopSoundDef : ScriptableObject
{
	public string startSoundName;

	public string stopSoundName;
}
