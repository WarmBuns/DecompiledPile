using UnityEngine;

namespace RoR2.Navigation;

public interface IPosition3Getter<T>
{
	Vector3 GetPosition3(T item);
}
