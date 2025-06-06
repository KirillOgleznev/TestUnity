using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpellUI : MonoBehaviour
{
    [Header("Element Spheres")]
    public Image[] elementSpheres = new Image[3]; // 3 сферы для элементов
    public GameObject[] sphereGlows = new GameObject[3]; // Эффекты свечения (опционально)

    [Header("Element Colors")]
    public Color fireColor = Color.red;
    public Color waterColor = Color.blue;
    public Color earthColor = new Color(0.6f, 0.4f, 0.2f);
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Цвет пустой сферы

    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0.8f, 1f, 1f);
    public bool useScaleAnimation = true;
    public bool useColorFadeAnimation = true;

    [Header("Spell Name Display")]
    public TextMeshProUGUI spellNameText;
    public float spellNameFadeDuration = 2f;

    [Header("Element Names (Optional)")]
    public TextMeshProUGUI[] elementNameTexts = new TextMeshProUGUI[3];

    // Private fields
    private SpellCaster spellCaster;
    private List<ElementType> currentElements = new List<ElementType>();
    private Coroutine[] sphereAnimations = new Coroutine[3];
    private Coroutine spellNameFadeCoroutine;

    // Spell name dictionary
    private Dictionary<string, string> spellNames = new Dictionary<string, string>
    {
        // Простые заклинания
        {"Fire", "Fireball"},
        {"Water", "Water Bolt"},
        {"Earth", "Earth Spike"},
        
        // Комбинированные заклинания
        {"Fire,Fire", "Greater Fireball"},
        {"Water,Water", "Tsunami"},
        {"Earth,Earth", "Rock Wall"},
        {"Water,Fire", "Steam Blast"},
        {"Fire,Water", "Steam Blast"},
        {"Fire,Earth", "Lava Blast"},
        {"Earth,Fire", "Lava Blast"},
        {"Water,Earth", "Mud Slide"},
        {"Earth,Water", "Mud Slide"},
        
        // Сложные заклинания
        {"Fire,Fire,Fire", "METEOR"},
        {"Water,Water,Water", "FLOOD"},
        {"Earth,Earth,Earth", "EARTHQUAKE"},
        {"Fire,Water,Earth", "ELEMENTAL STORM"},
        {"Water,Fire,Earth", "ELEMENTAL STORM"},
        {"Earth,Fire,Water", "ELEMENTAL STORM"},
        {"Fire,Earth,Water", "ELEMENTAL STORM"},
        {"Water,Earth,Fire", "ELEMENTAL STORM"},
        {"Earth,Water,Fire", "ELEMENTAL STORM"}
    };

    void Start()
    {
        // Находим SpellCaster
        spellCaster = FindObjectOfType<SpellCaster>();
        if (spellCaster == null)
        {
            Debug.LogError("SpellCaster not found! Make sure SpellCaster component exists in the scene.");
            return;
        }

        // Подписываемся на события
        spellCaster.OnElementsChanged.AddListener(UpdateElementDisplay);
        spellCaster.OnSpellCasted.AddListener(ShowSpellName);
        spellCaster.OnCastingStateChanged.AddListener(OnCastingStateChanged);

        // Инициализируем UI
        InitializeUI();
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (spellCaster != null)
        {
            spellCaster.OnElementsChanged.RemoveListener(UpdateElementDisplay);
            spellCaster.OnSpellCasted.RemoveListener(ShowSpellName);
            spellCaster.OnCastingStateChanged.RemoveListener(OnCastingStateChanged);
        }
    }

    private void InitializeUI()
    {
        // Проверяем, что все сферы назначены
        for (int i = 0; i < elementSpheres.Length; i++)
        {
            if (elementSpheres[i] == null)
            {
                Debug.LogError($"Element sphere {i} is not assigned!");
                continue;
            }

            // Устанавливаем начальный цвет (пустая сфера)
            elementSpheres[i].color = emptyColor;

            // Устанавливаем начальный масштаб
            elementSpheres[i].transform.localScale = Vector3.one;
        }

        // Скрываем текст заклинания
        if (spellNameText != null)
        {
            spellNameText.text = "";
            spellNameText.color = new Color(spellNameText.color.r, spellNameText.color.g, spellNameText.color.b, 0f);
        }

        // Скрываем названия элементов
        for (int i = 0; i < elementNameTexts.Length; i++)
        {
            if (elementNameTexts[i] != null)
            {
                elementNameTexts[i].text = "";
            }
        }
    }

    public void UpdateElementDisplay(List<ElementType> elements)
    {
        currentElements = new List<ElementType>(elements);

        // Обновляем каждую сферу
        for (int i = 0; i < elementSpheres.Length; i++)
        {
            if (elementSpheres[i] == null) continue;

            ElementType elementType = ElementType.None;
            if (i < elements.Count)
            {
                elementType = elements[i];
            }

            UpdateSphere(i, elementType);
        }

        // Обновляем preview заклинания
        UpdateSpellPreview();
    }

    private void UpdateSphere(int sphereIndex, ElementType elementType)
    {
        if (sphereIndex >= elementSpheres.Length || elementSpheres[sphereIndex] == null)
            return;

        Color targetColor = GetElementColor(elementType);
        Image sphere = elementSpheres[sphereIndex];

        // Останавливаем предыдущую анимацию
        if (sphereAnimations[sphereIndex] != null)
        {
            StopCoroutine(sphereAnimations[sphereIndex]);
        }

        // Запускаем анимацию обновления сферы
        sphereAnimations[sphereIndex] = StartCoroutine(AnimateSphere(sphere, targetColor, elementType != ElementType.None));

        // Обновляем название элемента
        if (sphereIndex < elementNameTexts.Length && elementNameTexts[sphereIndex] != null)
        {
            elementNameTexts[sphereIndex].text = elementType == ElementType.None ? "" : elementType.ToString();
        }

        // Обновляем эффект свечения
        if (sphereIndex < sphereGlows.Length && sphereGlows[sphereIndex] != null)
        {
            sphereGlows[sphereIndex].SetActive(elementType != ElementType.None);
        }
    }

    private IEnumerator AnimateSphere(Image sphere, Color targetColor, bool isActive)
    {
        Color startColor = sphere.color;
        Vector3 startScale = sphere.transform.localScale;
        Vector3 targetScale = Vector3.one;

        // Если сфера активируется, делаем bounce эффект
        if (isActive && useScaleAnimation)
        {
            targetScale = Vector3.one * 1.2f; // Немного увеличиваем
        }

        float elapsed = 0f;

        // Анимация цвета и масштаба
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;

            // Анимация цвета
            if (useColorFadeAnimation)
            {
                sphere.color = Color.Lerp(startColor, targetColor, progress);
            }
            else
            {
                sphere.color = targetColor;
            }

            // Анимация масштаба
            if (useScaleAnimation)
            {
                float scaleProgress = scaleCurve.Evaluate(progress);
                sphere.transform.localScale = Vector3.Lerp(startScale, targetScale, scaleProgress);
            }

            yield return null;
        }

        // Финальные значения
        sphere.color = targetColor;

        // Возвращаем масштаб к нормальному, если был bounce
        if (isActive && useScaleAnimation)
        {
            yield return new WaitForSeconds(0.1f);

            elapsed = 0f;
            while (elapsed < animationDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (animationDuration * 0.5f);
                sphere.transform.localScale = Vector3.Lerp(targetScale, Vector3.one, progress);
                yield return null;
            }
        }

        sphere.transform.localScale = Vector3.one;
    }

    private Color GetElementColor(ElementType elementType)
    {
        switch (elementType)
        {
            case ElementType.Fire: return fireColor;
            case ElementType.Water: return waterColor;
            case ElementType.Earth: return earthColor;
            default: return emptyColor;
        }
    }

    private void UpdateSpellPreview()
    {
        if (spellNameText == null) return;

        string comboString = GetCurrentComboString();

        if (string.IsNullOrEmpty(comboString))
        {
            spellNameText.text = "";
            return;
        }

        if (spellNames.ContainsKey(comboString))
        {
            spellNameText.text = spellNames[comboString];
            spellNameText.color = new Color(spellNameText.color.r, spellNameText.color.g, spellNameText.color.b, 0.6f);
        }
        else
        {
            spellNameText.text = "Unknown Spell";
            spellNameText.color = new Color(1f, 0.5f, 0.5f, 0.6f); // Красноватый для неизвестного заклинания
        }
    }

    private string GetCurrentComboString()
    {
        if (currentElements.Count == 0) return "";

        string combo = currentElements[0].ToString();
        for (int i = 1; i < currentElements.Count; i++)
        {
            combo += "," + currentElements[i].ToString();
        }
        return combo;
    }

    public void ShowSpellName(string spellCombo)
    {
        if (spellNameText == null) return;

        if (spellNameFadeCoroutine != null)
        {
            StopCoroutine(spellNameFadeCoroutine);
        }

        if (spellNames.ContainsKey(spellCombo))
        {
            spellNameText.text = spellNames[spellCombo] + " CAST!";
            spellNameText.color = new Color(spellNameText.color.r, spellNameText.color.g, spellNameText.color.b, 1f);

            spellNameFadeCoroutine = StartCoroutine(FadeOutSpellName());
        }
    }

    private IEnumerator FadeOutSpellName()
    {
        yield return new WaitForSeconds(1f); // Показываем 1 секунду

        Color startColor = spellNameText.color;
        float elapsed = 0f;

        while (elapsed < spellNameFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / spellNameFadeDuration);
            spellNameText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        spellNameText.text = "";
    }

    private void OnCastingStateChanged(bool isCasting)
    {
        // Можно добавить эффекты во время каста
        // Например, пульсация сфер или изменение их яркости

        for (int i = 0; i < elementSpheres.Length; i++)
        {
            if (elementSpheres[i] == null) continue;

            if (isCasting)
            {
                // Делаем сферы ярче во время каста
                Color currentColor = elementSpheres[i].color;
                elementSpheres[i].color = new Color(currentColor.r, currentColor.g, currentColor.b, currentColor.a * 1.5f);
            }
            else
            {
                // Возвращаем нормальную яркость
                UpdateSphere(i, i < currentElements.Count ? currentElements[i] : ElementType.None);
            }
        }
    }

    #region Public Methods для настройки

    public void SetAnimationDuration(float duration)
    {
        animationDuration = duration;
    }

    public void SetUseAnimations(bool useScale, bool useColorFade)
    {
        useScaleAnimation = useScale;
        useColorFadeAnimation = useColorFade;
    }

    public void SetElementColors(Color fire, Color water, Color earth, Color empty)
    {
        fireColor = fire;
        waterColor = water;
        earthColor = earth;
        emptyColor = empty;

        // Обновляем текущее отображение
        UpdateElementDisplay(currentElements);
    }

    #endregion
}