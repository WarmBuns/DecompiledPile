using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2.UI;

public class UIInputPassthrough : MonoBehaviour
{
	[FormerlySerializedAs("FilterAllButMovement")]
	[SerializeField]
	public bool OnlyAllowMovement = true;
}
