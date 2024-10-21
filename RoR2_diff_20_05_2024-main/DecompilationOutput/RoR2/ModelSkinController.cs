using UnityEngine;

namespace RoR2;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterModel))]
public class ModelSkinController : MonoBehaviour
{
	public SkinDef[] skins;

	private CharacterModel characterModel;

	public int currentSkinIndex { get; private set; } = -1;

	private void Awake()
	{
		characterModel = GetComponent<CharacterModel>();
	}

	private void Start()
	{
		if ((bool)characterModel.body)
		{
			ApplySkin((int)characterModel.body.skinIndex);
		}
	}

	public void ApplySkin(int skinIndex)
	{
		if (skinIndex != currentSkinIndex && (uint)skinIndex < skins.Length)
		{
			skins[skinIndex].Apply(base.gameObject);
			characterModel.forceUpdate = true;
			currentSkinIndex = skinIndex;
		}
	}
}
