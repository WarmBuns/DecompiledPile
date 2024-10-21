using UnityEngine;

namespace RoR2.UI.SkinControllers;

[ExecuteAlways]
public abstract class BaseSkinController : MonoBehaviour
{
	public UISkinData skinData;

	protected abstract void OnSkinUI();

	protected void Awake()
	{
		if ((bool)skinData)
		{
			DoSkinUI();
		}
	}

	private void DoSkinUI()
	{
		if ((bool)skinData)
		{
			OnSkinUI();
		}
	}
}
