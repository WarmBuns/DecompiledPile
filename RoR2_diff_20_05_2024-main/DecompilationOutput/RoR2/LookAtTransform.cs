using System;
using UnityEngine;

namespace RoR2;

[ExecuteAlways]
public class LookAtTransform : MonoBehaviour
{
	public enum Axis
	{
		Right,
		Left,
		Up,
		Down,
		Forward,
		Backward
	}

	public Transform target;

	public Axis axis = Axis.Forward;

	private void LateUpdate()
	{
		if (!target)
		{
			return;
		}
		Vector3 vector = target.position - base.transform.position;
		if (!(vector == Vector3.zero))
		{
			switch (axis)
			{
			case Axis.Right:
				base.transform.right = vector;
				break;
			case Axis.Left:
				base.transform.right = -vector;
				break;
			case Axis.Up:
				base.transform.up = vector;
				break;
			case Axis.Down:
				base.transform.right = -vector;
				break;
			case Axis.Forward:
				base.transform.forward = vector;
				break;
			case Axis.Backward:
				base.transform.forward = -vector;
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}
}
