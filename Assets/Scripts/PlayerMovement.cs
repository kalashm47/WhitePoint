using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public PlayerStats stats;

    private RaycastHit2D landingHit;
    private RaycastHit2D leftHit;
    private RaycastHit2D rightHit;
    private RaycastHit2D topHit;

    private BoxCollider2D playerCollider;


    float rightPositionX;
    float topPositionY;
    float bottomPositionY;
    float leftPositionX;

    [SerializeField] private Vector2 moveInput;

    [SerializeField] public float walkSpeed = 3f;      // ADDED: Separate walk speed
    [SerializeField] public float runSpeed = 7f;       // ADDED: Separate run speed

    [SerializeField] private bool canRun = true;        // ADDED: Toggle for running
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift; // ADDED: Run key

    [SerializeField][Range(0, 1)] float lerpConstant = 0.1f;

    [SerializeField] public float jumpForce = 5f;

    [SerializeField] private float jumpBufferTime = 0.2f;

    [SerializeField] private float coyoteTime = 0.1f;

    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private float groundCheckDistance = 0.1f;


    private float jumpBufferCounter;
    private float coyoteCounter;

    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool isJumping;
    private bool isRunning;      // ADDED: Track running state
    private float currentSpeed;   // ADDED: Current movement speed

    private Animator anim;

    private FrameInput frameInput;
    public Rigidbody2D PlayerRb { get; private set; }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public bool RunHeld;      // ADDED: Run input
        public Vector2 Move;
    }

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        PlayerRb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();

        if (stats == null)
        {
            Debug.LogError("[PlayerMovement] PlayerStats not assigned!");
        }

        currentSpeed = walkSpeed; // Initialize with walk speed  
    }

    private void Update()
    {
        GatherInput();
        GatherInput();
        UpdateBuffers();
        UpdateAnimations();
        UpdateMovementState(); // ADDED: Update walk/run state
        physicalDirectionalDetectable();

        // Debug for space button
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"[Space] Button PRESSED - Time: {Time.time}");
        }

        if (Input.GetKey(KeyCode.Space))
        {
            Debug.Log($"[Space] Button HELD - Frame: {Time.frameCount}");
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            Debug.Log($"[Space] Button RELEASED - Time: {Time.time}");
        }
    }

    private void UpdateMovementState()
    {
        // ADDED: Determine if player is running
        if (canRun && frameInput.RunHeld && Mathf.Abs(frameInput.Move.x) > 0.1f && isGrounded)
        {
            isRunning = true;
            currentSpeed = runSpeed;
        }
        else
        {
            isRunning = false;
            currentSpeed = walkSpeed;
        }
    }

    private void UpdateAnimations()
    {
        if (anim != null)
        {
            float horizontalSpeed = Mathf.Abs(frameInput.Move.x);
            float currentMovementSpeed = horizontalSpeed * (isRunning ? 2f : 1f);

            // EXISTE no Animator
            anim.SetFloat("Speed", currentMovementSpeed);

            // EXISTE no Animator
            anim.SetBool("isGrounded", isGrounded);

            // EXISTE no Animator
            anim.SetFloat("VerticalVelocity", PlayerRb.velocity.y);

            // EXISTE no Animator
            anim.SetBool("isFalling", !isGrounded && PlayerRb.velocity.y < 0);

            // JUMP
            if (isJumping)
            {
                anim.SetTrigger("Jump");
                isJumping = false;
            }
        }
    }

    private void UpdateBuffers()
    {
        if (frameInput.JumpDown)
        {
            jumpBufferCounter = jumpBufferTime;
            Debug.Log($"[Buffer] Jump buffer set to {jumpBufferTime} - Counter: {jumpBufferCounter}");
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        RefreshGroundState();
        playerBehaviour();
        HandleJump();
    }

    private void GatherInput()
    {
        frameInput = new FrameInput
        {
            JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),
            JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),
            RunHeld = Input.GetKey(runKey), // ADDED: Capture run input
            Move = new Vector2(Input.GetAxisRaw("Horizontal"), 0)
        };

        if (frameInput.JumpDown)
        {
            Debug.Log($"[FrameInput] JumpDown detected - isGrounded: {isGrounded} - Time: {Time.time}");
        }
    }

    private void RefreshGroundState()
    {
        if (playerCollider == null) return;

        RaycastHit2D hit = Physics2D.BoxCast(
            playerCollider.bounds.center,
            playerCollider.bounds.size,
            0,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        wasGroundedLastFrame = isGrounded;
        isGrounded = hit.collider != null;

        if (isGrounded && !wasGroundedLastFrame)
        {
            isJumping = false;
            // ADDED: Trigger landing animation
            if (anim != null)
            {
                anim.SetTrigger("Land");
            }
            Debug.Log("[Animation] Landed on ground - Reset jump state");
        }

        if (isGrounded != wasGroundedLastFrame)
        {
            Debug.Log($"[GroundState] Changed from {wasGroundedLastFrame} to {isGrounded} - Hit: {(hit.collider != null ? hit.collider.name : "None")}");
        }
    }

    private void HandleJump()
    {
        bool canJump = (jumpBufferCounter > 0) && (isGrounded || coyoteCounter > 0);

        if (canJump)
        {
            Debug.Log($"[HandleJump] JUMP AUTHORIZED! JumpBuffer: {jumpBufferCounter:F3}s | isGrounded: {isGrounded} | CoyoteTime: {coyoteCounter:F3}s");
            ExecuteJump();

            jumpBufferCounter = 0;
            coyoteCounter = 0;
        }
        else if (frameInput.JumpDown)
        {
            Debug.Log($"[HandleJump] Jump failed - JumpBuffer: {jumpBufferCounter:F3} | isGrounded: {isGrounded} | CoyoteTime: {coyoteCounter:F3}");
        }
    }

    private void physicalDirectionalDetectable()
    {

        landingHit = Physics2D.Raycast(new Vector2(this.transform.position.x, bottomPositionY + transform.position.y), new Vector2(transform.position.x, 0.2f));
        leftHit = Physics2D.Raycast(new Vector2(leftPositionX + transform.position.x, this.transform.position.y), new Vector2(leftPositionX - 0.2f, 0.0f), 0.2f);
        rightHit = Physics2D.Raycast(new Vector2(rightPositionX + transform.position.x, this.transform.position.y), new Vector2(rightPositionX + 0.2f, 0.0f), 0.2f);
        topHit = Physics2D.Raycast(new Vector2(this.transform.position.x, topPositionY + transform.position.y), new Vector2(transform.position.x, 0.2f), 0.2f);
    }
    private void ExecuteJump()
    {
        isJumping = true;
        isRunning = false; // Reset running state when jumping

        if (stats == null)
        {
            PlayerRb.velocity = new Vector2(PlayerRb.velocity.x, jumpForce);
            Debug.LogWarning($"[ExecuteJump] Using jumpForce fallback: {jumpForce}");
            return;
        }

        float jumpVelocity = Mathf.Sqrt(2f * stats.jumpHeight * Mathf.Abs(Physics2D.gravity.y * stats.baseGravity));

        Debug.Log($"[ExecuteJump] Jump executed! Velocity: {jumpVelocity} - Height: {stats.jumpHeight}");

        PlayerRb.velocity = new Vector2(PlayerRb.velocity.x, jumpVelocity);
    }


    void playerBehaviour()
    {
        float horizontal = frameInput.Move.x;

        if (horizontal != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Sign(horizontal),
                transform.localScale.y,
                transform.localScale.z
            );
        }

        Vector2 targetVelocity = new Vector2(
            horizontal * currentSpeed,
            PlayerRb.velocity.y
        );

        PlayerRb.velocity = Vector2.Lerp(
            PlayerRb.velocity,
            targetVelocity,
            lerpConstant
        );
    }

    // ADDED: Animation Event methods
    public void OnFootstep() // Call this from animation events
    {
        // Play footstep sound based on surface and running state
        float volume = isRunning ? 0.8f : 0.4f;
        float pitch = isRunning ? 1.2f : 1.0f;

        // You can add audio playback here
        Debug.Log($"[Footstep] Running: {isRunning}, Volume: {volume}, Pitch: {pitch}");
    }

    public void OnLand() // Call this from animation events
    {
        // Play landing sound/effect
        Debug.Log("[Landing] Player landed on ground");
    }
}