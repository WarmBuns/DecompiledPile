using UnityEngine;

namespace RoR2.DirectionalSearch;

public interface IGenericWorldSearchSelector<TSource>
{
	Transform GetTransform(TSource source);

	GameObject GetRootObject(TSource source);
}
