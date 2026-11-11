using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotateSpeed = 720f;

    [Header("Parkour Settings")]
    [SerializeField] LayerMask environmentScannerMask;
    [SerializeField] Vector3 environmentRayOffset = new Vector3(0, 0.25f, 0);
    [SerializeField] float environmentRayForwardLength = 1f;
    [SerializeField] float environmentRayHeightLength = 5f;
    [SerializeField] ParkourActionSO[] parkourActions;

    CharacterController controller;
    Animator animator;
    Transform camTransform;
    PlayerInput playerInput;
    InputAction jumpAction;

    Vector2 moveInput;
    float verticalVelocity;

    bool inAction = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        camTransform = Camera.main.transform;
        playerInput = GetComponent<PlayerInput>();
        jumpAction = playerInput.actions.FindAction("Jump");
    }

    void Update()
    {
        Move();
        HandleParkour();
    }

    // Input Reading
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // Actions
    void Move()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        Vector3 moveDirection = Quaternion.AngleAxis(camTransform.eulerAngles.y, Vector3.up) * inputDirection;
        verticalVelocity = controller.isGrounded ? -2f : Physics.gravity.y * Time.deltaTime;
        Vector3 velocity = moveDirection + Vector3.up * verticalVelocity;

        if (inAction) return;

        controller.Move(velocity * moveSpeed * Time.deltaTime);
        animator.SetFloat("Speed", moveInput.magnitude, 0.1f, Time.deltaTime);

        if (moveInput != Vector2.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
    }

    void HandleParkour()
    {
        if (jumpAction.WasPerformedThisFrame() && !inAction)
        {
            // Casting a ray from the knee height of the player
            Vector3 rayForwardOrigin = transform.position + environmentRayOffset;
            bool isProp = Physics.Raycast(rayForwardOrigin, transform.forward, out RaycastHit hit, environmentRayForwardLength, environmentScannerMask);

            Debug.DrawRay(rayForwardOrigin, transform.forward * environmentRayForwardLength, isProp ? Color.red : Color.white);

            if (isProp)
            {
                // Casting a ray from the top in front of the player
                Vector3 rayHeightOrigin = hit.point + Vector3.up * environmentRayHeightLength;
                bool canParkour = Physics.Raycast(rayHeightOrigin, Vector3.down, out RaycastHit heightHit, environmentRayHeightLength, environmentScannerMask);

                Debug.DrawRay(rayHeightOrigin, Vector3.down * environmentRayHeightLength, canParkour ? Color.red : Color.white);

                IEnumerator PlayParkourAction(string stateName, AvatarTarget bodyPart, Vector3 weightMaskVector, float matchStart, float matchEnd, float fadeTime = 0.1f)
                {
                    inAction = true;
                    controller.enabled = false;
                    Vector3 faceDir = -hit.normal;
                    faceDir.y = 0;
                    Quaternion targetRotation = Quaternion.LookRotation(faceDir);

                    try
                    {
                        animator.CrossFadeInFixedTime(stateName, fadeTime);
                        yield return null; // let the Animator begin the transition

                        float duration = animator.GetNextAnimatorStateInfo(0).length;
                        bool matched = false;

                        float timer = 0f;
                        while (timer < duration / animator.speed)
                        {
                            timer += Time.deltaTime;
                            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
                            // MatchTarget is ignored while a transition is in progress
                            if (!matched && !animator.IsInTransition(0))
                            {
                                animator.MatchTarget(heightHit.point, transform.rotation, bodyPart, new MatchTargetWeightMask(weightMaskVector, 0f), matchStart, matchEnd);
                                matched = true;
                            }
                            yield return null;
                        }
                    }
                    finally
                    {
                        controller.enabled = true;
                        inAction = false;
                    }
                }

                // Using Scriptable Objects to use specific animation based on height of the obstacle
                foreach (ParkourActionSO parkourAction in parkourActions)
                {
                    if (!string.IsNullOrEmpty(parkourAction.tag) && hit.transform.CompareTag(parkourAction.tag))
                    {
                        StartCoroutine(PlayParkourAction(parkourAction.name, parkourAction.matchBodyPart, parkourAction.weightMaskVector, parkourAction.matchStartTime, parkourAction.matchTargetTime));
                        break;
                    }

                    float height = heightHit.point.y - transform.position.y;
                    if (height >= parkourAction.minHeight && height < parkourAction.maxHeight)
                    {
                        StartCoroutine(PlayParkourAction(parkourAction.name, parkourAction.matchBodyPart, parkourAction.weightMaskVector, parkourAction.matchStartTime, parkourAction.matchTargetTime));
                        break;
                    }
                }
            }
        }
    }
}
