using System;
using UnityEngine.Events;

namespace RoR2.UI;

[Serializable]
public class PickupPickerEvent : UnityEvent<MPButton, PickupDef>
{
}
