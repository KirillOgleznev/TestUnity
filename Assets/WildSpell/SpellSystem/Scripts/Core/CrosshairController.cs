using UnityEngine;

public class CrosshairController : MonoBehaviour
{
    [Header("��������� �������")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private RectTransform crosshairUI;
    [SerializeField] private LayerMask aimLayerMask = -1;
    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private Transform playerTransform;

    [Header("�������")]
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

        Vector3 crosshairScreenPos;
        Canvas canvas = crosshairUI.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            crosshairScreenPos = crosshairUI.position;
        }
        else
        {
            crosshairScreenPos = RectTransformUtility.WorldToScreenPoint(playerCamera, crosshairUI.position);
        }

        Ray cameraRay = playerCamera.ScreenPointToRay(crosshairScreenPos);
        RaycastHit[] hits = Physics.RaycastAll(cameraRay, maxAimDistance, aimLayerMask);

        // ��������� �� ����������
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

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

            // ������ ��� ��� �������
            Ray cameraRay = playerCamera.ScreenPointToRay(crosshairUI.position);
            RaycastHit[] hits = Physics.RaycastAll(cameraRay, maxAimDistance, aimLayerMask);
            Debug.Log($"Raycast ����� {hits.Length} ��������");
            foreach (var hit in hits)
            {
                Debug.Log($"����� �: {hit.collider.name}, IsChild: {hit.transform.IsChildOf(playerTransform)}");
            }

            // ������� - ����������� ������� ������
            Debug.DrawLine(playerCamera.transform.position, targetPoint, Color.green, 0.1f);

            // ����� - ���������� ������� �� ���������
            if (playerTransform != null)
            {
                Debug.DrawLine(playerTransform.position, targetPoint, Color.blue, 0.1f);
            }

            // ������� ����� � ����� ���������
            float crossSize = 0.5f;
            Debug.DrawLine(targetPoint + Vector3.left * crossSize, targetPoint + Vector3.right * crossSize, Color.red, 0.1f);
            Debug.DrawLine(targetPoint + Vector3.forward * crossSize, targetPoint + Vector3.back * crossSize, Color.red, 0.1f);
            Debug.DrawLine(targetPoint + Vector3.up * crossSize, targetPoint + Vector3.down * crossSize, Color.red, 0.1f);
        }
    }
}