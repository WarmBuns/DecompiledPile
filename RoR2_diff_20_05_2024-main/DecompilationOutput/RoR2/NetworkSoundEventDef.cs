using RoR2.Audio;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/NetworkSoundEventDef")]
public class NetworkSoundEventDef : ScriptableObject
{
	public string eventName;

	public NetworkSoundEventIndex index { get; set; }

	public uint akId { get; set; }
}
