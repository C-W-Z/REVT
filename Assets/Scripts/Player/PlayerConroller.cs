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

    /* Events of button press down should be detected in Update() */
    private void Update()
    {
        /* Update States */
        if (Input.RawH < 0)
            timer.SetNoLeftCornerCorrect(noCornerCorrectTime);
        if (Input.RawH > 0)
            timer.SetNoRightCornerCorrect(noCornerCorrectTime);
        if (Input.JumpDown)
            timer.SetJumpBuffer(jumpBufferTime);

        CalculateJump();
    }

    // CheckBox detections & physic calculations will work better in FixedUpdate()
    private void FixedUpdate()
    {
        /* Update States */
        _deltaTime = Time.fixedDeltaTime;

        detect.DetectAll(groundLayer);
        topLeftCorner.Detect(groundLayer, tf.position);
        topRightCorner.Detect(groundLayer, tf.position);

        timer.Update(_deltaTime);

        if (detect.Down)
        {
            _jumpCutting = false;
            if (!_lastDetectDown)
                _jumping = false;

            timer.SetCoyote(coyoteTime);
        }

        _atJumpApex = _jumping && Mathf.Abs(_velocity.y) <= jumpApexSpeedThreshold;

        CornerCorrect();

        CalculateGravity(); // can be overwrite, so calculate first
        CalculateRun();

        /* Apply Move to Rigidbody*/
        RestrictVelocity();
        rb.velocity = _velocity;
    }

    void LateUpdate() => _lastDetectDown = detect.Down;

#endregion

#region Timer

    [System.Serializable]
    private struct Timer
    {
        private float _noLeftCornerCorrect; // LastPressLeft
        private float _noRightCornerCorrect; // LastPressRight
        private float _jumpBuffer; // LastPressJump
        private float _coyote;  // LastOnGround
        public readonly bool CanLeftCornerCorrect => _noLeftCornerCorrect < 0;
        public readonly bool CanRightCornerCorrect => _noRightCornerCorrect < 0;
        public readonly bool HasJumpBuffer => _jumpBuffer > 0;
        public readonly bool CanCoyote => _coyote > 0;
        public void Update(float deltaTime)
        {
            _noLeftCornerCorrect  -= deltaTime;
            _noRightCornerCorrect -= deltaTime;
            _jumpBuffer           -= deltaTime;
            _coyote               -= deltaTime;
        }
        public void SetNoLeftCornerCorrect(float time)
            => _noLeftCornerCorrect = time;
        public void SetNoRightCornerCorrect(float time)
            => _noRightCornerCorrect = time;
        public void SetJumpBuffer(float time)
            => _jumpBuffer = time;
        public void SetCoyote(float time)
            => _coyote = time;
    }

    private Timer timer;

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
    private bool _lastDetectDown;
    [SerializeField] private CornerDetect topLeftCorner, topRightCorner;
    [SerializeField, Tooltip("No left/right corner correct if the time after press left/right within this value")] private float noCornerCorrectTime = 0.5f;

    private void CornerCorrect()
    {
        // if hit head on left corner & not moving left -> push right a little bit
        if (topLeftCorner.Detected && timer.CanLeftCornerCorrect && _velocity.y > 0)
        {
            float leftBoundX = cl.bounds.center.x - cl.bounds.size.x / 2;
            float distance = topLeftCorner.HitPointX - leftBoundX;
            distance += 0.001f; // add a small value to avoid collide on edge
            tf.Translate(distance * Vector2.right);
        }
        // if hit head on right corner & not moving right -> push left a little bit
        else if (topRightCorner.Detected && timer.CanRightCornerCorrect && _velocity.y > 0)
        {
            float rightBoundX = cl.bounds.center.x + cl.bounds.size.x / 2;
            float distance = rightBoundX - topRightCorner.HitPointX;
            distance += 0.001f; // add a small value to avoid collide on edge
            tf.Translate(distance * Vector2.left);
        }
    }

    private void RestrictVelocity()
    {
        if ((_velocity.x > 0 && detect.Right) || (_velocity.x < 0 && detect.Left))
            _velocity.x = 0;
        if ((_velocity.y > 0 && detect.Up && !topLeftCorner.Detected && !topRightCorner.Detected) || (_velocity.y < 0 && detect.Down))
            _velocity.y = 0;
    }

#endregion

#region Run

    [Header("Run")]
    [SerializeField] private float maxRunSpeed = 13f;
    [SerializeField] private float runAcceleration = 90f, runDecceleration = 90f;
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
    [SerializeField] private float gravity = 80f;
    private Vector2 gravityDir = Vector2.down;
    [SerializeField] private float maxFallSpeed = 30f;

    private void CalculateGravity()
    {
        // calculate gravity scale
        float scale = 1;
        if (_atJumpApex)
            scale = jumpApexGravityMult;
        else if (_jumpCutting)
            scale = jumpCutGravityMult;

        Debug.Assert(gravityDir.magnitude == 1f, $"gravityDir: {gravityDir} has wrong magnitude: {gravityDir.magnitude}", this);

        // v = v_0 + a * t
        Vector2 v = _velocity + gravity * scale * _deltaTime * gravityDir;

        // limit max falling speed
        if (gravityDir.x != 0)
            v.x = Mathf.Max(v.x, maxFallSpeed * gravityDir.x);
        else
            v.y = Mathf.Max(v.y, maxFallSpeed * gravityDir.y);

        _velocity = v;
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
    [SerializeField] private float jumpApexSpeedThreshold = 3f;
    [SerializeField] private float jumpApexGravityMult = 0.5f;
    private bool _atJumpApex = false;

    private void CalculateJump()
    {
        float v = _velocity.y;

        // start jump
        if (!_jumping && (!detect.Up || topLeftCorner.Detected || topRightCorner.Detected) &&
            ( (detect.Down && (timer.HasJumpBuffer || Input.JumpDown)) || (Input.JumpDown && timer.CanCoyote) ))
        {
            v = jumpSpeed;
            _jumping = true;
            _jumpCutting = false;
        }

        // jump cut if release jump button
        if (!detect.Down && Input.JumpUp && !_jumpCutting && _jumping && _velocity.y > 0)
        {
            _jumpCutting = true;
            v *= jumpCutSpeedMult;
        }

        _velocity.y = v;
    }

#endregion
}