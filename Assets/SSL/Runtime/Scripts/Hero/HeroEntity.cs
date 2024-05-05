using UnityEngine;
using UnityEngine.Serialization;

public class HeroEntity : MonoBehaviour {
    [Header("Physics")]
    [SerializeField] private Rigidbody2D _rigidbody;

    [Header("Horizontal Movements")]
    
    [FormerlySerializedAs("_movementsSettings")]
    [SerializeField] private HeroHorizontalMovementSettings _groundHorizontalMovementSettings;
    private float _horizontalSpeed = 0f;
    private float _moveDirX = 0f;

    [Header("Dash")]
    [SerializeField] private HeroDashSettings _dashSettings;
    private float _dashTimer = 0f;
    private bool _isDash = false;

    [Header("Orientation")]
    [SerializeField] private Transform _orientVisualRoot;
    private float _orientX = 1f;

    [Header("Vertical Movements")]
    private float _verticalSpeed = 0f;

    [Header("Fall")]
    [SerializeField] private HeroFallSettings _fallSettings;

    [Header("Ground")]
    [SerializeField] private GroundDetector _groundDetector;
    public bool IsTouchingGround { get; private set; } = false;

    [Header("Left Side")]
    [SerializeField] private LeftSideDetector _leftSideDetector;
    public bool IsTouchingLeftSide { get; private set; } = false;

    [Header("Right Side")]
    [SerializeField] private RightSideDetector _rightSideDetector;
    public bool IsTouchingRightSide { get; private set; } = false;

    [Header("Jump")]
    [SerializeField] public HeroAllJumpsSettings[] _allJumpsSettings;
    [SerializeField] private HeroJumpSettings _jumpSettings;
    [SerializeField] private HeroFallSettings _jumpFallSettings;
    [SerializeField] private HeroHorizontalMovementSettings _jumpHorizontalMovementSettings;

    enum JumpState {
        NotJumping,
        JumpImpulsion,
        Falling,
    }
    private JumpState _jumpState = JumpState.NotJumping;
    private float _jumpTimer = 0f;

    [Header("Debug")]
    [SerializeField] private bool _guiDebug = false;

    // Camera Follow
    private CameraFollowable _cameraFollowable;

    public void SetMoveDirX(float dirX) {
        _moveDirX = dirX;
    }

    private void Awake() {
        _cameraFollowable = GetComponent<CameraFollowable>();
        _cameraFollowable.FollowPositionX = _rigidbody.position.x;
        _cameraFollowable.FollowPositionY = _rigidbody.position.y;
    }
    
    private void Update() {
        _UpdateOrientVisual();
        _cameraFollowable.Orientation = _orientX;
    }

    private void FixedUpdate() {
        _ApplyGroundDetection();
        _ApplyLeftSideDetection();
        _ApplyRightSideDetection();
        _UpdateCameraFollowPosition();

        HeroHorizontalMovementSettings HorizontalMovementSettings = _GetCurrentHorizontalMovementSettings();
        
        if (_isDash) {
            if (_dashTimer < _dashSettings.duration) {
                if (IsTouchingLeftSide || IsTouchingRightSide) {
                    _horizontalSpeed = 0f;
                }
                _Dash();
                _dashTimer += Time.fixedDeltaTime;
                return;
            } else {
                _dashTimer = 0f;
                _isDash = false;
                if (_horizontalSpeed != 0f) {
                    _horizontalSpeed = _groundHorizontalMovementSettings.speedMax;
                }
                _jumpState = JumpState.Falling;
            }
        }

        if (_AreOrientAndMovementOpposite()) {
            _TurnBack(HorizontalMovementSettings);
        } else {
            _UpdateHorizontalSpeed(HorizontalMovementSettings);
            _ChangeOrientFromHorizontalMovement();
        }

        if (IsJumping) {
            _UpdateJump();
        } else {
            if (!IsTouchingGround) {
                _ApplyFallGravity(_fallSettings);
            } else {
                _ResetVerticalSpeed();
            }
        }

        _ApplyHorizontalSpeed();
        _ApplyVerticalSpeed();
    }

    private void _ChangeOrientFromHorizontalMovement() {
        if (_moveDirX == 0f) return;
        _orientX = Mathf.Sign(_moveDirX);
    }

    private void _ApplyFallGravity(HeroFallSettings settings) {
        _verticalSpeed -= settings.fallGravity * Time.fixedDeltaTime;
        if (_verticalSpeed < -settings.fallSpeedMax) {
            _verticalSpeed = -settings.fallSpeedMax;
        }
    }

    public void JumpStart() {
        _jumpState = JumpState.JumpImpulsion;
        _jumpTimer = 0f;
    }

    public bool IsJumping => _jumpState != JumpState.NotJumping;

    public void StopJumpImpulsion() {
        _jumpState = JumpState.Falling;
    }

    public bool IsJumpImpulsing => _jumpState == JumpState.JumpImpulsion;
    public bool IsJumpMinDurationReached => _jumpTimer >= _jumpSettings.jumpMinDuration;

    private void _UpdateJumpStateImpulsion() {
        _jumpTimer += Time.fixedDeltaTime;
        if (_jumpTimer < _jumpSettings.jumpMaxDuration) {
            _verticalSpeed = _jumpSettings.jumpSpeed;
        } else {
            _jumpState = JumpState.Falling;
        }
    }

    private void _UpdateJumpStateFalling() {
        if (!IsTouchingGround) {
            _ApplyFallGravity(_jumpFallSettings);
        } else {
            _ResetVerticalSpeed();
            _jumpState = JumpState.NotJumping;
        }
    }

    private void _UpdateJump() {
        switch (_jumpState) {
            case JumpState.JumpImpulsion:
                _UpdateJumpStateImpulsion();
                break;
                
            case JumpState.Falling:
                _UpdateJumpStateFalling();
                break;
        }
    }

    private void _ApplyHorizontalSpeed() {
        Vector2 velocity = _rigidbody.velocity;
        velocity.x = _horizontalSpeed * _orientX;
        _rigidbody.velocity = velocity;
    }

    private void _ApplyVerticalSpeed() {
        Vector2 velocity = _rigidbody.velocity;
        velocity.y = _verticalSpeed;
        _rigidbody.velocity = velocity;
    }

    private void _ApplyGroundDetection() {
        IsTouchingGround = _groundDetector.DetectGroundNearBy();
    }

    private void _ApplyLeftSideDetection() {
        IsTouchingLeftSide = _leftSideDetector.DetectLeftSideNearBy();
    }

    private void _ApplyRightSideDetection() {
        IsTouchingRightSide = _rightSideDetector.DetectRightSideNearBy();
    }

    private void _ResetVerticalSpeed() {
        _verticalSpeed = 0f;
    }

    private void _UpdateOrientVisual() {
        Vector3 newScale = _orientVisualRoot.localScale;
        newScale.x = _orientX;
        _orientVisualRoot.localScale = newScale;
    }

    private void _Accelerate(HeroHorizontalMovementSettings settings) {
        _horizontalSpeed += settings.acceleration * Time.fixedDeltaTime;
        if (_horizontalSpeed > settings.speedMax) {
            _horizontalSpeed = settings.speedMax;
        }
    }

    private void _Decelerate(HeroHorizontalMovementSettings settings) {
        _horizontalSpeed -= settings.deceleration * Time.fixedDeltaTime;
        if (_horizontalSpeed < 0f) {
            _horizontalSpeed = 0f;
        }
    }

    private void _TurnBack(HeroHorizontalMovementSettings settings) {
        _horizontalSpeed -= settings.turnBackFriction* Time.fixedDeltaTime;
        if (_horizontalSpeed < 0f) {
            _horizontalSpeed = 0f;
            _ChangeOrientFromHorizontalMovement();
        }
    }

    private HeroHorizontalMovementSettings _GetCurrentHorizontalMovementSettings() {
        return IsTouchingGround ? _groundHorizontalMovementSettings : _jumpHorizontalMovementSettings;
    }

    private bool _AreOrientAndMovementOpposite() {
        return _moveDirX * _orientX < 0f;
    }

    private void _UpdateHorizontalSpeed(HeroHorizontalMovementSettings settings) {
        if (_moveDirX != 0) {
            _Accelerate(settings);
        } else {
            _Decelerate(settings);
        }
    }

    private void _UpdateCameraFollowPosition() {
        _cameraFollowable.FollowPositionX = _rigidbody.position.x;
        if (IsTouchingGround && !IsJumping) {
            _cameraFollowable.FollowPositionY = _rigidbody.position.y;
        }
    }

    public void _Dash() {
        Vector2 velocity = _rigidbody.velocity;
        velocity.x = _dashSettings.speedMax * _orientX;
        velocity.y = 0f;
        _rigidbody.velocity = velocity;
        _isDash = true;
    }

    public void WallJump() {
        if (IsTouchingLeftSide || IsTouchingRightSide) {
            Vector2 velocity = _rigidbody.velocity;
            velocity.x = -(_orientX);
            velocity.y = _jumpSettings.jumpSpeed * 50;
            _rigidbody.velocity = velocity;
            
            _jumpState = JumpState.JumpImpulsion;
            _jumpTimer = 0f;
            _orientX *= -1f;
        }
    }



    private void OnGUI() {
        if (!_guiDebug) return;

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(gameObject.name);
        GUILayout.Label($"MoveDirX = {_moveDirX}");
        GUILayout.Label($"OrientX = {_orientX}");
        if (IsTouchingGround) {
            GUILayout.Label("OnGround");
        } else {
            GUILayout.Label("OnAir");
        }
        if (IsTouchingLeftSide || IsTouchingRightSide) {
            GUILayout.Label("TouchWall");
        } else {
            GUILayout.Label("TouchAir");
        }
        GUILayout.Label($"JumpState = {_jumpState}");
        GUILayout.Label($"Horizontal Speed = {_horizontalSpeed}");
        GUILayout.Label($"Vertical Speed = {_verticalSpeed}");
        GUILayout.EndVertical();
    }
}