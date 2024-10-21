using RoR2;
using UnityEngine;

namespace EntityStates.MinorConstruct;

public class FindSurface : NoCastSpawn
{
	[SerializeField]
	public int raycastCount;

	[SerializeField]
	public float maxRaycastLength;

	public override void OnEnter()
	{
		RaycastHit hitInfo = default(RaycastHit);
		Vector3 corePosition = base.characterBody.corePosition;
		if (base.isAuthority && !(base.characterBody.master.minionOwnership?.ownerMaster))
		{
			for (int i = 0; i < raycastCount; i++)
			{
				if (Physics.Raycast(corePosition, Random.onUnitSphere, out hitInfo, maxRaycastLength, LayerIndex.world.mask))
				{
					base.transform.position = hitInfo.point;
					base.transform.up = hitInfo.normal;
				}
			}
		}
		base.OnEnter();
	}
}
