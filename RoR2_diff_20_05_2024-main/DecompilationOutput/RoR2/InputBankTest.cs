using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class InputBankTest : MonoBehaviour
{
	private struct CachedRaycastInfo
	{
		public float time;

		public float fixedTime;

		public bool didHit;

		public RaycastHit hitInfo;

		public float maxDistance;

		public static readonly CachedRaycastInfo empty = new CachedRaycastInfo
		{
			time = float.NegativeInfinity,
			fixedTime = float.NegativeInfinity,
			didHit = false,
			maxDistance = 0f
		};
	}

	public struct ButtonState
	{
		public bool down;

		public bool wasDown;

		public bool hasPressBeenClaimed;

		public bool justReleased
		{
			get
			{
				if (!down)
				{
					return wasDown;
				}
				return false;
			}
		}

		public bool justPressed
		{
			get
			{
				if (down)
				{
					return !wasDown;
				}
				return false;
			}
		}

		public void PushState(bool newState)
		{
			hasPressBeenClaimed &= newState;
			wasDown = down;
			down = newState;
		}
	}

	private CharacterBody characterBody;

	private Vector3 _aimDirection;

	private float lastRaycastTime = float.NegativeInfinity;

	private float lastFixedRaycastTime = float.NegativeInfinity;

	private bool didLastRaycastHit;

	private RaycastHit lastHitInfo;

	private float lastMaxDistance;

	private CachedRaycastInfo cachedRaycast = CachedRaycastInfo.empty;

	public Vector3 moveVector;

	public ButtonState skill1;

	public ButtonState skill2;

	public ButtonState skill3;

	public ButtonState skill4;

	public ButtonState interact;

	public ButtonState jump;

	public ButtonState sprint;

	public ButtonState activateEquipment;

	public ButtonState ping;

	public ButtonState rawMoveUp;

	public ButtonState rawMoveDown;

	public ButtonState rawMoveRight;

	public ButtonState rawMoveLeft;

	public Vector2 rawMoveData;

	public Vector3 aimDirection
	{
		get
		{
			if (!(_aimDirection != Vector3.zero))
			{
				return base.transform.forward;
			}
			return _aimDirection;
		}
		set
		{
			_aimDirection = value.normalized;
		}
	}

	public Vector3 aimOrigin
	{
		get
		{
			if (!characterBody.aimOriginTransform)
			{
				return base.transform.position;
			}
			return characterBody.aimOriginTransform.position;
		}
	}

	public int emoteRequest { get; set; } = -1;

	public Ray GetAimRay()
	{
		return new Ray(aimOrigin, aimDirection);
	}

	public bool GetAimRaycast(float maxDistance, out RaycastHit hitInfo)
	{
		float time = Time.time;
		float fixedTime = Time.fixedTime;
		if (!cachedRaycast.time.Equals(time) || !cachedRaycast.fixedTime.Equals(fixedTime) || (!(cachedRaycast.maxDistance >= maxDistance) && !cachedRaycast.didHit))
		{
			float extraRaycastDistance = 0f;
			Ray ray = CameraRigController.ModifyAimRayIfApplicable(GetAimRay(), base.gameObject, out extraRaycastDistance);
			cachedRaycast = CachedRaycastInfo.empty;
			cachedRaycast.time = time;
			cachedRaycast.fixedTime = fixedTime;
			cachedRaycast.maxDistance = maxDistance;
			cachedRaycast.didHit = Util.CharacterRaycast(base.gameObject, ray, maxDistance: maxDistance + extraRaycastDistance, layerMask: (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, hitInfo: out cachedRaycast.hitInfo, queryTriggerInteraction: QueryTriggerInteraction.Ignore);
		}
		bool flag = cachedRaycast.didHit;
		hitInfo = cachedRaycast.hitInfo;
		if (flag && hitInfo.distance > maxDistance)
		{
			flag = false;
			hitInfo = default(RaycastHit);
		}
		return flag;
	}

	public void SetRawMoveStates(Vector2 _rawMoveData)
	{
		float num = 0.9f;
		float num2 = 0.1f;
		rawMoveUp.PushState((_rawMoveData.y > num && !rawMoveUp.down) || (_rawMoveData.y > num2 && rawMoveUp.down));
		rawMoveDown.PushState((_rawMoveData.y < 0f - num && !rawMoveDown.down) || (_rawMoveData.y < 0f - num2 && rawMoveDown.down));
		rawMoveLeft.PushState((_rawMoveData.x < 0f - num && !rawMoveLeft.down) || (_rawMoveData.x < 0f - num2 && rawMoveLeft.down));
		rawMoveRight.PushState((_rawMoveData.x > num && !rawMoveRight.down) || (_rawMoveData.x > num2 && rawMoveRight.down));
		rawMoveData = _rawMoveData;
	}

	public bool CheckAnyButtonDown()
	{
		if (!skill1.down && !skill2.down && !skill3.down && !skill4.down && !interact.down && !jump.down && !sprint.down && !activateEquipment.down)
		{
			return ping.down;
		}
		return true;
	}

	private void Awake()
	{
		characterBody = GetComponent<CharacterBody>();
	}

	private void Start()
	{
		if (characterBody.hasEffectiveAuthority)
		{
			if ((bool)characterBody.characterDirection)
			{
				aimDirection = characterBody.characterDirection.forward;
			}
			else
			{
				aimDirection = base.transform.forward;
			}
		}
	}
}
