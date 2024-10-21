using UnityEngine;

namespace RoR2.CharacterAI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BaseAI))]
public class AIOwnership : MonoBehaviour
{
	public CharacterMaster ownerMaster;

	private BaseAI baseAI;

	private void Awake()
	{
		baseAI = GetComponent<BaseAI>();
	}

	private void FixedUpdate()
	{
		if ((bool)ownerMaster)
		{
			baseAI.leader.gameObject = ownerMaster.GetBodyObject();
		}
	}
}
