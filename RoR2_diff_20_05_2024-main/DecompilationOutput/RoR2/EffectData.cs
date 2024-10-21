using System.Runtime.CompilerServices;
using HG;
using JetBrains.Annotations;
using RoR2.Audio;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class EffectData
{
	private Vector3 _origin;

	public Quaternion rotation = defaultRotation;

	public float scale = defaultScale;

	public Color32 color = defaultColor;

	public Vector3 start = defaultStart;

	public SurfaceDefIndex surfaceDefIndex = defaultSurfaceDefIndex;

	public uint genericUInt = defaultGenericUInt;

	public float genericFloat = defaultGenericFloat;

	public bool genericBool = defaultGenericBool;

	public NetworkSoundEventIndex networkSoundEventIndex = defaultNetworkSoundEventIndex;

	private static readonly uint useNonDefaultRotationFlag = 1u;

	private static readonly uint useNonDefaultRootObjectFlag = 2u;

	private static readonly uint useNonDefaultModelChildIndexFlag = 4u;

	private static readonly uint useNonDefaultScaleFlag = 8u;

	private static readonly uint useNonDefaultColorFlag = 16u;

	private static readonly uint useNonDefaultStartFlag = 32u;

	private static readonly uint useNonDefaultSurfaceDefIndexFlag = 64u;

	private static readonly uint useNonDefaultGenericUIntFlag = 128u;

	private static readonly uint useNonDefaultGenericFloatFlag = 256u;

	private static readonly uint useNonDefaultGenericBoolFlag = 512u;

	private static readonly uint useNonDefaultNetworkSoundEventIndexFlag = 1024u;

	private static readonly Quaternion defaultRotation = Quaternion.identity;

	private static readonly GameObject defaultRootObject = null;

	private static readonly short defaultModelChildIndex = -1;

	private static readonly float defaultScale = 1f;

	private static readonly Color32 defaultColor = Color.white;

	private static readonly Vector3 defaultStart = Vector3.zero;

	private static readonly SurfaceDefIndex defaultSurfaceDefIndex = SurfaceDefIndex.Invalid;

	private static readonly uint defaultGenericUInt = 0u;

	private static readonly float defaultGenericFloat = 0f;

	private static readonly bool defaultGenericBool = false;

	public bool forceUnpooled;

	private static readonly NetworkSoundEventIndex defaultNetworkSoundEventIndex = NetworkSoundEventIndex.Invalid;

	public Vector3 origin
	{
		get
		{
			return _origin;
		}
		set
		{
			if (!Util.PositionIsValid(value))
			{
				Debug.LogFormat("EffectData.origin assignment position is invalid! Position={0}", value);
			}
			else
			{
				_origin = value;
			}
		}
	}

	public GameObject rootObject { get; private set; } = defaultRootObject;

	public short modelChildIndex { get; private set; } = defaultModelChildIndex;

	public static void Copy([NotNull] EffectData src, [NotNull] EffectData dest)
	{
		dest.origin = src.origin;
		dest.rotation = src.rotation;
		dest.rootObject = src.rootObject;
		dest.modelChildIndex = src.modelChildIndex;
		dest.scale = src.scale;
		dest.color = src.color;
		dest.start = src.start;
		dest.surfaceDefIndex = src.surfaceDefIndex;
		dest.genericUInt = src.genericUInt;
		dest.genericFloat = src.genericFloat;
		dest.genericBool = src.genericBool;
		dest.networkSoundEventIndex = src.networkSoundEventIndex;
		dest.forceUnpooled = src.forceUnpooled;
	}

	public void Reset()
	{
		_origin = Vector3.zero;
		rotation = Quaternion.identity;
		rootObject = null;
		modelChildIndex = -1;
		scale = 1f;
		color = Color.white;
		start = Vector3.zero;
		genericUInt = 0u;
		genericFloat = 0f;
		genericBool = false;
		forceUnpooled = false;
	}

	public void SetNetworkedObjectReference(GameObject networkedObject)
	{
		rootObject = networkedObject;
		modelChildIndex = -1;
	}

	public GameObject ResolveNetworkedObjectReference()
	{
		return rootObject;
	}

	public void SetHurtBoxReference(HurtBox hurtBox)
	{
		if (!hurtBox || !hurtBox.healthComponent)
		{
			rootObject = null;
			modelChildIndex = -1;
		}
		else
		{
			rootObject = hurtBox.healthComponent.gameObject;
			modelChildIndex = hurtBox.indexInGroup;
		}
	}

	public void SetHurtBoxReference(GameObject gameObject)
	{
		HurtBox hurtBox = gameObject?.GetComponent<HurtBox>();
		if ((bool)hurtBox)
		{
			SetHurtBoxReference(hurtBox);
			return;
		}
		rootObject = gameObject;
		modelChildIndex = -1;
	}

	public GameObject ResolveHurtBoxReference()
	{
		if (modelChildIndex == -1)
		{
			return rootObject;
		}
		if (!rootObject)
		{
			return null;
		}
		ModelLocator component = rootObject.GetComponent<ModelLocator>();
		if (!component)
		{
			return null;
		}
		Transform modelTransform = component.modelTransform;
		if (!modelTransform)
		{
			return null;
		}
		HurtBoxGroup component2 = modelTransform.GetComponent<HurtBoxGroup>();
		if (!component2)
		{
			return null;
		}
		HurtBox safe = ArrayUtils.GetSafe(component2.hurtBoxes, modelChildIndex);
		if (!safe)
		{
			return null;
		}
		return safe.gameObject;
	}

	public void SetChildLocatorTransformReference(GameObject rootObject, int childIndex)
	{
		this.rootObject = rootObject;
		modelChildIndex = (short)childIndex;
	}

	public Transform ResolveChildLocatorTransformReference()
	{
		if ((bool)rootObject)
		{
			if (modelChildIndex == -1)
			{
				return rootObject.transform;
			}
			ModelLocator component = rootObject.GetComponent<ModelLocator>();
			if ((bool)component && (bool)component.modelTransform)
			{
				ChildLocator component2 = component.modelTransform.GetComponent<ChildLocator>();
				if ((bool)component2)
				{
					return component2.FindChild(modelChildIndex);
				}
			}
		}
		return null;
	}

	public EffectData Clone()
	{
		EffectData effectData = new EffectData();
		Copy(this, effectData);
		return effectData;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ColorEquals(in Color32 x, in Color32 y)
	{
		if (x.r == y.r && x.g == y.g && x.b == y.b)
		{
			return x.a == y.a;
		}
		return false;
	}

	public void Serialize(NetworkWriter writer)
	{
		uint num = 0u;
		bool num2 = !rotation.Equals(defaultRotation);
		bool flag = rootObject != defaultRootObject;
		bool flag2 = modelChildIndex != defaultModelChildIndex;
		bool flag3 = scale != defaultScale;
		bool flag4 = !ColorEquals(in color, in defaultColor);
		bool flag5 = !start.Equals(defaultStart);
		bool flag6 = surfaceDefIndex != defaultSurfaceDefIndex;
		bool flag7 = genericUInt != defaultGenericUInt;
		bool flag8 = genericFloat != defaultGenericFloat;
		bool flag9 = genericBool != defaultGenericBool;
		bool flag10 = networkSoundEventIndex != defaultNetworkSoundEventIndex;
		if (num2)
		{
			num |= useNonDefaultRotationFlag;
		}
		if (flag)
		{
			num |= useNonDefaultRootObjectFlag;
		}
		if (flag2)
		{
			num |= useNonDefaultModelChildIndexFlag;
		}
		if (flag3)
		{
			num |= useNonDefaultScaleFlag;
		}
		if (flag4)
		{
			num |= useNonDefaultColorFlag;
		}
		if (flag5)
		{
			num |= useNonDefaultStartFlag;
		}
		if (flag6)
		{
			num |= useNonDefaultSurfaceDefIndexFlag;
		}
		if (flag7)
		{
			num |= useNonDefaultGenericUIntFlag;
		}
		if (flag8)
		{
			num |= useNonDefaultGenericFloatFlag;
		}
		if (flag9)
		{
			num |= useNonDefaultGenericBoolFlag;
		}
		if (flag10)
		{
			num |= useNonDefaultNetworkSoundEventIndexFlag;
		}
		writer.WritePackedUInt32(num);
		writer.Write(origin);
		if (num2)
		{
			writer.Write(rotation);
		}
		if (flag)
		{
			writer.Write(rootObject);
		}
		if (flag2)
		{
			writer.Write((byte)(modelChildIndex + 1));
		}
		if (flag3)
		{
			writer.Write(scale);
		}
		if (flag4)
		{
			writer.Write(color);
		}
		if (flag5)
		{
			writer.Write(start);
		}
		if (flag6)
		{
			writer.WritePackedIndex32((int)surfaceDefIndex);
		}
		if (flag7)
		{
			writer.WritePackedUInt32(genericUInt);
		}
		if (flag8)
		{
			writer.Write(genericFloat);
		}
		if (flag9)
		{
			writer.Write(genericBool);
		}
		if (flag10)
		{
			writer.WriteNetworkSoundEventIndex(networkSoundEventIndex);
		}
		writer.Write(forceUnpooled);
	}

	public void Deserialize(NetworkReader reader)
	{
		uint flags = reader.ReadPackedUInt32();
		bool flag = HasFlag(in useNonDefaultRotationFlag);
		bool flag2 = HasFlag(in useNonDefaultRootObjectFlag);
		bool flag3 = HasFlag(in useNonDefaultModelChildIndexFlag);
		bool flag4 = HasFlag(in useNonDefaultScaleFlag);
		bool flag5 = HasFlag(in useNonDefaultColorFlag);
		bool flag6 = HasFlag(in useNonDefaultStartFlag);
		bool flag7 = HasFlag(in useNonDefaultSurfaceDefIndexFlag);
		bool flag8 = HasFlag(in useNonDefaultGenericUIntFlag);
		bool flag9 = HasFlag(in useNonDefaultGenericFloatFlag);
		bool flag10 = HasFlag(in useNonDefaultGenericBoolFlag);
		bool flag11 = HasFlag(in useNonDefaultNetworkSoundEventIndexFlag);
		origin = reader.ReadVector3();
		rotation = (flag ? reader.ReadQuaternion() : defaultRotation);
		rootObject = (flag2 ? reader.ReadGameObject() : defaultRootObject);
		modelChildIndex = (flag3 ? ((short)(reader.ReadByte() - 1)) : defaultModelChildIndex);
		scale = (flag4 ? reader.ReadSingle() : defaultScale);
		color = (flag5 ? reader.ReadColor32() : defaultColor);
		start = (flag6 ? reader.ReadVector3() : defaultStart);
		surfaceDefIndex = (flag7 ? ((SurfaceDefIndex)reader.ReadPackedIndex32()) : defaultSurfaceDefIndex);
		genericUInt = (flag8 ? reader.ReadPackedUInt32() : defaultGenericUInt);
		genericFloat = (flag9 ? reader.ReadSingle() : defaultGenericFloat);
		genericBool = (flag10 ? reader.ReadBoolean() : defaultGenericBool);
		networkSoundEventIndex = (flag11 ? reader.ReadNetworkSoundEventIndex() : defaultNetworkSoundEventIndex);
		forceUnpooled = reader.ReadBoolean();
		bool HasFlag(in uint mask)
		{
			return (flags & mask) != 0;
		}
	}
}
