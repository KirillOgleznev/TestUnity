using UnityEngine;

public class LighthouseLight : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private bool clockwise = true;

    [Header("Light Effects")]
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.Constant(0, 1, 1);
    [SerializeField] private float baseIntensity = 5f;

    private Light spotLight;
    private float currentAngle;

    void Start()
    {
        spotLight = GetComponent<Light>();
    }

    void Update()
    {
        // Вращение
        float direction = clockwise ? 1f : -1f;
        currentAngle += rotationSpeed * direction * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0, currentAngle, 0);

        // Опционально: изменение интенсивности
        float normalizedAngle = (currentAngle % 360f) / 360f;
        spotLight.intensity = baseIntensity * intensityCurve.Evaluate(normalizedAngle);
    }
}