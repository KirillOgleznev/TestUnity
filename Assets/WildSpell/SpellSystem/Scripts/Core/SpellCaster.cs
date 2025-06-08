using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Linq;

[System.Serializable]
public enum ElementType
{
    None = 0,
    Fire = 1,
    Water = 2,
    Earth = 3
}


[System.Serializable]
public class SpellElement
{
    public ElementType elementType;
    public Color elementColor;
    public GameObject elementVFX;

    public SpellElement(ElementType type)
    {
        elementType = type;
        elementColor = GetElementColor(type);
    }

    private Color GetElementColor(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire: return Color.red;
            case ElementType.Water: return Color.blue;
            case ElementType.Earth: return new Color(0.6f, 0.4f, 0.2f);
            default: return Color.gray;
        }
    }
}

public class SpellCaster : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private bool rotatePlayerDuringCast = true;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("References")]
    public Transform spellSpawnPoint;
    [SerializeField] private ProjectileShooter projectileShooter;
    [SerializeField] private Transform playerRoot; // ������ ���

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
    [HideInInspector]
    public UnityEvent<bool> OnCastingStateChanged;
    [HideInInspector]
    public UnityEvent<List<ElementType>> OnElementsChanged;
    [HideInInspector]
    public UnityEvent<string> OnSpellCasted;
    [HideInInspector]
    public UnityEvent<BaseSpell> OnSpellFound;

    public ThirdPersonController thirdPersonController;
    private PlayerInput playerInput;

    private List<ElementType> currentElements = new List<ElementType>();
    private const int MaxElements = 3;

    private bool isCasting = false;
    private bool canCast = true;
    private BaseSpell currentSpell = null;
    private Coroutine castingCoroutine = null;

    // ������� ��� �������� ������ ���������� �� ���������� ���������
    private Dictionary<string, BaseSpell> spellLookup = new Dictionary<string, BaseSpell>();

    void Start()
    {
        // �������� ������ �� ����������
        playerInput = GetComponent<PlayerInput>();

        if (projectileShooter == null)
            projectileShooter = GetComponent<ProjectileShooter>();

        // �������������� ������� ����������
        InitializeSpellSystem();
    }

    void Update()
    {
        HandleElementInput();
        HandleCastInput();
        UpdateCurrentSpell();

        if (isCasting && rotatePlayerDuringCast && projectileShooter != null)
        {
            RotatePlayerToTarget();
        }
    }

    private void RotatePlayerToTarget()
    {
        Vector3 targetDirection = projectileShooter.GetShootDirection();

        targetDirection.y = 0;
        targetDirection.Normalize();

        if (targetDirection != Vector3.zero && playerRoot != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            // ������������ �������� ������ ���������, � �� SpellCaster
            playerRoot.rotation = Quaternion.RotateTowards(playerRoot.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void InitializeSpellSystem()
    {
        // �������� ��� ���������� �� �������� �������� ���� ��� �� ���������
        if (availableSpells.Count == 0)
        {
            availableSpells.AddRange(GetComponentsInChildren<BaseSpell>());
        }

        // ������ ������� ��� ������ ����������
        BuildSpellLookup();

        // ������� ��������� ��������
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

                // ��������� ��� ��������� ������������ ��� ��������������� ����������
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

        // ��� ������� ���������� (1 �������) ��������� ��� ����
        if (elements.Count == 1)
        {
            result.Add(new List<ElementType>(elements));
            return result;
        }

        // ��� ������� ���������� ��������� ��� ���������� ������������
        var uniqueElements = elements.Distinct().ToList();
        if (uniqueElements.Count == 1)
        {
            // ��� �������� ���������� (��������, Fire,Fire,Fire)
            result.Add(new List<ElementType>(elements));
        }
        else
        {
            // ���������� ��� ������������
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

        // ��������� ������� ������ ���������
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

        // ������� ����������
        if (Input.GetKeyDown(castKey))
        {
            if (currentSpell != null && !isCasting)
            {
                StartCasting();
            }
            else if (currentElements.Count > 0 && !isCasting)
            {
                // ���� ���� �������� �� ��� ����������� ����������
                CastFailedSpell();
            }
        }

        // ��������� ������� ��� ������� ��������
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

        // ��� ���������� ���� �������� - ������������� ��� ����������
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
            // �������� �������� ����� � ��������� �����
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

        if (thirdPersonController != null)
        {
            thirdPersonController.LockPlayerRotation = true;
        }

        Debug.Log($"Casting {currentSpell.SpellName}...");

        float castTime = Mathf.Max(currentSpell.CastTime, 0.1f);
        yield return new WaitForSeconds(castTime);

        if (currentSpell != null && isCasting)
        {
            Vector3 targetDirection = projectileShooter != null ?
                projectileShooter.GetShootDirection() :
                transform.forward;
            Vector3 targetPoint = projectileShooter != null ?
                projectileShooter.GetTargetPoint() :
                transform.position + transform.forward * 10f;

            currentSpell.Cast(transform, spellSpawnPoint, targetDirection, targetPoint);
            OnSpellCasted?.Invoke(currentSpell.SpellName);
            Debug.Log($"Successfully cast: {currentSpell.SpellName}");

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

        if (thirdPersonController != null)
        {
            thirdPersonController.LockPlayerRotation = false;
        }

        if (currentSpell != null)
        {
            currentSpell.StopCasting();
        }

        if (castingCoroutine != null)
        {
            StopCoroutine(castingCoroutine);
            castingCoroutine = null;
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

        // ����� �������� ������ ���������� ����������
        // CreateFailEffect();

        ClearElements();
    }

    #region Public Methods ��� �������� ����������

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
            BuildSpellLookup(); // ������������� lookup
        }
    }

    public void RemoveSpell(BaseSpell spell)
    {
        if (availableSpells.Contains(spell))
        {
            availableSpells.Remove(spell);
            BuildSpellLookup(); // ������������� lookup
        }
    }

    #endregion
}