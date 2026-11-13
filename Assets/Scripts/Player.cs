using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotateSpeed = 500f;
    [SerializeField] float jumpHeight = 2.5f;
    [SerializeField] LayerMask groundMask;

    [Header("Parkour Settings")]
    [SerializeField] LayerMask environmentScannerMask;
    [SerializeField] LayerMask climbScannerMask;
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
    Vector3 velocity;
    [SerializeField] Climable currentClimbPoint = null;

    bool inAction = false;
    bool onGround = false;
    bool isHanging = false;

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
        HandleParkour();

        if (inAction || isHanging) return;

        GroundCheck();
        HandleMovement();
    }

    // Input Reading
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // Actions
    void GroundCheck()
    {
        Vector3 origin = transform.position + Vector3.up * controller.radius;
        float reach = controller.stepOffset + 0.1f;
        onGround = Physics.SphereCast(origin, controller.radius * 0.9f, Vector3.down, out _, reach, groundMask, QueryTriggerInteraction.Ignore);
        animator.SetBool("Grounded", onGround);
    }

    void HandleMovement()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        Vector3 moveDirection = Quaternion.AngleAxis(camTransform.eulerAngles.y, Vector3.up) * inputDirection;
        verticalVelocity = onGround ? verticalVelocity : verticalVelocity + Physics.gravity.y * Time.deltaTime;
        velocity = moveDirection + Vector3.up * verticalVelocity;

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
            if (!TryParkour() && !TryClimb() && !isHanging && onGround)
                verticalVelocity = jumpHeight;
        }
    }

    bool TryParkour()
    {
        if (isHanging) return false;

        // Casting a ray from the knee height of the player
        Vector3 rayForwardOrigin = transform.position + environmentRayOffset;
        if (!Physics.Raycast(rayForwardOrigin, transform.forward, out RaycastHit hit, environmentRayForwardLength, environmentScannerMask)) return false;

        // Casting a ray from the top in front of the player
        Vector3 rayHeightOrigin = hit.point + Vector3.up * environmentRayHeightLength;
        if (!Physics.Raycast(rayHeightOrigin, Vector3.down, out RaycastHit heightHit, environmentRayHeightLength, environmentScannerMask)) return false;

        Quaternion targetRotation = Quaternion.LookRotation(-hit.normal);

        // Using Scriptable Objects to use specific animation based on height of the obstacle
        foreach (ParkourActionSO parkourAction in parkourActions)
        {
            float height = heightHit.point.y - transform.position.y;
            if ((!string.IsNullOrEmpty(parkourAction.tag) && hit.transform.CompareTag(parkourAction.tag)) || (height >= parkourAction.minHeight && height < parkourAction.maxHeight))
            {
                StartCoroutine(PlayParkourAction(parkourAction.name, heightHit.point, targetRotation, parkourAction.matchBodyPart, parkourAction.weightMaskVector, parkourAction.matchStartTime, parkourAction.matchTargetTime));
                return true;
            }
        }

        return false;
    }

    bool TryClimb()
    {
        if (!isHanging)
        {
            Vector3 climbRayOrigin = transform.position + Vector3.up * 1.5f;
            Vector3 climbRayOffset = new Vector3(0, 0.1f, 0);

            for (int i = 0; i < 10; i++)
            {
                Debug.DrawRay(climbRayOrigin + climbRayOffset * i, transform.forward);

                if (Physics.Raycast(climbRayOrigin + climbRayOffset * i, transform.forward, out RaycastHit hit, environmentRayForwardLength, climbScannerMask))
                {
                    currentClimbPoint = hit.transform.GetComponent<Climable>();
                    HopTo(currentClimbPoint, "GrabLedge");
                    isHanging = true;
                    return true;
                }
            }
        }
        else
        {
            float x = Mathf.Round(moveInput.x);
            float y = Mathf.Round(moveInput.y);
            if (y == 1) HopTo(currentClimbPoint.upClimbPoint.climablePoint, "HopUp");
            else if (y == -1) HopTo(currentClimbPoint.downClimbPoint.climablePoint, "HopDown");
            else if (x == 1) HopTo(currentClimbPoint.rightClimbPoint.climablePoint, "HopRight");
            else if (x == -1) HopTo(currentClimbPoint.leftClimbPoint.climablePoint, "HopLeft");
        }
        return false;
    }

    void HopTo(Climable neighbor, string stateName)
    {
        if (neighbor == null) return; // no ledge in that direction

        Transform t = neighbor.transform;
        Quaternion rot = Quaternion.LookRotation(t.forward);
        StartCoroutine(PlayParkourAction(stateName, t.position, rot, AvatarTarget.RightHand, Vector3.one, 0.41f, 0.54f, true));
        currentClimbPoint = neighbor;
    }

    IEnumerator PlayParkourAction(string stateName, Vector3 position, Quaternion targetRotation, AvatarTarget bodyPart, Vector3 weightMaskVector, float matchStart, float matchEnd, bool isClimb = false, float fadeTime = 0.1f)
    {
        inAction = true;
        animator.applyRootMotion = true;
        controller.enabled = false;

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
                    position = isClimb ? position + new Vector3(0.3f, 0, 0) : position;
                    animator.MatchTarget(position, targetRotation, bodyPart, new MatchTargetWeightMask(weightMaskVector, 0f), matchStart, matchEnd);
                    matched = true;
                }
                yield return null;
            }
        }
        finally
        {
            animator.applyRootMotion = false;
            controller.enabled = true;
            inAction = false;
        }
    }
}
