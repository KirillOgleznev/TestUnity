using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Linq;

public class SpellCasterBackup : MonoBehaviour
{
    [Header("References")]
    public Transform spellSpawnPoint;
    public Transform[] elementSpherePositions = new Transform[3]; // Позиции для UI сфер

    [Header("Spell Settings")]
    public float baseCastTime = 1.0f;
    public float castRange = 10f;
    public LayerMask targetLayers = 1;

    [Header("Available Spells")]
    [SerializeField] private List<BaseSpell> availableSpells = new List<BaseSpell>();

    [Header("Element Input Keys")]
    public KeyCode fireKey = KeyCode.Alpha1;
    public KeyCode waterKey = KeyCode.Alpha2;
    public KeyCode earthKey = KeyCode.Alpha3;

    [Header("Cast Input")]
    public KeyCode castKey = KeyCode.Mouse0;
    public KeyCode cancelKey = KeyCode.Mouse1;

    [Header("Events")]
    public UnityEvent<bool> OnCastingStateChanged;
    public UnityEvent<List<ElementType>> OnElementsChanged;
    public UnityEvent<string> OnSpellCasted;
    public UnityEvent<BaseSpell> OnSpellFound;

    // Private fields
    private ThirdPersonController thirdPersonController;
    private PlayerInput playerInput;

    private List<ElementType> currentElements = new List<ElementType>();
    private const int MaxElements = 3;

    private bool isCasting = false;
    private bool canCast = true;
    private BaseSpell currentSpell = null;
    private Coroutine castingCoroutine = null;

    // Словарь для быстрого поиска заклинаний по комбинации элементов
    private Dictionary<string, BaseSpell> spellLookup = new Dictionary<string, BaseSpell>();

    void Start()
    {
        // Получаем ссылки на компоненты
        thirdPersonController = GetComponent<ThirdPersonController>();
        playerInput = GetComponent<PlayerInput>();

        // Инициализируем систему заклинаний
        InitializeSpellSystem();
    }

    void Update()
    {
        HandleElementInput();
        HandleCastInput();
        UpdateCurrentSpell();
    }

    private void InitializeSpellSystem()
    {
        // Получаем все заклинания из дочерних объектов если они не назначены
        if (availableSpells.Count == 0)
        {
            availableSpells.AddRange(GetComponentsInChildren<BaseSpell>());
        }

        // Строим словарь для поиска заклинаний
        BuildSpellLookup();

        // Очищаем начальные элементы
        currentElements.Clear();
        OnElementsChanged?.Invoke(currentElements);
    }

    private void BuildSpellLookup()
    {
        spellLookup.Clear();

        foreach (var spell in availableSpells)
        {
            if (spell != null && spell.RequiredElements != null && spell.RequiredElements.Count > 0)
            {
                string key = ElementsToString(spell.RequiredElements);
                
                // Добавляем все возможные перестановки для комбинированных заклинаний
                var permutations = GetAllPermutations(spell.RequiredElements);
                foreach (var permutation in permutations)
                {
                    string permKey = ElementsToString(permutation);
                    if (!spellLookup.ContainsKey(permKey))
                    {
                        spellLookup[permKey] = spell;
                    }
                }
            }
        }

        Debug.Log($"Initialized {spellLookup.Count} spell combinations");
    }

    private List<List<ElementType>> GetAllPermutations(List<ElementType> elements)
    {
        var result = new List<List<ElementType>>();
        
        // Для простых заклинаний (1 элемент) добавляем как есть
        if (elements.Count == 1)
        {
            result.Add(new List<ElementType>(elements));
            return result;
        }

        // Для сложных заклинаний добавляем все уникальные перестановки
        var uniqueElements = elements.Distinct().ToList();
        if (uniqueElements.Count == 1)
        {
            // Все элементы одинаковые (например, Fire,Fire,Fire)
            result.Add(new List<ElementType>(elements));
        }
        else
        {
            // Генерируем все перестановки
            result.AddRange(GeneratePermutations(elements));
        }

        return result;
    }

    private List<List<ElementType>> GeneratePermutations(List<ElementType> elements)
    {
        var result = new List<List<ElementType>>();
        
        if (elements.Count <= 1)
        {
            result.Add(new List<ElementType>(elements));
            return result;
        }

        for (int i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            var remaining = new List<ElementType>(elements);
            remaining.RemoveAt(i);

            var subPermutations = GeneratePermutations(remaining);
            foreach (var subPerm in subPermutations)
            {
                var newPerm = new List<ElementType> { element };
                newPerm.AddRange(subPerm);
                result.Add(newPerm);
            }
        }

        return result;
    }

    private void HandleElementInput()
    {
        if (isCasting || !canCast) return;

        // Проверяем нажатие клавиш элементов
        if (Input.GetKeyDown(fireKey))
            AddElement(ElementType.Fire);
        else if (Input.GetKeyDown(waterKey))
            AddElement(ElementType.Water);
        else if (Input.GetKeyDown(earthKey))
            AddElement(ElementType.Earth);
    }

    private void HandleCastInput()
    {
        if (!canCast) return;

        // Кастуем заклинание
        if (Input.GetKeyDown(castKey))
        {
            if (currentSpell != null && !isCasting)
            {
                StartCasting();
            }
            else if (currentElements.Count > 0 && !isCasting)
            {
                // Если есть элементы но нет подходящего заклинания
                CastFailedSpell();
            }
        }

        // Прерываем кастинг или очищаем элементы
        if (Input.GetKeyDown(cancelKey))
        {
            if (isCasting)
            {
                CancelCasting();
            }
            else
            {
                ClearElements();
            }
        }

        // Для заклинаний типа огнемета - останавливаем при отпускании
        if (Input.GetKeyUp(castKey) && isCasting && currentSpell != null)
        {
            if (currentSpell is FlamethrowerSpell)
            {
                StopCasting();
            }
        }
    }

    private void UpdateCurrentSpell()
    {
        BaseSpell newSpell = FindSpellForCurrentElements();
        
        if (newSpell != currentSpell)
        {
            currentSpell = newSpell;
            OnSpellFound?.Invoke(currentSpell);
            
            if (currentSpell != null)
            {
                Debug.Log($"Spell ready: {currentSpell.SpellName}");
            }
        }
    }

    public void AddElement(ElementType element)
    {
        if (currentElements.Count >= MaxElements)
        {
            // Сдвигаем элементы влево и добавляем новый
            currentElements.RemoveAt(0);
        }

        currentElements.Add(element);
        OnElementsChanged?.Invoke(currentElements);

        Debug.Log($"Added element: {element}. Current combo: {ElementsToString(currentElements)}");
    }

    public void ClearElements()
    {
        currentElements.Clear();
        currentSpell = null;
        OnElementsChanged?.Invoke(currentElements);
        OnSpellFound?.Invoke(null);
        Debug.Log("Elements cleared");
    }

    private BaseSpell FindSpellForCurrentElements()
    {
        if (currentElements.Count == 0) return null;

        string comboString = ElementsToString(currentElements);
        
        if (spellLookup.ContainsKey(comboString))
        {
            return spellLookup[comboString];
        }

        return null;
    }

    private string ElementsToString(List<ElementType> elements)
    {
        if (elements == null || elements.Count == 0) return "";

        string combo = elements[0].ToString();
        for (int i = 1; i < elements.Count; i++)
        {
            combo += "," + elements[i].ToString();
        }
        return combo;
    }

    private void StartCasting()
    {
        if (currentSpell == null || isCasting) return;

        if (!currentSpell.CanCast(transform))
        {
            Debug.Log($"Can't cast {currentSpell.SpellName} - on cooldown");
            return;
        }

        castingCoroutine = StartCoroutine(CastSpellCoroutine());
    }

    private IEnumerator CastSpellCoroutine()
    {
        isCasting = true;
        OnCastingStateChanged?.Invoke(true);

        // Блокируем движение персонажа во время каста
        if (thirdPersonController != null)
        {
            // thirdPersonController.SetCanMove(false);
        }

        Debug.Log($"Casting {currentSpell.SpellName}...");

        // Ждем время каста (если оно больше 0)
        float castTime = Mathf.Max(currentSpell.CastTime, 0.1f);
        yield return new WaitForSeconds(castTime);

        // Выполняем заклинание
        if (currentSpell != null && isCasting) // Проверяем что кастинг не был прерван
        {
            currentSpell.Cast(transform, spellSpawnPoint);
            OnSpellCasted?.Invoke(currentSpell.SpellName);
            Debug.Log($"Successfully cast: {currentSpell.SpellName}");

            // Для мгновенных заклинаний сразу останавливаем кастинг
            if (!(currentSpell is FlamethrowerSpell))
            {
                StopCasting();
            }
        }
    }

    private void StopCasting()
    {
        if (!isCasting) return;

        isCasting = false;
        
        // Останавливаем текущее заклинание
        if (currentSpell != null)
        {
            currentSpell.StopCasting();
        }

        // Останавливаем корутину
        if (castingCoroutine != null)
        {
            StopCoroutine(castingCoroutine);
            castingCoroutine = null;
        }

        // Очищаем элементы после каста
        ClearElements();

        // Разблокируем движение
        if (thirdPersonController != null)
        {
            // thirdPersonController.SetCanMove(true);
        }

        OnCastingStateChanged?.Invoke(false);
    }

    private void CancelCasting()
    {
        if (!isCasting) return;

        Debug.Log("Casting cancelled");
        StopCasting();
    }

    private void CastFailedSpell()
    {
        Debug.Log($"Unknown spell combination: {ElementsToString(currentElements)}");
        
        // Можно добавить эффект неудачного заклинания
        // CreateFailEffect();
        
        ClearElements();
    }

    #region Public Methods для внешнего управления

    public void SetCanCast(bool canCast)
    {
        this.canCast = canCast;
        
        if (!canCast && isCasting)
        {
            CancelCasting();
        }
    }

    public bool IsCasting()
    {
        return isCasting;
    }

    public List<ElementType> GetCurrentElements()
    {
        return new List<ElementType>(currentElements);
    }

    public BaseSpell GetCurrentSpell()
    {
        return currentSpell;
    }

    public void AddElementByIndex(int elementIndex)
    {
        if (elementIndex >= 1 && elementIndex <= 3) // 1-Fire, 2-Water, 3-Earth
        {
            AddElement((ElementType)elementIndex);
        }
    }

    public void AddSpell(BaseSpell spell)
    {
        if (spell != null && !availableSpells.Contains(spell))
        {
            availableSpells.Add(spell);
            BuildSpellLookup(); // Перестраиваем lookup
        }
    }

    public void RemoveSpell(BaseSpell spell)
    {
        if (availableSpells.Contains(spell))
        {
            availableSpells.Remove(spell);
            BuildSpellLookup(); // Перестраиваем lookup
        }
    }

    #endregion
}