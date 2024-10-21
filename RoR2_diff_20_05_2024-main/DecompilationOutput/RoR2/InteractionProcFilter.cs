using UnityEngine;

namespace RoR2;

public class InteractionProcFilter : MonoBehaviour
{
	[Tooltip("Whether or not OnInteractionBegin for this object should trigger extra gameplay effects like Fireworks and Squid Polyp.")]
	public bool shouldAllowOnInteractionBeginProc = true;
}
