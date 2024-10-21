using UnityEngine;

namespace RoR2;

public interface IHologramContentProvider
{
	bool ShouldDisplayHologram(GameObject viewer);

	GameObject GetHologramContentPrefab();

	void UpdateHologramContent(GameObject hologramContentObject, Transform viewerBody);
}
