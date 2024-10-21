namespace RoR2.Networking;

public class UmsgType
{
	public const short Lowest = 48;

	public const short SetEntityState = 48;

	public const short PlayerFireProjectile = 49;

	public const short ReleaseProjectilePredictionId = 50;

	public const short CharacterTransformUpdate = 51;

	public const short Effect = 52;

	public const short BulletDamage = 53;

	public const short UpdateTime = 54;

	public const short CreateExpEffect = 55;

	public const short ResetSkills = 56;

	public const short PickupItem = 57;

	public const short StatsUpdate = 58;

	public const short BroadcastChat = 59;

	public const short DamageDealt = 60;

	public const short Heal = 61;

	public const short ConstructTurret = 62;

	public const short AmmoPackPickup = 63;

	public const short Test = 64;

	public const short Ping = 65;

	public const short PingResponse = 66;

	public const short Kick = 67;

	public const short Teleport = 68;

	public const short SetJetpackJumpState = 69;

	public const short PreGameRuleVoteControllerSendClientVotes = 70;

	public const short OverlapAttackHits = 71;

	public const short EmitPointSound = 72;

	public const short EmitEntitySound = 73;

	public const short SetClientAuth = 74;

	public const short ReportBlastAttackDamage = 75;

	public const short NetworkUIPromptMessage = 76;

	public const short ReportMercFocusedAssaultHitReplaceMeLater = 77;

	public const short TransformPickup = 78;

	public const short Pause = 79;

	public const short TransmitWokDataToServer = 80;

	public const short Highest = 80;
}
