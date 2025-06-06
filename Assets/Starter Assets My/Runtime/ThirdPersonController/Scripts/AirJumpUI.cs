using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace StarterAssets
{
    public class AirJumpUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Родительский объект для иконок прыжков")]
        public Transform jumpIconsParent;

        [Tooltip("Префаб иконки доступного прыжка")]
        public GameObject availableJumpIcon;

        [Tooltip("Префаб иконки использованного прыжка")]
        public GameObject usedJumpIcon;

        [Header("Animation")]
        [Tooltip("Анимировать появление/исчезновение иконок")]
        public bool animateIcons = true;

        [Tooltip("Скорость анимации")]
        public float animationSpeed = 5f;

        [Header("Colors")]
        [Tooltip("Цвет доступного прыжка")]
        public Color availableColor = Color.white;

        [Tooltip("Цвет использованного прыжка")]
        public Color usedColor = Color.gray;

        [Tooltip("Цвет при восстановлении")]
        public Color restoreColor = Color.green;

        // Приватные переменные
        private AirJumpSystem airJumpSystem;
        private List<Image> jumpIcons = new List<Image>();
        private int lastMaxJumps = 0;

        void Start()
        {
            // Находим компонент AirJumpSystem
            airJumpSystem = FindObjectOfType<AirJumpSystem>();

            if (airJumpSystem == null)
            {
                Debug.LogWarning("AirJumpUI: Не найден компонент AirJumpSystem!");
                return;
            }

            // Подписываемся на события
            airJumpSystem.OnAirJumpPerformed += OnAirJumpUsed;
            airJumpSystem.OnAirJumpsReset += OnAirJumpsRestored;

            // Создаем начальные иконки
            UpdateUI();
        }

        void Update()
        {
            if (airJumpSystem == null) return;

            // Проверяем, изменилось ли максимальное количество прыжков
            int currentMaxJumps = airJumpSystem.maxAirJumps;
            if (currentMaxJumps != lastMaxJumps)
            {
                lastMaxJumps = currentMaxJumps;
                RecreateIcons();
            }
        }

        void OnDestroy()
        {
            // Отписываемся от событий
            if (airJumpSystem != null)
            {
                airJumpSystem.OnAirJumpPerformed -= OnAirJumpUsed;
                airJumpSystem.OnAirJumpsReset -= OnAirJumpsRestored;
            }
        }

        void RecreateIcons()
        {
            // Удаляем старые иконки
            foreach (var icon in jumpIcons)
            {
                if (icon != null)
                    DestroyImmediate(icon.gameObject);
            }
            jumpIcons.Clear();

            // Создаем новые иконки
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

                // Дополнительная анимация масштаба
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

            // Дополнительная анимация при использовании прыжка
            if (animateIcons && jumpNumber <= jumpIcons.Count)
            {
                StartCoroutine(AnimateIconUsage(jumpNumber - 1));
            }
        }

        void OnAirJumpsRestored()
        {
            // Анимация восстановления
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

            // Увеличиваем иконку
            float time = 0;
            while (time < 0.2f)
            {
                time += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 1.3f, time / 0.2f);
                icon.transform.localScale = originalScale * scale;
                yield return null;
            }

            // Возвращаем к нормальному размеру
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
            // Временно окрашиваем все иконки в цвет восстановления
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

            // Возвращаем исходные цвета
            for (int i = 0; i < jumpIcons.Count; i++)
            {
                if (jumpIcons[i] != null)
                {
                    jumpIcons[i].color = originalColors[i];
                }
            }
        }

        // Публичные методы для ручного управления

        /// <summary>
        /// Принудительно обновляет UI
        /// </summary>
        public void ForceUpdateUI()
        {
            UpdateUI();
        }

        /// <summary>
        /// Показывает/скрывает UI
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