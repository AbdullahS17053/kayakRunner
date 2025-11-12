using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace RageRunGames.KayakController
{
    public enum ForceOn
    {
        WaterTrigger,
        AnimationEvent
    }

    [RequireComponent(typeof(Rigidbody))]
    public class KayakController : MonoBehaviour
    {
        [Header("Force Settings")]
        [SerializeField] private ForceOn forceOn;
        [SerializeField] private bool useExternalPluginBuoyancyForces;
        [SerializeField] private bool useExternalPluginDragForces;

        [Header("Water Force Settings")]
        [SerializeField] private bool enableWaterForceOnKayak;
        [SerializeField] private float waterForceMultiplier;
        [SerializeField] private Vector3 waterForceDirection;

        [Header("Paddle Settings")] 
        [SerializeField] private Transform paddleParent;

        [Header("Physics Settings")] 
        [SerializeField] private float forwardStrokeForce = 12f;
        [SerializeField] private float maxVelocity = 6f;
        [SerializeField] private float maxAngularVelocity = 5f;
        [SerializeField] private float dragInWater = 1.5f;
        [SerializeField] private float angularDragInWater = 3f;
        [SerializeField] private float stability = 10f;
        [SerializeField] private float turningTorque = 6f;
        [SerializeField] private float drawStrokeForce = 15f;

        [Header("Steering Settings")] 
        [SerializeField] private float steerTorqueMultiplier = 2f;

        [Header("Buoyancy Settings")] 
        [SerializeField] private Transform[] buoyancyPoints;
        [SerializeField] private KCWaterSurface waterSurface;

        [Header("Visual Leaning")] 
        [SerializeField] private Transform visualModel;
        [SerializeField] private float leanAmount = 10f;
        [SerializeField] private float leanSpeed = 5f;

        [Header("Audio Settings")] 
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] audioClips;

        [Header("Mobile / Auto Settings")]
        [SerializeField] private bool alwaysMoveForward = false; // ✅ Added for continuous forward motion

        private Animator animator;
        [SerializeField] Rigidbody rb;

        private bool isLeftDrawStroking;
        private bool isRightDrawStroking;

        private bool isLeftRudderStroking;
        private bool isRightRudderStroking;

        private float drawStrokeAmount;
        private Vector3 glideVelocity;
        private float glideDecay = 0.95f;

        private float vertical;
        private float horizontal;
        
        
        public bool IsPaddleInWater { get; set; } = false;
        public ForceOn ForceOn => forceOn;

        public int health = 100;
        // Store last hit time per obstacle so we can ignore repeated collisions
        private float hitCooldown = 3f;
        private Dictionary<GameObject, float> lastHitTimes = new Dictionary<GameObject, float>();

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        public InputActionAsset inputActionsAsset;

        private InputAction forwardReverseAction;
        private InputAction leftRightDrawAction;
#endif

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private void Awake()
        {
            if (inputActionsAsset == null)
            {
                Debug.LogError("Missing the Input Action File");
                return;
            }

            var kayakMap = inputActionsAsset.FindActionMap("KayakInputs", throwIfNotFound: false);
            if (kayakMap == null)
            {
                Debug.LogError("InputActionMap 'KayakInputs' not found in assigned InputActionAsset.");
                return;
            }

            forwardReverseAction = kayakMap.FindAction("ForwardReversePaddling", throwIfNotFound: false);
            leftRightDrawAction = kayakMap.FindAction("LeftRightDraw", throwIfNotFound: false);

            if (forwardReverseAction == null) Debug.LogError("'ForwardReversePaddling' action not found in 'KayakInputs' map.");
            if (leftRightDrawAction == null) Debug.LogError("'LeftRightDraw' action not found in 'KayakInputs' map.");

            kayakMap.Enable();

            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            if (!waterSurface)
                waterSurface = FindObjectOfType<KCWaterSurface>();
        }
#else
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            if (!waterSurface)
                waterSurface = FindObjectOfType<KCWaterSurface>();
        }
#endif

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (forwardReverseAction == null || leftRightDrawAction == null)
                return;

            isLeftDrawStroking = false;
            isRightDrawStroking = false;

            vertical = forwardReverseAction.ReadValue<Vector2>().y;
            horizontal = forwardReverseAction.ReadValue<Vector2>().x;
            float input = leftRightDrawAction.ReadValue<float>();

            isLeftDrawStroking = input < -0.1f;
            isRightDrawStroking = input > 0.1f;
#else
            vertical = Input.GetAxisRaw("Vertical");
            horizontal = Input.GetAxisRaw("Horizontal");

// ✅ Override input when UI buttons are pressed
            if (turnLeftPressed)
                horizontal = -1f;
            else if (turnRightPressed)
                horizontal = 1f;

            isLeftDrawStroking = Input.GetKey(KeyCode.Q);
            isRightDrawStroking = Input.GetKey(KeyCode.E);
#endif
            // ✅ Continuous forward motion override
            if (alwaysMoveForward)
            {
                vertical = 1f; // simulate holding 'W' or forward paddle
            }

            bool isForward = vertical > 0.1f;
            bool isReverse = vertical < -0.1f;

            bool isRight = horizontal > 0.1f;
            bool isLeft = horizontal < -0.1f;

            animator.SetBool("LeftForwardStroking", false);
            animator.SetBool("RightForwardStroking", false);
            animator.SetBool("LeftReverseStroking", false);
            animator.SetBool("RightReverseStroking", false);

            animator.SetBool("ForwardStroking", false);
            animator.SetBool("ReverseStroking", false);

            animator.SetBool("LeftSweepStroking", false);
            animator.SetBool("RightSweepStroking", false);

            animator.SetBool("LeftDrawStroking", false);
            animator.SetBool("RightDrawStroking", false);

            if (isForward && isRight)
            {
                animator.SetBool("LeftForwardStroking", true);
            }
            else if (isForward && isLeft)
            {
                animator.SetBool("RightForwardStroking", true);
            }
            else if (isReverse && isRight)
            {
                animator.SetBool("LeftReverseStroking", true);
            }
            else if (isReverse && isLeft)
            {
                animator.SetBool("RightReverseStroking", true);
            }
            else
            {
                animator.SetBool("ForwardStroking", isForward && !isLeftDrawStroking && !isRightDrawStroking);
                animator.SetBool("ReverseStroking", isReverse && !isLeftDrawStroking && !isRightDrawStroking);
                animator.SetBool("LeftSweepStroking", isRight && !isLeftDrawStroking && !isRightDrawStroking);
                animator.SetBool("RightSweepStroking", isLeft && !isLeftDrawStroking && !isRightDrawStroking);

                animator.SetBool("LeftDrawStroking", isLeftDrawStroking);
                animator.SetBool("RightDrawStroking", isRightDrawStroking);
            }

            if (!isLeftDrawStroking && !isRightDrawStroking)
            {
                drawStrokeAmount = 0f;
            }
            
            UpdateAnimations();
            /*
#if UNITY_STANDALONE || UNITY_EDITOR
            // --- PC Input Controls ---
            if (Input.GetKey(KeyCode.W))
            {
                SetMoveDirection(1); // Forward
            }
            else if (Input.GetKey(KeyCode.S))
            {
                SetMoveDirection(-1); // Reverse
            }
            else
            {
                SetMoveDirection(0); // Stop when no key pressed
            }

            if (Input.GetKey(KeyCode.A))
            {
                OnLeftButtonDown();
            }
            else
            {
                OnLeftButtonUp();
            }

            if (Input.GetKey(KeyCode.D))
            {
                OnRightButtonDown();
            }
            else
            {
                OnRightButtonUp();
            }
#endif
*/
            
#if UNITY_ANDROID || UNITY_IOS
            UpdateTouchDrag();
#endif

        }

        private void FixedUpdate()
        {
            if (!useExternalPluginBuoyancyForces)
            {
                ApplyBuoyancy();
            }

            ApplyWaterDrag();
            StabilizeKayak();

            if (enableWaterForceOnKayak)
            {
                AddWaterForce();
            }

            float targetLeanAngle = -horizontal * leanAmount;

            if (isLeftDrawStroking)
            {
                targetLeanAngle = leanAmount * 1.125f;
            }
            else if (isRightDrawStroking)
            {
                targetLeanAngle = -leanAmount * 1.125f;
            }

            if (Mathf.Abs(vertical) > 0f && Mathf.Approximately(horizontal, 0f))
            {
                float waveFrequency = waterSurface.WaveFrequency;
                targetLeanAngle = waveFrequency * -leanAmount * 0.2f;
            }

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetLeanAngle);
            visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, targetRotation, Time.deltaTime * leanSpeed);

            if (glideVelocity.magnitude > 0.01f)
            {
                rb.AddForce(glideVelocity, ForceMode.Force);
                glideVelocity *= glideDecay;
            }

            if (rb.linearVelocity.magnitude > maxVelocity)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
            }

            if (rb.angularVelocity.magnitude > maxAngularVelocity)
            {
                rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
            }

            if (IsPaddleInWater)
            {
                drawStrokeAmount = drawStrokeForce;
            }

            if (isLeftDrawStroking)
            {
                drawStrokeAmount -= Time.fixedDeltaTime * 15f;
                rb.AddForce(-transform.right * drawStrokeAmount);
            }

            if (isRightDrawStroking)
            {
                drawStrokeAmount -= Time.fixedDeltaTime * 15f;
                rb.AddForce(transform.right * drawStrokeAmount);
            }

            // ✅ Continuous forward force if alwaysMoveForward is active
            if (alwaysMoveForward && !isLeftDrawStroking && !isRightDrawStroking)
            {
                float targetSpeed = maxVelocity * 0.6f;
                float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
                if (currentSpeed < targetSpeed)
                {
                    rb.AddForce(transform.forward * forwardStrokeForce * 0.05f, ForceMode.Force);
                }

            }
        }

        public void ApplyPaddleForce(Vector3 hitPoint, Vector3 paddleVelocity)
        {
            if (isRightDrawStroking || isLeftDrawStroking) return;

            if (Mathf.Abs(vertical) > 0.1f && horizontal == 0f)
            {
                Vector3 forwardPush = transform.forward * Mathf.Clamp(paddleVelocity.magnitude, 0, forwardStrokeForce) * vertical;
                rb.AddForceAtPosition(forwardPush, hitPoint, ForceMode.Force);

                Vector3 localPoint = transform.InverseTransformPoint(hitPoint);
                float sideInfluence = Mathf.Clamp(localPoint.x, -1f, 1f);
                rb.AddTorque(Vector3.up * sideInfluence * turningTorque, ForceMode.Force);
            }

            Vector3 projectedForce = transform.forward * Mathf.Clamp(paddleVelocity.magnitude, 0, forwardStrokeForce) *
                                     vertical;
            glideVelocity = projectedForce * 0.5f;

            if (Mathf.Abs(horizontal) < 0.1f)
            {
                rb.AddForce(transform.forward * forwardStrokeForce * 0.1f * vertical);
            }
            else
            {
                rb.AddForce(transform.forward * forwardStrokeForce * 0.05f * vertical);
                rb.AddTorque(Vector3.up * steerTorqueMultiplier * turningTorque * -horizontal, ForceMode.Force);
            }
        }

        private void ApplyBuoyancy()
        {
            if (buoyancyPoints == null || buoyancyPoints.Length == 0)
            {
                ApplySimpleBuoyancy();
                return;
            }

            foreach (Transform point in buoyancyPoints)
            {
                float depth = waterSurface.SurfaceHeight - point.position.y;
                if (depth > 0f)
                    rb.AddForceAtPosition(Vector3.up * depth * 9.81f, point.position, ForceMode.Acceleration);
            }
        }

        private void ApplySimpleBuoyancy()
        {
            float submergedAmount = Mathf.Max(0f, waterSurface.SurfaceHeight - transform.position.y);
            rb.AddForce(Vector3.up * submergedAmount * 9.81f, ForceMode.Acceleration);
        }

        public void AddWaterForce()
        {
            rb.AddForce(new Vector3(waterForceDirection.x, 0f, waterForceDirection.z) * waterForceMultiplier, ForceMode.Acceleration);
        }

        private void ApplyWaterDrag()
        {
            if (useExternalPluginDragForces) return;

            float speedFactor = rb.linearVelocity.magnitude;
            rb.linearDamping = dragInWater + speedFactor * 0.05f;
            rb.angularDamping = angularDragInWater + rb.angularVelocity.magnitude * 0.025f;

            Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
            localVelocity.x *= 0.8f;
            rb.linearVelocity = transform.TransformDirection(localVelocity);
        }

        private void StabilizeKayak()
        {
            float tilt = Vector3.Angle(transform.up, Vector3.up);
            if (tilt > 5f)
            {
                Vector3 correctionTorque = Vector3.Cross(transform.up, Vector3.up) * stability;
                rb.AddTorque(correctionTorque - rb.angularVelocity * 0.1f, ForceMode.Acceleration);
            }
        }

        public void PlayOneShot(float stereoPan = 0f)
        {
            audioSource.panStereo = stereoPan;
            audioSource.pitch = Random.Range(0.85f, 1.15f);
            audioSource.volume = Random.Range(0.8f, 1f);
            audioSource.PlayOneShot(audioClips[Random.Range(0, audioClips.Length)]);
        }
        // --- UI Button Methods for Mobile Steering ---
        private bool turnLeftPressed = false;
        private bool turnRightPressed = false;

        public void OnLeftUpButtonDown()
        {
            SetMoveDirection(1);
            turnLeftPressed = true;
        }

        public void OnLeftUpButtonUp()
        {
            turnLeftPressed = false;
            SetMoveDirection(0);
        }

        public void OnLeftDownButtonDown()
        {
            SetMoveDirection(-1);
            turnLeftPressed = true;
        }
        public void OnLeftDownButtonUp()
        {
            turnLeftPressed = false;
            SetMoveDirection(0);
        }

        public void OnRightUpButtonDown()
        {
            SetMoveDirection(1);
            turnRightPressed = true;
        }

        public void OnRightUpButtonUp()
        {
            turnRightPressed = false;
            SetMoveDirection(0);
        }
        public void OnRightDownButtonDown()
        {
            SetMoveDirection(-1);
            turnRightPressed = true;
        }
        public void OnRightDownButtonUp()
        {
            turnRightPressed = false;
            SetMoveDirection(0);
        }
        // --- Touch Drag Controls (for Mobile) ---
        private Vector2 dragStartPos;
        private bool isDragging = false;
        private float dragThreshold = 50f; // Minimum pixels before considering it a drag
        private int moveDirection = 0; // 1 = forward, -1 = reverse, 0 = idle

        private void UpdateTouchDrag()
        {
            /*if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        dragStartPos = touch.position;
                        isDragging = true;
                        break;

                    case TouchPhase.Moved:
                        if (!isDragging) return;

                        Vector2 dragDelta = touch.position - dragStartPos;

                        // Vertical drag detection
                        if (Mathf.Abs(dragDelta.y) > dragThreshold)
                        {
                            if (dragDelta.y > 0)
                            {
                                SetMoveDirection(1); // Forward
                            }
                            else
                            {
                                SetMoveDirection(-1); // Reverse
                            }

                            // Reset start position to avoid continuous triggering
                            dragStartPos = touch.position;
                        }

                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        isDragging = false;
                        break;
                }
            }*/

            // 🔹 Continuous movement based on direction
            if (moveDirection != 0)
            {
                MoveKayak(moveDirection);
            }
        }

// --- Drag Response Methods ---
        private void SetMoveDirection(int direction)
        {
            moveDirection = direction;
        }

        private void MoveKayak(int direction)
        {
            if (!isLeftDrawStroking && !isRightDrawStroking)
            {
                float targetSpeed = maxVelocity * 0.6f;
                float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward * direction);

                if (currentSpeed < targetSpeed)
                {
                    rb.AddForce(transform.forward * direction * forwardStrokeForce * 0.05f, ForceMode.Force);
                }
            }
        }

        void UpdateAnimations()
        {
            if (moveDirection == 1)
            {
                Debug.Log("Moving Forward");
                animator.SetBool("ForwardStroking", true);
                animator.SetBool("ReverseStroking", false);
            }
            else if (moveDirection == -1)
            {
                Debug.Log("Moving Reverse");
                animator.SetBool("ForwardStroking", false);
                animator.SetBool("ReverseStroking", true);
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            GameObject obj = other.gameObject;
            string tag = obj.tag;

            // Check if recently collided with this object
            if (lastHitTimes.ContainsKey(obj))
            {
                float lastHit = lastHitTimes[obj];
                if (Time.time - lastHit < hitCooldown)
                    return; // Ignore repeated hit within cooldown
            }

            // Record this hit time
            lastHitTimes[obj] = Time.time;

            // Handle obstacle damage
            int damage = 0;

            if (tag == "SmallObstacle") damage = 10;
            else if (tag == "MediumObstacle") damage = 20;
            else if (tag == "LargeObstacle") damage = 30;

            if (damage > 0)
            {
                health -= damage;
                health = Mathf.Max(health, 0); // Prevent negative health

                // Update UI
                UIManager uiManager = FindAnyObjectByType<UIManager>();
                if (uiManager != null)
                    uiManager.UpdateHealth();
            }
        }
    }
    
}
