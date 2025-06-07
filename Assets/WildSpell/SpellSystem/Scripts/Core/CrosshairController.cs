using UnityEngine;

public class CrosshairController : MonoBehaviour
{
    [Header("Настройки прицела")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private RectTransform crosshairUI;
    [SerializeField] private LayerMask aimLayerMask = -1;
    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private Transform playerTransform;

    [Header("Отладка")]
    [SerializeField] private bool showDebugRay = true;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerTransform == null)
            playerTransform = GameObject.FindWithTag("Player")?.transform;
    }

    public Vector3 GetTargetPoint()
    {
        if (playerCamera == null || crosshairUI == null)
            return Vector3.zero;

        // ИСПРАВЛЕНИЕ: правильное получение позиции UI элемента
        Vector3 crosshairScreenPos;

        // Если Canvas в Screen Space - Overlay
        Canvas canvas = crosshairUI.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Для Screen Space - Overlay используем позицию напрямую
            crosshairScreenPos = crosshairUI.position;
        }
        else
        {
            // Для других режимов Canvas
            crosshairScreenPos = RectTransformUtility.WorldToScreenPoint(playerCamera, crosshairUI.position);
        }

        // Луч от камеры через прицел
        Ray cameraRay = playerCamera.ScreenPointToRay(crosshairScreenPos);

        RaycastHit[] hits = Physics.RaycastAll(cameraRay, maxAimDistance, aimLayerMask);

        foreach (RaycastHit hit in hits)
        {
            if (playerTransform != null && hit.transform.IsChildOf(playerTransform))
                continue;

            return hit.point;
        }

        return cameraRay.origin + cameraRay.direction * maxAimDistance;
    }

    public Vector3 GetDirectionFromPoint(Vector3 startPosition)
    {
        Vector3 targetPoint = GetTargetPoint();
        return (targetPoint - startPosition).normalized;
    }

    void Update()
    {
        if (showDebugRay)
        {
            Vector3 targetPoint = GetTargetPoint();
            Debug.DrawLine(playerCamera.transform.position, targetPoint, Color.green, 0.1f);
            Debug.DrawRay(targetPoint, Vector3.up * 1f, Color.red, 0.1f);
        }
    }
}