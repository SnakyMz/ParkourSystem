using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotateSpeed = 720f;
    [SerializeField] float gravity = -20f;

    CharacterController cc;
    Transform cam;
    Vector2 moveInput;
    float verticalVelocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cam = Camera.main.transform;
    }

    void Update()
    {
        // --- Horizontal ---
        Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;

        Vector3 moveDir = Vector3.ClampMagnitude(camForward * moveInput.y + camRight * moveInput.x, 1f);
        Vector3 horizontalVelocity = moveDir * moveSpeed;

        // --- Vertical ---
        if (cc.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // small downward force keeps the controller grounded on slopes

        verticalVelocity += gravity * Time.deltaTime;

        // --- Single Move call ---
        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        cc.Move(velocity * Time.deltaTime);

        // --- Rotation ---
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
