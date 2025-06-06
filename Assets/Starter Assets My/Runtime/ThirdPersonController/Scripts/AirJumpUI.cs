using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace StarterAssets
{
    public class AirJumpUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("������������ ������ ��� ������ �������")]
        public Transform jumpIconsParent;

        [Tooltip("������ ������ ���������� ������")]
        public GameObject availableJumpIcon;

        [Tooltip("������ ������ ��������������� ������")]
        public GameObject usedJumpIcon;

        [Header("Animation")]
        [Tooltip("����������� ���������/������������ ������")]
        public bool animateIcons = true;

        [Tooltip("�������� ��������")]
        public float animationSpeed = 5f;

        [Header("Colors")]
        [Tooltip("���� ���������� ������")]
        public Color availableColor = Color.white;

        [Tooltip("���� ��������������� ������")]
        public Color usedColor = Color.gray;

        [Tooltip("���� ��� ��������������")]
        public Color restoreColor = Color.green;

        // ��������� ����������
        private AirJumpSystem airJumpSystem;
        private List<Image> jumpIcons = new List<Image>();
        private int lastMaxJumps = 0;

        void Start()
        {
            // ������� ��������� AirJumpSystem
            airJumpSystem = FindObjectOfType<AirJumpSystem>();

            if (airJumpSystem == null)
            {
                Debug.LogWarning("AirJumpUI: �� ������ ��������� AirJumpSystem!");
                return;
            }

            // ������������� �� �������
            airJumpSystem.OnAirJumpPerformed += OnAirJumpUsed;
            airJumpSystem.OnAirJumpsReset += OnAirJumpsRestored;

            // ������� ��������� ������
            UpdateUI();
        }

        void Update()
        {
            if (airJumpSystem == null) return;

            // ���������, ���������� �� ������������ ���������� �������
            int currentMaxJumps = airJumpSystem.maxAirJumps;
            if (currentMaxJumps != lastMaxJumps)
            {
                lastMaxJumps = currentMaxJumps;
                RecreateIcons();
            }
        }

        void OnDestroy()
        {
            // ������������ �� �������
            if (airJumpSystem != null)
            {
                airJumpSystem.OnAirJumpPerformed -= OnAirJumpUsed;
                airJumpSystem.OnAirJumpsReset -= OnAirJumpsRestored;
            }
        }

        void RecreateIcons()
        {
            // ������� ������ ������
            foreach (var icon in jumpIcons)
            {
                if (icon != null)
                    DestroyImmediate(icon.gameObject);
            }
            jumpIcons.Clear();

            // ������� ����� ������
            for (int i = 0; i < airJumpSystem.maxAirJumps; i++)
            {
                CreateJumpIcon();
            }

            UpdateUI();
        }

        void CreateJumpIcon()
        {
            GameObject iconPrefab = availableJumpIcon != null ? availableJumpIcon : usedJumpIcon;
            if (iconPrefab == null) return;

            GameObject iconObj = Instantiate(iconPrefab, jumpIconsParent);
            Image iconImage = iconObj.GetComponent<Image>();

            if (iconImage != null)
            {
                jumpIcons.Add(iconImage);
            }
        }

        void UpdateUI()
        {
            if (airJumpSystem == null) return;

            int usedJumps = airJumpSystem.GetUsedAirJumps();

            for (int i = 0; i < jumpIcons.Count; i++)
            {
                if (jumpIcons[i] == null) continue;

                bool isUsed = i < usedJumps;
                Color targetColor = isUsed ? usedColor : availableColor;

                if (animateIcons)
                {
                    jumpIcons[i].color = Color.Lerp(jumpIcons[i].color, targetColor, Time.deltaTime * animationSpeed);
                }
                else
                {
                    jumpIcons[i].color = targetColor;
                }

                // �������������� �������� ��������
                if (animateIcons)
                {
                    float targetScale = isUsed ? 0.8f : 1.0f;
                    Vector3 currentScale = jumpIcons[i].transform.localScale;
                    Vector3 targetScaleVector = Vector3.one * targetScale;
                    jumpIcons[i].transform.localScale = Vector3.Lerp(currentScale, targetScaleVector, Time.deltaTime * animationSpeed);
                }
            }
        }

        void OnAirJumpUsed(int jumpNumber)
        {
            UpdateUI();

            // �������������� �������� ��� ������������� ������
            if (animateIcons && jumpNumber <= jumpIcons.Count)
            {
                StartCoroutine(AnimateIconUsage(jumpNumber - 1));
            }
        }

        void OnAirJumpsRestored()
        {
            // �������� ��������������
            if (animateIcons)
            {
                StartCoroutine(AnimateRestore());
            }

            UpdateUI();
        }

        System.Collections.IEnumerator AnimateIconUsage(int iconIndex)
        {
            if (iconIndex < 0 || iconIndex >= jumpIcons.Count || jumpIcons[iconIndex] == null)
                yield break;

            Image icon = jumpIcons[iconIndex];
            Vector3 originalScale = icon.transform.localScale;

            // ����������� ������
            float time = 0;
            while (time < 0.2f)
            {
                time += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 1.3f, time / 0.2f);
                icon.transform.localScale = originalScale * scale;
                yield return null;
            }

            // ���������� � ����������� �������
            time = 0;
            while (time < 0.2f)
            {
                time += Time.deltaTime;
                float scale = Mathf.Lerp(1.3f, 0.8f, time / 0.2f);
                icon.transform.localScale = originalScale * scale;
                yield return null;
            }
        }

        System.Collections.IEnumerator AnimateRestore()
        {
            // �������� ���������� ��� ������ � ���� ��������������
            Color[] originalColors = new Color[jumpIcons.Count];
            for (int i = 0; i < jumpIcons.Count; i++)
            {
                if (jumpIcons[i] != null)
                {
                    originalColors[i] = jumpIcons[i].color;
                    jumpIcons[i].color = restoreColor;
                }
            }

            yield return new WaitForSeconds(0.3f);

            // ���������� �������� �����
            for (int i = 0; i < jumpIcons.Count; i++)
            {
                if (jumpIcons[i] != null)
                {
                    jumpIcons[i].color = originalColors[i];
                }
            }
        }

        // ��������� ������ ��� ������� ����������

        /// <summary>
        /// ������������� ��������� UI
        /// </summary>
        public void ForceUpdateUI()
        {
            UpdateUI();
        }

        /// <summary>
        /// ����������/�������� UI
        /// </summary>
        public void SetUIVisible(bool visible)
        {
            if (jumpIconsParent != null)
            {
                jumpIconsParent.gameObject.SetActive(visible);
            }
        }
    }
}