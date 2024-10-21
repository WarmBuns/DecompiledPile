using HG;
using UnityEngine;

namespace RoR2.Audio;

public static class LoopSoundManager
{
	public struct SoundLoopPtr
	{
		public readonly ValueHeap<SoundLoopNode>.Ptr ptr;

		public bool isValid => soundLoopHeap.PtrIsValid(in ptr);

		public SoundLoopPtr(ValueHeap<SoundLoopNode>.Ptr ptr)
		{
			this.ptr = ptr;
		}

		public SoundLoopNode GetValue()
		{
			return soundLoopHeap.GetValue(in ptr);
		}

		public void SetValue(in SoundLoopNode value)
		{
			soundLoopHeap.SetValue(in ptr, in value);
		}

		public ref SoundLoopNode GetRef()
		{
			return ref soundLoopHeap.GetRef(ptr);
		}

		public void SetRtpc(string rtpcName, float value)
		{
			AkSoundEngine.SetRTPCValueByPlayingID(rtpcName, value, GetRef().akId);
		}
	}

	public class SoundLoopHelper : MonoBehaviour
	{
		public SoundLoopPtr first { get; set; }

		public SoundLoopPtr last { get; set; }

		public GameObject cachedGameObject { get; set; }

		private void OnDestroy()
		{
			while (first.isValid)
			{
				StopSoundLoopLocal(first);
			}
		}
	}

	public struct SoundLoopNode
	{
		public SoundLoopHelper owner;

		public LoopSoundDef loopSoundDef;

		public uint akId;

		public SoundLoopPtr next;

		public SoundLoopPtr previous;
	}

	private static readonly ValueHeap<SoundLoopNode> soundLoopHeap = new ValueHeap<SoundLoopNode>(128u);

	public static SoundLoopPtr PlaySoundLoopLocal(GameObject gameObject, LoopSoundDef loopSoundDef)
	{
		gameObject.GetComponent<AkGameObj>();
		SoundLoopHelper soundLoopHelper = gameObject.GetComponent<SoundLoopHelper>();
		if (!soundLoopHelper)
		{
			soundLoopHelper = gameObject.AddComponent<SoundLoopHelper>();
			soundLoopHelper.cachedGameObject = soundLoopHelper.gameObject;
		}
		SoundLoopPtr soundLoopPtr = new SoundLoopPtr(soundLoopHeap.Alloc());
		SoundLoopPtr last = soundLoopHelper.last;
		ref SoundLoopNode @ref = ref soundLoopPtr.GetRef();
		@ref.owner = soundLoopHelper;
		@ref.loopSoundDef = loopSoundDef;
		if (soundLoopHelper.last.isValid)
		{
			@ref.previous = last;
			last.GetRef().next = soundLoopPtr;
		}
		else
		{
			soundLoopHelper.first = soundLoopPtr;
		}
		soundLoopHelper.last = soundLoopPtr;
		@ref.akId = AkSoundEngine.PostEvent(loopSoundDef.startSoundName, gameObject);
		return soundLoopPtr;
	}

	public static SoundLoopPtr PlaySoundLoopLocalRtpc(GameObject gameObject, LoopSoundDef loopSoundDef, string rtpcName, float rtpcValue)
	{
		SoundLoopPtr result = PlaySoundLoopLocal(gameObject, loopSoundDef);
		ref SoundLoopNode @ref = ref result.GetRef();
		if (@ref.akId != 0)
		{
			AkSoundEngine.SetRTPCValueByPlayingID(rtpcName, rtpcValue, @ref.akId);
		}
		return result;
	}

	public static void StopSoundLoopLocal(SoundLoopPtr ptr)
	{
		if (ptr.isValid)
		{
			ref SoundLoopNode @ref = ref ptr.GetRef();
			AkSoundEngine.PostEvent(@ref.loopSoundDef.stopSoundName, @ref.owner.cachedGameObject);
			if (@ref.previous.isValid)
			{
				@ref.previous.GetRef().next = @ref.next;
			}
			else
			{
				@ref.owner.first = @ref.next;
			}
			if (@ref.next.isValid)
			{
				@ref.next.GetRef().previous = @ref.previous;
			}
			else
			{
				@ref.owner.last = @ref.previous;
			}
			soundLoopHeap.Free(in ptr.ptr);
		}
	}
}
