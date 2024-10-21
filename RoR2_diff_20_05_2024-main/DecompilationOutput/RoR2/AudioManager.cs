using System;
using System.Reflection;
using RoR2.ConVar;
using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(AkGameObj))]
public class AudioManager : MonoBehaviour
{
	private class VolumeConVar : BaseConVar
	{
		private readonly string rtpcName;

		private string fallbackString = "100";

		public VolumeConVar(string name, ConVarFlags flags, string defaultValue, string helpText, string rtpcName)
			: base(name, flags, defaultValue, helpText)
		{
			this.rtpcName = rtpcName;
		}

		public override void SetString(string newValue)
		{
			if (!AkSoundEngine.IsInitialized())
			{
				return;
			}
			fallbackString = newValue;
			if (TextSerialization.TryParseInvariant(newValue, out float result))
			{
				if (rtpcName == cvVolumeMaster.rtpcName)
				{
					float num = float.Parse(cvVolumeParentMsx.GetString());
					num *= result / 100f;
					cvVolumeMsx.fallbackString = num.ToString();
					AkSoundEngine.SetRTPCValue(cvVolumeMsx.rtpcName, Mathf.Clamp(num, 0f, 100f));
				}
				else if (rtpcName == cvVolumeParentMsx.rtpcName)
				{
					float num2 = float.Parse(cvVolumeMaster.fallbackString);
					float value = result * (num2 / 100f);
					cvVolumeMsx.fallbackString = value.ToString();
					AkSoundEngine.SetRTPCValue(cvVolumeMsx.rtpcName, Mathf.Clamp(value, 0f, 100f));
					return;
				}
				AkSoundEngine.SetRTPCValue(rtpcName, Mathf.Clamp(result, 0f, 100f));
			}
		}

		public override string GetString()
		{
			int io_rValueType = 1;
			if (AkSoundEngine.GetRTPCValue(rtpcName, ulong.MaxValue, 0u, out var out_rValue, ref io_rValueType) == AKRESULT.AK_Success)
			{
				return TextSerialization.ToStringInvariant(out_rValue);
			}
			return fallbackString;
		}
	}

	private class AudioFocusedOnlyConVar : BaseConVar
	{
		private class ApplicationFocusListener : MonoBehaviour
		{
			public Action<bool> onApplicationFocus;

			private void OnApplicationFocus(bool focus)
			{
				onApplicationFocus?.Invoke(focus);
			}
		}

		private static AudioFocusedOnlyConVar instance = new AudioFocusedOnlyConVar("audio_focused_only", ConVarFlags.Archive | ConVarFlags.Engine, null, "Whether or not audio should mute when focus is lost.");

		private bool onlyPlayWhenFocused;

		private bool isFocused;

		private static MethodInfo akSoundEngineController_ActivateAudio_methodInfo = typeof(AkSoundEngineController).GetMethod("ActivateAudio", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		public AudioFocusedOnlyConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
			isFocused = Application.isFocused;
			RoR2Application.onUpdate += SearchForAkInitializer;
		}

		public override void SetString(string newValue)
		{
			if (TextSerialization.TryParseInvariant(newValue, out int result))
			{
				onlyPlayWhenFocused = result != 0;
				Refresh();
			}
		}

		public override string GetString()
		{
			if (!onlyPlayWhenFocused)
			{
				return "0";
			}
			return "1";
		}

		private void OnApplicationFocus(bool focus)
		{
			isFocused = focus;
			Refresh();
		}

		private void Refresh()
		{
			bool flag = !isFocused && onlyPlayWhenFocused;
			bool flag2 = false;
			AkSoundEngineController akSoundEngineController = AkSoundEngineController.Instance;
			if (akSoundEngineController != null)
			{
				AkSoundEngineController_ActivateAudio(akSoundEngineController, !flag, !flag2);
			}
		}

		private static void AkSoundEngineController_ActivateAudio(AkSoundEngineController akSoundEngineController, bool activate, bool renderAnyway)
		{
			akSoundEngineController_ActivateAudio_methodInfo.Invoke(akSoundEngineController, new object[2] { activate, renderAnyway });
		}

		private void SearchForAkInitializer()
		{
			AkInitializer akInitializer = (AkInitializer)akInitializerMsInstanceField.GetValue(null);
			if ((bool)akInitializer)
			{
				RoR2Application.onUpdate -= SearchForAkInitializer;
				ApplicationFocusListener applicationFocusListener = akInitializer.gameObject.AddComponent<ApplicationFocusListener>();
				applicationFocusListener.onApplicationFocus = (Action<bool>)Delegate.Combine(applicationFocusListener.onApplicationFocus, new Action<bool>(OnApplicationFocus));
				Refresh();
			}
		}
	}

	private class WwiseLogEnabledConVar : BaseConVar
	{
		private static WwiseLogEnabledConVar instance = new WwiseLogEnabledConVar("wwise_log_enabled", ConVarFlags.Archive | ConVarFlags.Engine, null, "Wwise logging. 0 = disabled 1 = enabled");

		private WwiseLogEnabledConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			if (!TextSerialization.TryParseInvariant(newValue, out int result))
			{
				return;
			}
			AkInitializer akInitializer = akInitializerMsInstanceField.GetValue(null) as AkInitializer;
			if ((bool)akInitializer)
			{
				AkCallbackManager.InitializationSettings initializationSettings = akInitializer.InitializationSettings?.CallbackManagerInitializationSettings;
				if (initializationSettings != null)
				{
					initializationSettings.IsLoggingEnabled = result != 0;
				}
			}
		}

		public override string GetString()
		{
			AkInitializer akInitializer = akInitializerMsInstanceField.GetValue(null) as AkInitializer;
			if ((bool)akInitializer && akInitializer.InitializationSettings?.CallbackManagerInitializationSettings != null)
			{
				if (!akInitializer.InitializationSettings.CallbackManagerInitializationSettings.IsLoggingEnabled)
				{
					return "0";
				}
				return "1";
			}
			return "1";
		}
	}

	private AkGameObj akGameObj;

	private static VolumeConVar cvVolumeMaster;

	private static VolumeConVar cvVolumeSfx;

	private static VolumeConVar cvVolumeMsx;

	private static VolumeConVar cvVolumeParentMsx;

	private static readonly FieldInfo akInitializerMsInstanceField;

	public static AudioManager instance { get; private set; }

	public static event Action<AudioManager> onAwakeGlobal;

	private void Awake()
	{
		instance = this;
		akGameObj = GetComponent<AkGameObj>();
		AudioManager.onAwakeGlobal?.Invoke(this);
	}

	static AudioManager()
	{
		cvVolumeMaster = new VolumeConVar("volume_master", ConVarFlags.Archive | ConVarFlags.Engine, "100", "The master volume of the game audio, from 0 to 100.", "Volume_Master");
		cvVolumeSfx = new VolumeConVar("volume_sfx", ConVarFlags.Archive | ConVarFlags.Engine, "100", "The volume of sound effects, from 0 to 100.", "Volume_SFX");
		cvVolumeMsx = new VolumeConVar("volume_music", ConVarFlags.Archive | ConVarFlags.Engine, "100", "The music volume, from 0 to 100.", "Volume_MSX");
		cvVolumeParentMsx = new VolumeConVar("parent_volume_music", ConVarFlags.Archive, "100", "The parent music volume, from 0 to 100.", "Parent_Volume_MSX");
		akInitializerMsInstanceField = typeof(AkInitializer).GetField("ms_Instance", BindingFlags.Static | BindingFlags.NonPublic);
		PauseManager.onPauseStartGlobal = (Action)Delegate.Combine(PauseManager.onPauseStartGlobal, (Action)delegate
		{
			AkSoundEngine.PostEvent("Pause_All", ulong.MaxValue);
		});
		PauseManager.onPauseEndGlobal = (Action)Delegate.Combine(PauseManager.onPauseEndGlobal, (Action)delegate
		{
			AkSoundEngine.PostEvent("Unpause_All", ulong.MaxValue);
		});
	}
}
