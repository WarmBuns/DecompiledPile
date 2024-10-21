namespace RoR2;

public abstract class VoiceChatManager
{
	public abstract bool IsVoiceChatEnabled { get; }

	public abstract PlatformID LocalUser { get; }

	public abstract bool IsPlayerLocalMuted(PlatformID id);

	public abstract bool IsPlayerMicAccessAvailable(PlatformID id);

	public abstract bool IsPlayerSpeaking(PlatformID id);

	public abstract bool IsPlayerBlocked(PlatformID id);

	public abstract void ToggleMutePlayer(PlatformID id);

	public virtual void SetMuteForPlayer(PlatformID id, bool muted)
	{
	}
}
