
using UnityEngine;

public class PlayerBehavior : MonoBehaviour
{
    [Header("Snow Audio")]
    public AudioSource snowAudioSource;

    [Range(0f, 1f)]
    public float crouchVolume = 0.2f;

    [Range(0f, 1f)]
    public float walkVolume = 0.5f;

    [Range(0f, 1f)]
    public float runVolume = 1f;

    private float snowAudioTimer;

    [Header("Snow Detection")]
    public LayerMask snowLayer;
    public float surfaceCheckDistance = 2f;
    public float snowNoise = 1f;

    [Header("Footstep Sounds")]
    public float walkNoiseRadius = 8f;
    public float runNoiseRadius = 10f;
    public float crouchNoiseRadius = 4f;

    public float walkStepInterval = 0.55f;
    public float runStepInterval = 0.3f;
    public float crouchStepInterval = 0.8f;

    private float footstepTimer;
    private EnemyAI[] enemies;

    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f;

    private Animator _animator;

    public bool isRunning;
    public bool isCrouching;

    public Transform cameraTransform;

    private Rigidbody _rb;

    private float hInput;
    private float vInput;

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _rb = GetComponent<Rigidbody>();
        enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (_rb == null)
        {
            Debug.LogError("PlayerBehavior: Rigidbody не найден на Player.");
        }

        if (_animator == null)
        {
            Debug.LogWarning("PlayerBehavior: Animator не найден у Player или его дочерних объектов.");
        }

        if (cameraTransform == null)
        {
            Debug.LogError("PlayerBehavior: Не назначена камера.");
        }

    }

    void Update()
    {
        // WASD
        hInput = Input.GetAxisRaw("Horizontal");
        vInput = Input.GetAxisRaw("Vertical");

        // приседание
        isCrouching = Input.GetKey(KeyCode.LeftControl);

        // бег только если персонаж не приседает
        isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
        
        if (Mathf.Abs(hInput) > 0.1f || Mathf.Abs(vInput) > 0.1f)
        {
            MakeFootstepNoise();
        }
        else
        {
            footstepTimer = 0f;
        }

        PlaySnowSteps();
    }

    void FixedUpdate()
    {
        if (_rb == null || cameraTransform == null)
            return;

        Move();
    }

    private void Move()
    {
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 direction = cameraForward * vInput + cameraRight * hInput;

        direction = Vector3.ClampMagnitude(direction, 1f);

        // скорость для Blend Tree
        if (_animator != null)
        {
            float animationSpeed = 0f;

            if (direction.magnitude > 0.1f)
            {
                animationSpeed = isRunning ? 1f : 0.5f;
            }

            _animator.SetFloat("Speed", animationSpeed);
            _animator.SetBool("IsCrouching", isCrouching);
        }

        if (direction.magnitude > 0.1f)
        {
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            if (isCrouching)
            {
                currentSpeed *= 0.5f;
            }

            Vector3 movement = direction * currentSpeed * Time.fixedDeltaTime;

            _rb.MovePosition(_rb.position + movement);

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            _rb.MoveRotation(
                Quaternion.Slerp(_rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime)
            );
        }
    }

    float GetSurfaceNoiseMultiplier()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, surfaceCheckDistance, ~0,
        QueryTriggerInteraction.Collide);

        foreach (RaycastHit hit in hits)
        {

            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Snow"))
            {
                return snowNoise;
            }
        }

        return 0f;
    }
    void MakeFootstepNoise()
    {
        float noiseRadius;
        float stepInterval;

        if (isCrouching)
        {
            noiseRadius = crouchNoiseRadius;
            stepInterval = crouchStepInterval;
        }
        else if (isRunning)
        {
            noiseRadius = runNoiseRadius;
            stepInterval = runStepInterval;
        }
        else
        {
            noiseRadius = walkNoiseRadius;
            stepInterval = walkStepInterval;
        }

        footstepTimer += Time.deltaTime;

        if (footstepTimer >= stepInterval)
        {
            footstepTimer = 0f;

            float surfaceMultiplier = GetSurfaceNoiseMultiplier();

            // если поверхность не обнаружена, звук не создаётся
            if (surfaceMultiplier <= 0f)
                return;

            noiseRadius *= surfaceMultiplier;

            foreach (EnemyAI enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.HearSound(transform.position, noiseRadius);
                }
            }
        }
    }

    void PlaySnowSteps()
    {
        bool isMoving = Mathf.Abs(hInput) > 0.1f || Mathf.Abs(vInput) > 0.1f;

        bool isOnSnow = GetSurfaceNoiseMultiplier() > 0f;

        if (isMoving && isOnSnow)
        {
            float interval;
            float volume;

            if (isCrouching)
            {
                interval = 0.8f;
                volume = crouchVolume;
            }
            else if (isRunning)
            {
                interval = 0.3f;
                volume = runVolume;
            }
            else
            {
                interval = 0.5f;
                volume = walkVolume;
            }

            snowAudioTimer += Time.deltaTime;

            if (snowAudioTimer >= interval)
            {
                snowAudioTimer = 0f;

                if (snowAudioSource != null)
                {
                    snowAudioSource.PlayOneShot(
                        snowAudioSource.clip,
                        volume
                    );
                }
            }
        }
        else
        {
            snowAudioTimer = 0f;
        }
    }
}
