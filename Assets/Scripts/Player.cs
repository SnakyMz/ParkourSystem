using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotateSpeed = 720f;

    CharacterController controller;
    Animator animator;
    Transform camTransform;
    Vector2 moveInput;
    float verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        camTransform = Camera.main.transform;
    }

    void Update()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        Vector3 moveDirection = Quaternion.AngleAxis(camTransform.eulerAngles.y, Vector3.up) * inputDirection;
        verticalVelocity = controller.isGrounded ? -2f : verticalVelocity + Physics.gravity.y * Time.deltaTime;
        Vector3 velocity = moveDirection + Vector3.up * verticalVelocity;

        controller.Move(velocity * moveSpeed * Time.deltaTime);
        animator.SetFloat("Speed", moveInput.magnitude, 0.1f, Time.deltaTime);

        if (moveInput != Vector2.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
