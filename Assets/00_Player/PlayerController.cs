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
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float crouchSpeedMultiplier = 0.5f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float deceleration = 10f;

        [Header("Stamina Consumption")]
        [SerializeField] private float sprintStaminaDrain = 10f;
        [SerializeField] private float jumpStaminaCost = 15f;

        [Header("Jump Mechanics")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float jumpCutMultiplier = 0.4f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Collision & Physics")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 0.9f;

        [Header("Art & Animations (Sprites)")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite walkSideSprite;
        [SerializeField] private Sprite walkFrontSprite;
        [SerializeField] private Sprite walkBackSprite;

        private Rigidbody2D _rb;
        private BoxCollider2D _boxCollider;
        private PlayerVitals _vitals;
        private SpriteRenderer _spriteRenderer;

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
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // Configuração OBRIGATÓRIA de física
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (groundCheckPoint == null) groundCheckPoint = transform;
        }

        private void Update()
        {
            ProcessInput();
            UpdateTimers();
            UpdateColliderState();
            ProcessStaminaLogic();
            UpdateVisuals(); // Chama a troca de arte
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

            _rb.AddForce(Vector2.right * (velocityDiff * accelRate), ForceMode2D.Force);
        }

        private void ExecuteJump()
        {
            // O jumpBufferCounter é zerado imediatamente para evitar pulo duplo
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
            float targetHeight = _isCrouching ? crouchingHeight : standingHeight;

            // Só levanta se não tiver teto em cima
            if (!_isCrouching && CheckCeilingCollision())
            {
                targetHeight = crouchingHeight;
                _isCrouching = true;
            }

            Vector2 newSize = _boxCollider.size;
            newSize.y = targetHeight;
            _boxCollider.size = newSize;

            Vector2 newOffset = _boxCollider.offset;
            newOffset.y = (targetHeight - standingHeight) * 0.5f;
            _boxCollider.offset = newOffset;
        }

        private bool CheckCeilingCollision()
        {
            Vector2 origin = (Vector2)transform.position + _boxCollider.offset;
            return Physics2D.Raycast(origin, Vector2.up, standingHeight * 0.5f, groundLayer);
        }

        private void UpdateVisuals()
        {
            if (_spriteRenderer == null) return;

            if (Mathf.Abs(_moveInput.x) > 0.01f)
            {
                _spriteRenderer.flipX = _moveInput.x < 0;
                if (walkSideSprite != null) _spriteRenderer.sprite = walkSideSprite;
            }
            else
            {
                if (idleSprite != null) _spriteRenderer.sprite = idleSprite;
            }

            // Lógica para walkFront e walkBack podem ser adicionadas se você mover no eixo Y ou Z.
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckPoint == null) return;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }
    }
}