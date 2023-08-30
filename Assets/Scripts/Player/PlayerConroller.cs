using UnityEngine;

public class PlayerConroller : MonoBehaviour
{
#region Variables

    [Header("Components")]
    [SerializeField] private Transform tf;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D cl;
    private float _deltaTime;
    private Vector2 _velocity;

#endregion

#region MonoBehaviour

    private void Update()
    {
        CalculateJump();
    }

    private void FixedUpdate()
    {
        _deltaTime = Time.fixedDeltaTime;
        detect.DetectAll(groundLayer);
        topLeftCorner.Detect(groundLayer, tf.position);
        topRightCorner.Detect(groundLayer, tf.position);
        timer.Update(_deltaTime);
        // _velocity = rb.velocity;

        if (detect.Down)
        {
            _jumpCutting = false;
            if (!_lastHitDown)
                _jumping = false;
        }

        if (Input.JumpDown)
            timer.JumpBuffer = jumpBufferTime;

        if (detect.Down)
            timer.LastOnGround = coyoteTime;

        _atJumpApex = _jumping && Mathf.Abs(_velocity.y) <= jumpApexSpeedThreshold;

        CornerCorrect();

        /* Horizontal Move */
        CalculateRun();

        /* Vertical Move */
        SetGravity();

        RestrictVelocity();
        rb.velocity = _velocity;
    }

    void LateUpdate() => _lastHitDown = detect.Down;

#endregion

#region Detects

    [System.Serializable]
    private struct Detect
    {
        [SerializeField] private CheckBox upCheck, downCheck, leftCheck, rightCheck;
        private bool _up, _down, _left, _right;
        public readonly bool Up => _up;
        public readonly bool Down => _down;
        public readonly bool Left => _left;
        public readonly bool Right => _right;

        public void DetectAll(LayerMask layer)
        {
            _up    = upCheck   .Detect(layer);
            _down  = downCheck .Detect(layer);
            _left  = leftCheck .Detect(layer);
            _right = rightCheck.Detect(layer);
        }
    }

    [System.Serializable]
    private struct CornerDetect
    {
        [SerializeField] private CheckBox outer, inner, hitRay;
        private bool _detected;
        private float _hitPointX;
        public readonly bool Detected => _detected;
        public readonly float HitPointX => _hitPointX;

        public void Detect(LayerMask layer, Vector2 defaultPos)
        {
            _detected = outer.Detect(layer) && !inner.Detect(layer);
            if (_detected)
                _hitPointX = hitRay.GetHitPoint(layer, defaultPos).x;
        }
    }

    [Header("Detects")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Detect detect;
    [SerializeField] private CornerDetect topLeftCorner, topRightCorner;
    private bool _lastHitDown;

    private void CornerCorrect()
    {
        // if hit head on corner -> push forward a little bit
        if (topLeftCorner.Detected && Input.RawH >= 0 && _velocity.y > 0)
        {
            float leftBoundX = cl.bounds.center.x - cl.bounds.size.x / 2;
            float distance = topLeftCorner.HitPointX - leftBoundX;
            tf.Translate(distance * Vector2.right);
        }
        if (topRightCorner.Detected && Input.RawH <= 0 && _velocity.y > 0)
        {
            float rightBoundX = cl.bounds.center.x + cl.bounds.size.x / 2;
            float distance = topRightCorner.HitPointX - rightBoundX;
            tf.Translate(distance * Vector2.right);
        }
    }

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
    [SerializeField] private Vector2 gravity = new(0f, -80f);
    [SerializeField] private float maxFallSpeed = 30f;

    private void SetGravity()
    {
        float scale = 1;

        if (_atJumpApex)
            scale *= jumpApexGravityMult;
        else if (_jumpCutting)
            scale *= jumpCutGravityMult;

        // v = v_0 + a * t
        float v = _velocity.y + gravity.y * scale * _deltaTime;

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

        if ((!detect.Up || topLeftCorner.Detected || topRightCorner.Detected) &&
            ((detect.Down && timer.JumpBuffer > 0) ||
            (Input.JumpDown && !_jumping && timer.LastOnGround > 0)))
        {
            v = jumpSpeed;
            _jumping = true;
            _jumpCutting = false;
        }

        // jump cut if release jump button
        if (!detect.Down && Input.JumpUp && !_jumpCutting && _jumping)
        {
            _jumpCutting = true;
            v *= jumpCutSpeedMult;
        }

        _velocity.y = v;
    }

#endregion

    private void RestrictVelocity()
    {
        if ((_velocity.x > 0 && detect.Right) || (_velocity.x < 0 && detect.Left))
            _velocity.x = 0;
        if ((_velocity.y > 0 && detect.Up && !topLeftCorner.Detected && !topRightCorner.Detected) || (_velocity.y < 0 && detect.Down))
            _velocity.y = 0;
    }
}