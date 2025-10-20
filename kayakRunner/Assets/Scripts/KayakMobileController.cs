using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RageRunGames.KayakController
{
    [RequireComponent(typeof(KayakController))]
    [RequireComponent(typeof(Rigidbody))]
    public class KayakMobileController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KayakController kayakController;
        [SerializeField] private Rigidbody rb;

        [Header("UI Buttons")]
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        [Header("Settings")]
        [SerializeField] private float forwardSpeed = 4f;
        [SerializeField] private float steerAmount = 1.5f;
        [SerializeField] private float maxAngularVelocity = 3f;
        [SerializeField] private float paddleSoundDelay = 0.6f;

        private bool isTurningLeft;
        private bool isTurningRight;
        private Animator animator;
        private float nextPaddleSoundTime;

        private void Awake()
        {
            if (!rb)
                rb = GetComponent<Rigidbody>();

            if (!kayakController)
                kayakController = GetComponent<KayakController>();

            animator = GetComponentInChildren<Animator>(true);
            if (!animator)
                Debug.LogError("KayakMobileController: Animator not found in children!");

            // Set up UI buttons for press/hold behavior
            SetupButton(leftButton, OnLeftDown, OnLeftUp);
            SetupButton(rightButton, OnRightDown, OnRightUp);
        }

        private void FixedUpdate()
        {
            MoveForward();
            HandleSteering();
            UpdateAnimations();
        }

        private void MoveForward()
        {
            // Continuous forward motion
            rb.AddForce(transform.forward * forwardSpeed, ForceMode.Acceleration);

            // Clamp angular velocity for stability
            if (rb.angularVelocity.magnitude > maxAngularVelocity)
                rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
        }

        private void HandleSteering()
        {
            if (isTurningLeft)
            {
                rb.AddTorque(Vector3.up * -steerAmount, ForceMode.Acceleration);
                TryPlayPaddleSound(-0.3f);
            }
            else if (isTurningRight)
            {
                rb.AddTorque(Vector3.up * steerAmount, ForceMode.Acceleration);
                TryPlayPaddleSound(0.3f);
            }
            else
            {
                TryPlayPaddleSound(0f);
            }
        }

        private void UpdateAnimations()
        {
            bool movingStraight = !isTurningLeft && !isTurningRight;

            animator.SetBool("ForwardStroking", movingStraight);
            animator.SetBool("LeftForwardStroking", isTurningRight);
            animator.SetBool("RightForwardStroking", isTurningLeft);
        }

        private void TryPlayPaddleSound(float direction)
        {
            if (Time.time >= nextPaddleSoundTime)
            {
                kayakController.PlayOneShot(direction);
                nextPaddleSoundTime = Time.time + paddleSoundDelay;
            }
        }

        #region UI Button Handling
        private void OnLeftDown() => isTurningLeft = true;
        private void OnLeftUp() => isTurningLeft = false;
        private void OnRightDown() => isTurningRight = true;
        private void OnRightUp() => isTurningRight = false;

        private void SetupButton(Button button, UnityEngine.Events.UnityAction onDown, UnityEngine.Events.UnityAction onUp)
        {
            if (!button) return;

            var trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            // PointerDown
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener((_) => onDown.Invoke());
            trigger.triggers.Add(down);

            // PointerUp
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener((_) => onUp.Invoke());
            trigger.triggers.Add(up);
        }
        #endregion
    }
}
