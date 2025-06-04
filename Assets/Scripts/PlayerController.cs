
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraHolder;      // Drag‐in your CameraHolder here
    public GameObject projectilePrefab; // Prefab for projectile to shoot
    public Transform projectileSpawn;   // Spawn point for projectiles

    // Private state
    private CharacterController cc;
    private PlayerControls controls;
    private Vector2 moveInput = Vector2.zero;
    private Vector2 lookInput = Vector2.zero;
    private bool isSprinting = false;
    private bool isGrounded;
    private float verticalVelocity = 0f;

    // (We no longer need canDodge bool here if we're doing a simple cooldown in the Dodge method.)
    private float nextFireTime = 0f;
    private Camera mainCam;
    private int currentHealth;
    private float pitch = 0f;

    [Header("Movement Settings")]
    public float walkSpeed = 4f;        // Base movement speed
    public float sprintSpeed = 7f;      // Speed when sprinting
    public float jumpForce = 8f;        // Force applied when jumping
    public float gravity = -20f;        // Gravity force applied to player

    // Dodge / Sprint settings
    [Header("Dodge/Sprint Settings")]
    public float dodgeDistance = 3f;    // Distance to dodge
    public float dodgeCooldown = 1f;    // Cooldown between dodges
    private bool canDodge = true;

    [Header("Combat Settings")]
    public float projectileSpeed = 30f; // Speed of projectiles
    public float fireRate = 0.5f;       // Time between shots
    public int maxHealth = 100;         // Maximum player health

    [Header("Camera Settings")]
    public float lookSensitivity = 1f;  // Sensitivity for camera movement
    public float normalFOV = 60f;       // Normal field of view
    public float aimFOV = 40f;          // Field of view when aiming
    public float aimSpeed = 10f;        // Speed of FOV transition
    private bool isAiming = false;      // Whether player is currently aiming

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        controls = new PlayerControls();

        // … Move / Look / Shoot / etc. (unchanged) …

        // 1) DODGE: Tap interaction
        controls.Gameplay.Dodge.performed += ctx => OnDodge();

        // 2) SPRINT: Hold interaction
        controls.Gameplay.Sprint.started += ctx => OnSprintStarted();
        controls.Gameplay.Sprint.canceled += ctx => OnSprintCanceled();

        // … Other bindings: UseItem, Interact, Jump, SwitchLightDark, etc. …
        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Gameplay.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Look.canceled += ctx => lookInput = Vector2.zero;

        controls.Gameplay.Shoot.performed += ctx => OnShoot();
        controls.Gameplay.SwitchLightDark.performed += ctx => OnSwitchLightDark();
        controls.Gameplay.Interact.performed += ctx => OnInteract();
        controls.Gameplay.UseItem.performed += ctx => OnUseItem();
        controls.Gameplay.Jump.performed += ctx => OnJump();
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
    }

    private void Start()
    {
        mainCam = Camera.main;
        currentHealth = maxHealth;
        if (mainCam != null)
            mainCam.fieldOfView = normalFOV;
    }

    private void Update()
    {
        HandleMovement();
        HandleLook();
        HandleAimFOV();
    }

    // ───────────── Movement (inc. Sprinting) ──────────────────────────────
    private void HandleMovement()
    {
        isGrounded = cc.isGrounded;
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -1f;

        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 forward = transform.forward * moveInput.y;
        Vector3 right = transform.right * moveInput.x;
        Vector3 moveDir = (forward + right).normalized;
        Vector3 velocity = moveDir * targetSpeed;

        // Gravity & jump
        if (isGrounded && verticalVelocity > -2f)
            verticalVelocity = -1f;

        verticalVelocity += gravity * Time.deltaTime;
        velocity.y = verticalVelocity;
        cc.Move(velocity * Time.deltaTime);
    }

    // ───────────── Jump ───────────────────────────────────────────────────
    private void OnJump()
    {
        if (isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
    }

    // ───────────── Dodging ─────────────────────────────────────────────────
    private void OnDodge()
    {
        if (!canDodge) return;

        // Compute a dodge direction (if you’re strafing, dodge that way; otherwise, forward)
        Vector3 dodgeDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        if (dodgeDir.sqrMagnitude < 0.1f)
            dodgeDir = transform.forward;

        // Instantly move the CharacterController a short distance
        cc.Move(dodgeDir * dodgeDistance);
        canDodge = false;
        StartCoroutine(DodgeCooldownRoutine());
    }

    private System.Collections.IEnumerator DodgeCooldownRoutine()
    {
        yield return new WaitForSeconds(dodgeCooldown);
        canDodge = true;
    }

    // ───────────── Sprinting ───────────────────────────────────────────────
    private void OnSprintStarted()
    {
        // This is called once the button has been **held** past the Hold threshold
        isSprinting = true;
    }

    private void OnSprintCanceled()
    {
        // This is called when you release the button after holding
        isSprinting = false;
    }

    // ───────────── Look / Aim / Shoot etc. (unchanged) ────────────────────
    private void HandleLook()
    {
        if (lookInput.sqrMagnitude > 0.0001f)
        {
            float yaw = lookInput.x * lookSensitivity;
            transform.Rotate(Vector3.up, yaw, Space.World);

            pitch -= lookInput.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -75f, +75f);
            cameraHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void HandleAimFOV()
    {
        if (mainCam == null) return;
        float targetFOV = isAiming ? aimFOV : normalFOV;
        mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, targetFOV, Time.deltaTime * aimSpeed);
    }

    private void OnShoot()
    {
        if (Time.time < nextFireTime || projectilePrefab == null || projectileSpawn == null)
            return;

        nextFireTime = Time.time + fireRate;
        GameObject bullet = Instantiate(projectilePrefab, projectileSpawn.position, Quaternion.identity);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 shootDir = cameraHolder.forward;
            rb.linearVelocity = shootDir * projectileSpeed;
        }
    }

    private void OnSwitchLightDark() { /* … */ }
    private void OnInteract()
    {
        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 2f))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null) interactable.Interact();
        }
    }
    private void OnUseItem() { /* … */ }

    // ───────────── Health & Damage ────────────────────────────────────────
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Debug.Log("Player died.");
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 25), $"Health: {currentHealth}/{maxHealth}");
    }
#endif
}
