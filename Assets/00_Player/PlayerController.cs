using UnityEngine;

namespace Core.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerVitals))]
    public class PlayerController : MonoBehaviour
    {
        [Header("State Locks")]
        public bool isMovementUnlocked = true;
        public bool isJumpUnlocked = true;
        public bool isCrouchUnlocked = true;
        public bool isSprintUnlocked = true;

        [Header("Movement Dynamics")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float crouchSpeedMultiplier = 0.5f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float deceleration = 10f;

        [Header("Stamina Consumption")]
        [SerializeField] private float sprintStaminaDrain = 10f;
        [SerializeField] private float jumpStaminaCost = 15f;

        [Header("Jump Mechanics")]
        [SerializeField] private float jumpForce = 9f;
        [SerializeField] private float jumpCutMultiplier = 0.4f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Collision & Physics")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float standingHeight = 1.0f;
        [SerializeField] private float crouchingHeight = 0.5f;
        [SerializeField] private Vector2 colliderCenterOffset = Vector2.zero;

        private Rigidbody2D _rb;
        private BoxCollider2D _boxCollider;
        private PlayerVitals _vitals;

        private Vector2 _moveInput;
        private bool _isGrounded;
        private bool _isCrouching;
        private bool _isSprinting;
        private float _coyoteTimeCounter;
        private float _jumpBufferCounter;
        private bool _isJumpHeld;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _boxCollider = GetComponent<BoxCollider2D>();
            _vitals = GetComponent<PlayerVitals>();

            if (groundCheckPoint == null) groundCheckPoint = transform;
        }

        private void Update()
        {
            ProcessInput();
            UpdateTimers();
            UpdateColliderState();
            ProcessStaminaLogic();
        }

        private void FixedUpdate()
        {
            UpdateGroundStatus();
            ExecuteMovement();
            ExecuteJump();
        }

        private void ProcessInput()
        {
            _moveInput.x = isMovementUnlocked ? Input.GetAxisRaw("Horizontal") : 0f;

            if (isJumpUnlocked && Input.GetButtonDown("Jump")) _jumpBufferCounter = jumpBufferTime;
            _isJumpHeld = isJumpUnlocked && Input.GetButton("Jump");

            _isCrouching = isCrouchUnlocked && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.S));

            // Sprint exige input, estamina positiva, não estar agachado e movimento ativo
            _isSprinting = isSprintUnlocked && Input.GetKey(KeyCode.LeftShift) && !_isCrouching && Mathf.Abs(_moveInput.x) > 0.1f && _vitals.CurrentStamina > 0f;
        }

        private void UpdateTimers()
        {
            _coyoteTimeCounter = _isGrounded ? coyoteTime : _coyoteTimeCounter - Time.deltaTime;
            if (_jumpBufferCounter > 0f) _jumpBufferCounter -= Time.deltaTime;
        }

        private void UpdateGroundStatus()
        {
            _isGrounded = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0f, groundLayer);
        }

        private void ProcessStaminaLogic()
        {
            if (_isSprinting)
            {
                _vitals.ConsumeStamina(sprintStaminaDrain * Time.deltaTime);
            }
            else if (_isGrounded)
            {
                // Regenera mais rápido parado (1f) do que andando (0.5f)
                float regenMultiplier = Mathf.Abs(_moveInput.x) > 0.1f ? 0.5f : 1f;
                _vitals.RegenStamina(Time.deltaTime, regenMultiplier);
            }
        }

        private void ExecuteMovement()
        {
            float currentSpeedLimit = _isSprinting ? sprintSpeed : walkSpeed;

            currentSpeedLimit *= _vitals.GetSpeedModifier();
            if (_isCrouching && _isGrounded) currentSpeedLimit *= crouchSpeedMultiplier;

            float targetVelocityX = _moveInput.x * currentSpeedLimit;

            float velocityDiff = targetVelocityX - _rb.linearVelocityX;
            float accelRate = (Mathf.Abs(targetVelocityX) > 0.01f) ? acceleration : deceleration;
            float appliedForce = velocityDiff * accelRate;

            _rb.AddForce(Vector2.right * appliedForce, ForceMode2D.Force);
        }

        private void ExecuteJump()
        {
            if (_jumpBufferCounter > 0f && _coyoteTimeCounter > 0f && _vitals.HasStamina(jumpStaminaCost))
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocityX, jumpForce);
                _vitals.ConsumeStamina(jumpStaminaCost);

                _jumpBufferCounter = 0f;
                _coyoteTimeCounter = 0f;
            }

            if (!_isJumpHeld && _rb.linearVelocityY > 0f)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocityX, _rb.linearVelocityY * jumpCutMultiplier);
            }
        }

        private void UpdateColliderState()
        {
            if (_isCrouching)
            {
                AdjustCollider(crouchingHeight);
            }
            else if (!CheckCeilingCollision())
            {
                AdjustCollider(standingHeight);
            }
            else
            {
                _isCrouching = true;
            }
        }

        private bool CheckCeilingCollision()
        {
            Vector2 origin = (Vector2)transform.position + colliderCenterOffset;
            return Physics2D.Raycast(origin, Vector2.up, standingHeight * 0.5f, groundLayer);
        }

        private void AdjustCollider(float targetHeight)
        {
            Vector2 newSize = _boxCollider.size;
            newSize.y = targetHeight;
            _boxCollider.size = newSize;

            Vector2 newOffset = colliderCenterOffset;
            newOffset.y = (targetHeight - standingHeight) * 0.5f;
            _boxCollider.offset = newOffset;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckPoint == null) return;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }
    }
}