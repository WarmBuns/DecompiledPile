using System;
using System.Collections.Generic;
using UnityEngine;

public static class RendererSetMaterialsExtension
{
	private static Material[][] sharedMaterialArrays;

	private static void InitSharedMaterialsArrays(int maxMaterials)
	{
		sharedMaterialArrays = new Material[maxMaterials + 1][];
		if (maxMaterials > 0)
		{
			sharedMaterialArrays[0] = Array.Empty<Material>();
			for (int i = 1; i < sharedMaterialArrays.Length; i++)
			{
				sharedMaterialArrays[i] = new Material[i];
			}
		}
	}

	static RendererSetMaterialsExtension()
	{
		InitSharedMaterialsArrays(16);
	}

	public static void SetSharedMaterials(this Renderer renderer, Material[] sharedMaterials, int count)
	{
		if (sharedMaterialArrays.Length < count)
		{
			InitSharedMaterialsArrays(count);
		}
		Material[] array = sharedMaterialArrays[count];
		Array.Copy(sharedMaterials, array, count);
		renderer.sharedMaterials = array;
		Array.Clear(array, 0, count);
	}

	public static void SetSharedMaterials(this Renderer renderer, List<Material> sharedMaterials)
	{
		int count = sharedMaterials.Count;
		if (sharedMaterialArrays.Length < count)
		{
			InitSharedMaterialsArrays(count);
		}
		Material[] array = sharedMaterialArrays[count];
		sharedMaterials.CopyTo(array, 0);
		renderer.sharedMaterials = array;
		Array.Clear(array, 0, count);
	}

	public static void SetMaterials(this Renderer renderer, Material[] materials, int count)
	{
		if (sharedMaterialArrays.Length < count)
		{
			InitSharedMaterialsArrays(count);
		}
		Material[] array = sharedMaterialArrays[count];
		Array.Copy(materials, array, count);
		renderer.materials = array;
		Array.Clear(array, 0, count);
	}

	public static void SetMaterials(this Renderer renderer, List<Material> materials)
	{
		int count = materials.Count;
		if (sharedMaterialArrays.Length < count)
		{
			InitSharedMaterialsArrays(count);
		}
		Material[] array = sharedMaterialArrays[count];
		materials.CopyTo(array, 0);
		renderer.materials = array;
		Array.Clear(array, 0, count);
	}
}
