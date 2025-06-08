using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitalElementSpheres : MonoBehaviour
{
    [Header("Sphere References")]
    [HideInInspector]
    public GameObject[] elementSpheres = new GameObject[3]; // 3 сферы для элементов
    public Transform sphereParent; // Родитель для сфер (если нужен)

    [Header("Orbital Settings")]
    public float orbitRadius = 2f; // Радиус орбиты вокруг персонажа
    public float orbitSpeed = 30f; // Скорость вращения (градусы в секунду)
    public float sphereHeight = 1.5f; // Высота сфер над землей
    public Vector3 orbitOffset = Vector3.zero; // Смещение центра орбиты

    [Header("Sphere Positioning")]
    public float sphereSpacing = 120f; // Угол между сферами (120° = равномерно)
    public float[] sphereHeightOffsets = { 0f, 0.2f, -0.2f }; // Разная высота для каждой сферы

    [Header("Animation Settings")]
    public float appearDuration = 0.5f;
    public float disappearDuration = 0.3f;
    public AnimationCurve appearCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve disappearCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Visual Effects")]
    public bool useFloatingAnimation = true;
    public float floatAmplitude = 0.1f; // Амплитуда покачивания
    public float floatFrequency = 2f; // Частота покачивания
    public bool usePulseEffect = true;
    public float pulseSpeed = 3f;
    public float pulseIntensity = 0.2f;

    [Header("Materials")]
    public Material fireElementMaterial;
    public Material waterElementMaterial;
    public Material earthElementMaterial;
    public Material emptyElementMaterial;

    // Private fields
    private SpellCaster spellCaster;
    private List<ElementType> currentElements = new List<ElementType>();
    private float currentOrbitAngle = 0f;
    private Vector3[] targetPositions = new Vector3[3];
    private bool[] sphereActiveStates = new bool[3];
    private Coroutine[] sphereAnimations = new Coroutine[3];
    private Renderer[] sphereRenderers = new Renderer[3];
    private Vector3[] basePositions = new Vector3[3];
    private float[] floatOffsets = new float[3]; // Для разнофазного покачивания

    void Start()
    {
        // Находим SpellCaster
        spellCaster = FindObjectOfType<SpellCaster>();
        if (spellCaster == null)
        {
            Debug.LogError("SpellCaster not found!");
            return;
        }

        // Подписываемся на события
        spellCaster.OnElementsChanged.AddListener(UpdateOrbitalSpheres);
        spellCaster.OnCastingStateChanged.AddListener(OnCastingStateChanged);

        // Инициализируем сферы
        InitializeSpheres();

        // Устанавливаем случайные смещения для floating анимации
        for (int i = 0; i < floatOffsets.Length; i++)
        {
            floatOffsets[i] = Random.Range(0f, 2f * Mathf.PI);
        }
    }

    void Update()
    {
        UpdateOrbitalMovement();

        if (useFloatingAnimation)
        {
            UpdateFloatingAnimation();
        }

        if (usePulseEffect)
        {
            UpdatePulseEffect();
        }
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (spellCaster != null)
        {
            spellCaster.OnElementsChanged.RemoveListener(UpdateOrbitalSpheres);
            spellCaster.OnCastingStateChanged.RemoveListener(OnCastingStateChanged);
        }
    }

    private void InitializeSpheres()
    {
        // Если сферы не созданы, создаем их
        for (int i = 0; i < elementSpheres.Length; i++)
        {
            if (elementSpheres[i] == null)
            {
                elementSpheres[i] = CreateElementSphere(i);
            }

            // Получаем рендерер
            sphereRenderers[i] = elementSpheres[i].GetComponent<Renderer>();

            // Устанавливаем начальное состояние (неактивная сфера)
            SetSphereState(i, ElementType.None, false);
        }

        // Вычисляем базовые позиции
        CalculateBasePositions();
    }

    private GameObject CreateElementSphere(int index)
    {
        // Создаем сферу
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = $"ElementSphere_{index + 1}";
        sphere.transform.localScale = Vector3.one * 0.3f; // Маленький размер

        // Устанавливаем родителя, если указан
        if (sphereParent != null)
        {
            sphere.transform.SetParent(sphereParent);
        }

        // Удаляем коллайдер (не нужен для визуального эффекта)
        Collider sphereCollider = sphere.GetComponent<Collider>();
        if (sphereCollider != null)
        {
            DestroyImmediate(sphereCollider);
        }

        // Добавляем компонент для particle effects (опционально)
        AddParticleEffects(sphere);

        return sphere;
    }

    private void AddParticleEffects(GameObject sphere)
    {
        // Создаем красивый particle system для мерцания
        GameObject particleObj = new GameObject("SphereParticles");
        particleObj.transform.SetParent(sphere.transform);
        particleObj.transform.localPosition = Vector3.zero;

        ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();

        // Main модуль
        var main = particles.main;
        main.startLifetime = 2f;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.03f); // Случайный размер от 0.01 до 0.03
        main.maxParticles = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = Color.white; // Цвет будет браться из материала

        // Emission модуль
        var emission = particles.emission;
        emission.rateOverTime = 8f;

        // Shape модуль
        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f; // Радиус появления частиц

        // Velocity over Lifetime (частицы разлетаются)
        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(0.5f);

        // Color over Lifetime (частицы угасают)
        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = gradient;

        // Size over Lifetime (частицы уменьшаются)
        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Renderer модуль - используем материал сферы
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = sphere.GetComponent<Renderer>().material;

        // Отключаем по умолчанию
        particles.Stop();
    }

    private void CalculateBasePositions()
    {
        for (int i = 0; i < targetPositions.Length; i++)
        {
            float angle = i * sphereSpacing * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * orbitRadius;
            float z = Mathf.Sin(angle) * orbitRadius;
            float y = sphereHeight + sphereHeightOffsets[i];

            basePositions[i] = new Vector3(x, y, z) + orbitOffset;
        }
    }

    public void UpdateOrbitalSpheres(List<ElementType> elements)
    {
        currentElements = new List<ElementType>(elements);

        // Обновляем каждую сферу
        for (int i = 0; i < elementSpheres.Length; i++)
        {
            ElementType elementType = ElementType.None;
            bool isActive = false;

            if (i < elements.Count)
            {
                elementType = elements[i];
                isActive = true;
            }

            SetSphereState(i, elementType, isActive);
        }
    }

    private void SetSphereState(int sphereIndex, ElementType elementType, bool isActive)
    {
        if (sphereIndex >= elementSpheres.Length || elementSpheres[sphereIndex] == null)
            return;

        GameObject sphere = elementSpheres[sphereIndex];
        bool wasActive = sphereActiveStates[sphereIndex];
        sphereActiveStates[sphereIndex] = isActive;

        // Останавливаем предыдущую анимацию
        if (sphereAnimations[sphereIndex] != null)
        {
            StopCoroutine(sphereAnimations[sphereIndex]);
        }

        // Устанавливаем материал
        SetSphereMaterial(sphereIndex, elementType);

        // Анимируем появление/исчезновение
        if (isActive && !wasActive)
        {
            // Сфера появляется
            sphereAnimations[sphereIndex] = StartCoroutine(AnimateSphereAppear(sphere));
        }
        else if (!isActive && wasActive)
        {
            // Сфера исчезает
            sphereAnimations[sphereIndex] = StartCoroutine(AnimateSphereDisappear(sphere));
        }
        else if (isActive)
        {
            // Сфера уже активна, просто обновляем материал
            sphere.SetActive(true);
            sphere.transform.localScale = Vector3.one * 0.3f;
        }

        // Управляем particle effects
        ParticleSystem particles = sphere.GetComponentInChildren<ParticleSystem>();
        if (particles != null)
        {
            if (isActive)
            {
                particles.Play();
                SetParticleColor(particles, elementType);
            }
            else
            {
                particles.Stop();
            }
        }
    }

    private void SetSphereMaterial(int sphereIndex, ElementType elementType)
    {
        if (sphereRenderers[sphereIndex] == null) return;

        Material targetMaterial = emptyElementMaterial;

        switch (elementType)
        {
            case ElementType.Fire:
                targetMaterial = fireElementMaterial;
                break;
            case ElementType.Water:
                targetMaterial = waterElementMaterial;
                break;
            case ElementType.Earth:
                targetMaterial = earthElementMaterial;
                break;
        }

        if (targetMaterial != null)
        {
            sphereRenderers[sphereIndex].material = targetMaterial;
        }
        else
        {
            // Если материал не назначен, используем цвет по умолчанию
            SetSphereColor(sphereIndex, GetElementColor(elementType));
        }
    }

    private void SetSphereColor(int sphereIndex, Color color)
    {
        if (sphereRenderers[sphereIndex] != null)
        {
            sphereRenderers[sphereIndex].material.color = color;
        }
    }

    private Color GetElementColor(ElementType elementType)
    {
        switch (elementType)
        {
            case ElementType.Fire: return new Color(1f, 0.3f, 0.1f); // Ярко-красный
            case ElementType.Water: return new Color(0.1f, 0.5f, 1f); // Ярко-синий
            case ElementType.Earth: return new Color(0.6f, 0.4f, 0.2f); // Коричневый
            default: return new Color(0.3f, 0.3f, 0.3f, 0.3f); // Прозрачный серый
        }
    }

    private void SetParticleColor(ParticleSystem particles, ElementType elementType)
    {
        var main = particles.main;
        main.startColor = GetElementColor(elementType);
    }

    private IEnumerator AnimateSphereAppear(GameObject sphere)
    {
        sphere.SetActive(true);
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one * 0.3f;

        float elapsed = 0f;

        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / appearDuration;
            float curveValue = appearCurve.Evaluate(progress);

            sphere.transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);

            // Добавляем небольшое вращение при появлении
            sphere.transform.Rotate(Vector3.up, 360f * Time.deltaTime);

            yield return null;
        }

        sphere.transform.localScale = targetScale;
    }

    private IEnumerator AnimateSphereDisappear(GameObject sphere)
    {
        Vector3 startScale = sphere.transform.localScale;
        Vector3 targetScale = Vector3.zero;

        float elapsed = 0f;

        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / disappearDuration;
            float curveValue = disappearCurve.Evaluate(progress);

            sphere.transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);

            yield return null;
        }

        sphere.transform.localScale = targetScale;
        sphere.SetActive(false);
    }

    private void UpdateOrbitalMovement()
    {
        // Обновляем угол орбиты
        currentOrbitAngle += orbitSpeed * Time.deltaTime;
        if (currentOrbitAngle >= 360f)
        {
            currentOrbitAngle -= 360f;
        }

        // Позиционируем каждую сферу
        for (int i = 0; i < elementSpheres.Length; i++)
        {
            if (elementSpheres[i] == null || !sphereActiveStates[i]) continue;

            // Вычисляем позицию на орбите
            float sphereAngle = (currentOrbitAngle + i * sphereSpacing) * Mathf.Deg2Rad;
            float x = Mathf.Cos(sphereAngle) * orbitRadius;
            float z = Mathf.Sin(sphereAngle) * orbitRadius;
            float y = sphereHeight + sphereHeightOffsets[i];

            Vector3 orbitPosition = new Vector3(x, y, z) + orbitOffset;

            // Применяем позицию относительно персонажа
            targetPositions[i] = transform.position + orbitPosition;
            elementSpheres[i].transform.position = targetPositions[i];
        }
    }

    private void UpdateFloatingAnimation()
    {
        for (int i = 0; i < elementSpheres.Length; i++)
        {
            if (elementSpheres[i] == null || !sphereActiveStates[i]) continue;

            // Добавляем вертикальное покачивание
            float floatOffset = Mathf.Sin((Time.time + floatOffsets[i]) * floatFrequency) * floatAmplitude;
            Vector3 currentPos = elementSpheres[i].transform.position;
            currentPos.y = targetPositions[i].y + floatOffset;
            elementSpheres[i].transform.position = currentPos;
        }
    }

    private void UpdatePulseEffect()
    {
        for (int i = 0; i < elementSpheres.Length; i++)
        {
            if (elementSpheres[i] == null || !sphereActiveStates[i]) continue;

            // Пульсация масштаба
            float pulseValue = 1f + Mathf.Sin(Time.time * pulseSpeed + i) * pulseIntensity;
            Vector3 baseScale = Vector3.one * 0.3f;
            elementSpheres[i].transform.localScale = baseScale * pulseValue;
        }
    }

    private void OnCastingStateChanged(bool isCasting)
    {
        if (isCasting)
        {
            // Во время каста ускоряем вращение и увеличиваем пульсацию
            orbitSpeed *= 2f;
            pulseIntensity *= 1.5f;
        }
        else
        {
            // Возвращаем нормальные значения
            orbitSpeed /= 2f;
            pulseIntensity /= 1.5f;
        }
    }

    #region Public Methods для настройки

    public void SetOrbitRadius(float radius)
    {
        orbitRadius = radius;
        CalculateBasePositions();
    }

    public void SetOrbitSpeed(float speed)
    {
        orbitSpeed = speed;
    }

    public void SetSphereHeight(float height)
    {
        sphereHeight = height;
        CalculateBasePositions();
    }

    public void SetAnimationSettings(bool useFloating, bool usePulse)
    {
        useFloatingAnimation = useFloating;
        usePulseEffect = usePulse;
    }

    #endregion
}