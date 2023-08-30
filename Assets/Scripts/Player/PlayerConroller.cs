using UnityEngine;

public class PlayerConroller : MonoBehaviour
{
#region Variables

    [Header("Components")]
    [SerializeField] private Transform tf;
    [SerializeField] private Rigidbody2D rb;
    private float _deltaTime;
    private Vector2 _velocity;

#endregion

#region MonoBehaviour

    private void Update()
    {
        _deltaTime = Time.deltaTime;
        detect.DetectAll();
        timer.Update(_deltaTime);
        _velocity = rb.velocity;

        if (detect.down)
        {
            _jumpCutting = false;
            if (!_lastHitDown)
                _jumping = false;
        }

        if (Input.JumpDown)
            timer.JumpBuffer = jumpBufferTime;

        if (detect.down)
            timer.LastOnGround = coyoteTime;

        _atJumpApex = _jumping && Mathf.Abs(_velocity.y) <= jumpApexSpeedThreshold;

        /* Horizontal Move */
        CalculateRun();

        /* Vertical Move */
        SetGravity();
        CalculateJump();

        // RestrictVelocity();
        rb.velocity = _velocity;
    }

    void LateUpdate() => _lastHitDown = detect.down;

#endregion

#region Detects

    [System.Serializable]
    private struct Detect
    {
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private CheckBox upCheck, downCheck, leftCheck, rightCheck;
        public bool up, down, left, right;

        public void DetectAll()
        {
            up    = upCheck   .Detect(groundLayer);
            down  = downCheck .Detect(groundLayer);
            left  = leftCheck .Detect(groundLayer);
            right = rightCheck.Detect(groundLayer);
        }
    }

    [Header("Detects")]
    [SerializeField] private Detect detect;

    private bool _lastHitDown;

#endregion

#region Timer

    [System.Serializable]
    private struct Timer
    {
        public float JumpBuffer;
        public float LastOnGround;
        public void Update(float deltaTime)
        {
            JumpBuffer   -= deltaTime;
            LastOnGround -= deltaTime;
        }
    }
    private Timer timer;

#endregion

#region Run

    [Header("Run")]
    [SerializeField] private float maxRunSpeed = 13f;
    [SerializeField] private float runAcceleration = 90f, runDecceleration = 150f;
    [SerializeField] private float jumpApexBonusMoveSpeed = 2f;

    private void CalculateRun()
    {
        float rawH = Input.RawH;
        float v = _velocity.x;

        if (rawH != 0)
        {
            // v += a * t
            v += rawH * runAcceleration * _deltaTime;
            v = Mathf.Clamp(v, -maxRunSpeed, maxRunSpeed);
            // bonus speed at jump apex
            if (_atJumpApex)
                v += rawH * jumpApexBonusMoveSpeed;
        }
        else
            v = Mathf.MoveTowards(v, 0, runDecceleration * _deltaTime);

        _velocity.x = v;
    }

#endregion

#region Gravity

    [Header("Gravity")]
    [SerializeField] private Vector2 gravity = new(0f, 80f);
    private const float gravityScale = 1;
    [SerializeField] private float maxFallSpeed = 30f;

    private void SetGravity()
    {
        float scale = gravityScale;

        if (_atJumpApex)
            scale *= jumpApexGravityMult;
        else if (_jumpCutting)
            scale *= jumpCutGravityMult;

        // v = v_0 + a * t
        float v = _velocity.y - gravity.y * scale * _deltaTime;

        v = Mathf.Max(v, -maxFallSpeed);

        _velocity.y = v;
    }

#endregion

#region Jump

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 30f;
    [SerializeField] private float jumpCutSpeedMult = 0.5f;
    [SerializeField] private float jumpCutGravityMult = 2f;
    private bool _jumpCutting = false;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;
    private bool _jumping = false;
    [SerializeField] private float jumpApexSpeedThreshold = 0.5f;
    [SerializeField] private float jumpApexGravityMult = 0.5f;
    private bool _atJumpApex = false;

    private void CalculateJump()
    {
        float v = _velocity.y;

        if (!detect.up &&
            ((detect.down && timer.JumpBuffer > 0) ||
            (Input.JumpDown && !_jumping && timer.LastOnGround > 0)))
        {
            v = jumpSpeed;
            _jumping = true;
            _jumpCutting = false;
        }

        // jump cut if release jump button
        if (!detect.down && Input.JumpUp && !_jumpCutting && _jumping)
        {
            _jumpCutting = true;
            v *= jumpCutSpeedMult;
        }

        _velocity.y = v;
    }

#endregion

    private void RestrictVelocity()
    {
        if ((_velocity.x > 0 && detect.right) || (_velocity.x < 0 && detect.left))
            _velocity.x = 0;
        if ((_velocity.y > 0 && detect.up) || (_velocity.y < 0 && detect.down))
            _velocity.y = 0;
    }
}