using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ObstacleSlidingMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 10f;

    [Header("Capsule Cast Settings")]
    public float castDistance = 0.17f;

    [Header("Debug")]
    public bool drawDebug = true;

    private CharacterController _controller;
    private Transform _transform;
    private Camera _camera;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _transform = transform;
        _camera = Camera.main;
    }

    void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude < 0.01f)
            return;

        Vector3 moveDir = new Vector3(input.x, 0, input.y).normalized;

        // Поворот направления относительно камеры
        float yaw = _camera.transform.eulerAngles.y;
        Vector3 worldMove = Quaternion.Euler(0, yaw, 0) * moveDir;

        // Параметры капсулы
        Vector3 p1 = _transform.position + Vector3.up * (_controller.height / 2 - _controller.radius);
        Vector3 p2 = _transform.position - Vector3.up * (_controller.height / 2 - _controller.radius);

        if (Physics.CapsuleCast(p1, p2, _controller.radius, worldMove, out RaycastHit hit, castDistance))
        {
            // Всегда проецируем движение на плоскость препятствия
            Vector3 slide = Vector3.ProjectOnPlane(worldMove, hit.normal).normalized;

            if (drawDebug)
            {
                Debug.DrawRay(hit.point, hit.normal, Color.red);
                Debug.DrawRay(_transform.position, worldMove, Color.green);
                Debug.DrawRay(_transform.position, slide, Color.blue);
            }

            _controller.Move(slide * speed * Time.deltaTime);
        }
        else
        {
            _controller.Move(worldMove * speed * Time.deltaTime);
        }
    }
}