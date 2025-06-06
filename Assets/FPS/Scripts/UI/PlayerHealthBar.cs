using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class PlayerHealthBar : MonoBehaviour
    {
        [Tooltip("Image component displaying current health")]
        public Image HealthFillImage;

        [Tooltip("Перетащи сюда игрока с Health компонентом")]
        public Health playerHealth; // Поле для ручной настройки

        Health m_PlayerHealth;

        void Start()
        {
            // Используем заданный в Inspector Health
            if (playerHealth != null)
            {
                m_PlayerHealth = playerHealth;
            }
            else
            {
                Debug.LogError("Перетащи игрока в поле Player Health!");
            }
        }

        void Update()
        {
            if (m_PlayerHealth != null)
            {
                // Правильные названия свойств с большой буквы:
                HealthFillImage.fillAmount = m_PlayerHealth.CurrentHealth / m_PlayerHealth.MaxHealth;
            }
        }
    }
}