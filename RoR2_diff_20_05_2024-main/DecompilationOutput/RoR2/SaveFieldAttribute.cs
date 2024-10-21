using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rewired;
using UnityEngine;

namespace RoR2;

public class SaveFieldAttribute : Attribute
{
	[Serializable]
	public struct HardwareMapEntry
	{
		public string HWGuid;

		public string XMLMap;
	}

	[Serializable]
	public struct HardwareMapWrapper
	{
		public HardwareMapEntry[] Entries;
	}

	public Action<UserProfile, string> setter;

	public Func<UserProfile, string> getter;

	public Action<UserProfile, UserProfile> copier;

	public string defaultValue = string.Empty;

	public string fieldName;

	public string explicitSetupMethod;

	private FieldInfo fieldInfo;

	public void Setup(FieldInfo fieldInfo)
	{
		this.fieldInfo = fieldInfo;
		Type fieldType = fieldInfo.FieldType;
		fieldName = fieldInfo.Name;
		if (explicitSetupMethod != null)
		{
			MethodInfo method = typeof(SaveFieldAttribute).GetMethod(explicitSetupMethod, BindingFlags.Instance | BindingFlags.Public);
			if (method == null)
			{
				Debug.LogErrorFormat("Explicit setup {0} specified by field {1} could not be found. Use the nameof() operator to ensure the method exists.");
			}
			else if (method.GetParameters().Length > 1)
			{
				Debug.LogErrorFormat("Explicit setup method {0} for field {1} must have one parameter.", explicitSetupMethod, fieldInfo.Name);
			}
			else
			{
				method.Invoke(this, new object[1] { fieldInfo });
			}
		}
		else if (fieldType == typeof(string))
		{
			SetupString(fieldInfo);
		}
		else if (fieldType == typeof(int))
		{
			SetupInt(fieldInfo);
		}
		else if (fieldType == typeof(uint))
		{
			SetupUint(fieldInfo);
		}
		else if (fieldType == typeof(float))
		{
			SetupFloat(fieldInfo);
		}
		else if (fieldType == typeof(bool))
		{
			SetupBool(fieldInfo);
		}
		else if (fieldType == typeof(SurvivorDef))
		{
			SetupSurvivorDef(fieldInfo);
		}
		else
		{
			Debug.LogErrorFormat("No explicit setup method or supported type for save field {0}", fieldInfo.Name);
		}
	}

	public void SetupString(FieldInfo fieldInfo)
	{
		getter = (UserProfile userProfile) => (string)fieldInfo.GetValue(userProfile);
		setter = delegate(UserProfile userProfile, string valueString)
		{
			fieldInfo.SetValue(userProfile, valueString);
		};
		copier = delegate(UserProfile srcProfile, UserProfile destProfile)
		{
			fieldInfo.SetValue(destProfile, fieldInfo.GetValue(srcProfile));
		};
	}

	public void SetupFloat(FieldInfo fieldInfo)
	{
		getter = (UserProfile userProfile) => TextSerialization.ToStringInvariant((float)fieldInfo.GetValue(userProfile));
		setter = delegate(UserProfile userProfile, string valueString)
		{
			if (TextSerialization.TryParseInvariant(valueString, out float result))
			{
				fieldInfo.SetValue(userProfile, result);
			}
		};
		copier = delegate(UserProfile srcProfile, UserProfile destProfile)
		{
			fieldInfo.SetValue(destProfile, fieldInfo.GetValue(srcProfile));
		};
	}

	public void SetupInt(FieldInfo fieldInfo)
	{
		getter = (UserProfile userProfile) => TextSerialization.ToStringInvariant((int)fieldInfo.GetValue(userProfile));
		setter = delegate(UserProfile userProfile, string valueString)
		{
			if (TextSerialization.TryParseInvariant(valueString, out int result))
			{
				fieldInfo.SetValue(userProfile, result);
			}
		};
		copier = delegate(UserProfile srcProfile, UserProfile destProfile)
		{
			fieldInfo.SetValue(destProfile, fieldInfo.GetValue(srcProfile));
		};
	}

	public void SetupUint(FieldInfo fieldInfo)
	{
		getter = (UserProfile userProfile) => TextSerialization.ToStringInvariant((uint)fieldInfo.GetValue(userProfile));
		setter = delegate(UserProfile userProfile, string valueString)
		{
			if (TextSerialization.TryParseInvariant(valueString, out uint result))
			{
				fieldInfo.SetValue(userProfile, result);
			}
		};
		copier = delegate(UserProfile srcProfile, UserProfile destProfile)
		{
			fieldInfo.SetValue(destProfile, fieldInfo.GetValue(srcProfile));
		};
	}

	public void SetupBool(FieldInfo fieldInfo)
	{
		getter = (UserProfile userProfile) => (!(bool)fieldInfo.GetValue(userProfile)) ? "0" : "1";
		setter = delegate(UserProfile userProfile, string valueString)
		{
			if (TextSerialization.TryParseInvariant(valueString, out int result))
			{
				fieldInfo.SetValue(userProfile, result > 0);
			}
		};
		copier = delegate(UserProfile srcProfile, UserProfile destProfile)
		{
			fieldInfo.SetValue(destProfile, fieldInfo.GetValue(srcProfile));
		};
	}

	public void SetupTokenList(FieldInfo fieldInfo)
	{
		getter = (UserProfile userProfile) => string.Join(" ", (List<string>)fieldInfo.GetValue(userProfile));
		setter = delegate(UserProfile userProfile, string valueString)
		{
			List<string> list = (List<string>)fieldInfo.GetValue(userProfile);
			list.Clear();
			string[] array = valueString.Split(' ');
			foreach (string item in array)
			{
				list.Add(item);
			}
		};
		copier = delegate(UserProfile srcProfile, UserProfile destProfile)
		{
			List<string> src = (List<string>)fieldInfo.GetValue(srcProfile);
			List<string> dest = (List<string>)fieldInfo.GetValue(destProfile);
			Util.CopyList(src, dest);
		};
	}

	public void SetupPickupsSet(FieldInfo fieldInfo)
	{
		getter = delegate(UserProfile userProfile)
		{
			bool[] pickupsSet = (bool[])fieldInfo.GetValue(userProfile);
			return string.Join(" ", from pickupDef in PickupCatalog.allPickups
				where pickupsSet[pickupDef.pickupIndex.value]
				select pickupDef.internalName);
		};
		setter = delegate(UserProfile userProfile, string valueString)
		{
			bool[] array = (bool[])fieldInfo.GetValue(userProfile);
			Array.Clear(array, 0, 0);
			string[] array2 = valueString.Split(' ');
			for (int i = 0; i < array2.Length; i++)
			{
				PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(array2[i]);
				if (pickupIndex.isValid)
				{
					array[pickupIndex.value] = true;
				}
			}
		};
		copier = delegate(UserProfile srcProfile, UserProfile destProfile)
		{
			bool[] sourceArray = (bool[])fieldInfo.GetValue(srcProfile);
			bool[] array3 = (bool[])fieldInfo.GetValue(destProfile);
			Array.Copy(sourceArray, array3, array3.Length);
		};
	}

	public void SetupSurvivorDef(FieldInfo fieldInfo)
	{
		getter = delegate(UserProfile userProfile)
		{
			SurvivorDef survivorDef = (SurvivorDef)fieldInfo.GetValue(userProfile);
			return (!survivorDef) ? SurvivorCatalog.defaultSurvivor.cachedName : survivorDef.cachedName;
		};
		setter = delegate(UserProfile userProfile, string valueString)
		{
			SurvivorDef value = SurvivorCatalog.FindSurvivorDef(valueString);
			fieldInfo.SetValue(userProfile, value);
		};
		copier = DefaultCopier;
	}

	public void SetupKeyboardMap(FieldInfo fieldInfo)
	{
		SetupControllerMap(fieldInfo, ControllerType.Keyboard);
	}

	public void SetupMouseMap(FieldInfo fieldInfo)
	{
		SetupControllerMap(fieldInfo, ControllerType.Mouse);
	}

	public void SetupJoystickMap(FieldInfo fieldInfo)
	{
		SetupControllerMap(fieldInfo, ControllerType.Joystick);
	}

	private void SetupControllerMap(FieldInfo fieldInfo, ControllerType controllerType)
	{
		getter = (UserProfile userProfile) => ((ControllerMap)fieldInfo.GetValue(userProfile))?.ToXmlString() ?? string.Empty;
		setter = delegate(UserProfile userProfile, string valueString)
		{
			fieldInfo.SetValue(userProfile, ControllerMap.CreateFromXml(controllerType, valueString));
		};
		copier = delegate(UserProfile srcProfile, UserProfile destProfile)
		{
			switch (controllerType)
			{
			case ControllerType.Joystick:
				fieldInfo.SetValue(destProfile, new JoystickMap((JoystickMap)fieldInfo.GetValue(srcProfile)));
				break;
			case ControllerType.Keyboard:
				fieldInfo.SetValue(destProfile, new KeyboardMap((KeyboardMap)fieldInfo.GetValue(srcProfile)));
				break;
			case ControllerType.Mouse:
				fieldInfo.SetValue(destProfile, new MouseMap((MouseMap)fieldInfo.GetValue(srcProfile)));
				break;
			default:
				throw new NotImplementedException();
			}
		};
	}

	public void SetupJoystickHardwareMap(FieldInfo fieldInfo)
	{
		getter = delegate(UserProfile userProfile)
		{
			DefaultControllerMaps.RewiredDebugLog("SetupJoystickHardwareMap.getter");
			Dictionary<Guid, JoystickMap> dictionary = (Dictionary<Guid, JoystickMap>)fieldInfo.GetValue(userProfile);
			if (dictionary != null)
			{
				List<HardwareMapEntry> list = new List<HardwareMapEntry>();
				foreach (KeyValuePair<Guid, JoystickMap> item2 in dictionary)
				{
					HardwareMapEntry item = default(HardwareMapEntry);
					item.HWGuid = item2.Key.ToString();
					item.XMLMap = ((item2.Value != null) ? item2.Value.ToXmlString() : "");
					list.Add(item);
				}
				HardwareMapWrapper hardwareMapWrapper = default(HardwareMapWrapper);
				hardwareMapWrapper.Entries = list.ToArray();
				string text = JsonUtility.ToJson(hardwareMapWrapper);
				DefaultControllerMaps.RewiredDebugLog("\tReturning '{0}'", text);
				return text;
			}
			return string.Empty;
		};
		setter = delegate(UserProfile userProfile, string valueString)
		{
			DefaultControllerMaps.RewiredDebugLog("SetupJoystickHardwareMap.setter> {0}", valueString);
			HardwareMapWrapper hardwareMapWrapper2 = JsonUtility.FromJson<HardwareMapWrapper>(valueString);
			Dictionary<Guid, JoystickMap> dictionary2 = new Dictionary<Guid, JoystickMap>();
			HardwareMapEntry[] entries = hardwareMapWrapper2.Entries;
			for (int i = 0; i < entries.Length; i++)
			{
				HardwareMapEntry hardwareMapEntry = entries[i];
				Guid key = Guid.Parse(hardwareMapEntry.HWGuid);
				string xMLMap = hardwareMapEntry.XMLMap;
				if (ControllerMap.CreateFromXml(ControllerType.Joystick, xMLMap) is JoystickMap joystickMap)
				{
					DefaultControllerMaps.RewiredDebugLog("Serialized Joystick Map for {0}", key.ToString());
					LocalUser.DumpControllerMap(joystickMap);
					dictionary2.Add(key, joystickMap);
				}
			}
			fieldInfo.SetValue(userProfile, dictionary2);
		};
		copier = delegate(UserProfile srcProfile, UserProfile destProfile)
		{
			DefaultControllerMaps.RewiredDebugLog("SetupJoystickHardwareMap.copier");
			fieldInfo.SetValue(destProfile, new Dictionary<Guid, JoystickMap>((Dictionary<Guid, JoystickMap>)fieldInfo.GetValue(srcProfile)));
		};
	}

	private void DefaultCopier(UserProfile srcProfile, UserProfile destProfile)
	{
		fieldInfo.SetValue(destProfile, fieldInfo.GetValue(srcProfile));
	}
}
