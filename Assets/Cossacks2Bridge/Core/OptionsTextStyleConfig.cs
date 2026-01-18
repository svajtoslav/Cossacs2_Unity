using UnityEngine;

namespace Cossacks2Bridge.Core
{
    /// <summary>
    /// Конфигурация стилей текста для Options экрана.
    /// Шрифты загружаются из Resources/Fonts/
    /// </summary>
    public static class OptionsTextStyleConfig
    {
        // ═══════════════════════════════════════════════════════════
        // SECTION TITLE: "Настройки Видео", "Настройки Аудио", "Игровые Настройки"
        // Georgia, размер 14, цвет #881203 (R136 G18 B3)
        // ═══════════════════════════════════════════════════════════
        public static class SectionTitle
        {
            public const string FontPath = "Fonts/Georgia";
            public const float FontSize = 14f;
            public static readonly Color32 Color = new Color32(136, 18, 3, 255); // #881203
            public const float CharacterSpacing = 0f;
            public const bool Bold = false;
        }

        // ═══════════════════════════════════════════════════════════
        // OPTION LABEL: "Громкость звука", "Разрешение экрана" и т.д.
        // PlayfairDisplay, размер 12, чёрный
        // ═══════════════════════════════════════════════════════════
        public static class OptionLabel
        {
            public const string FontPath = "Fonts/PlayfairDisplay-VariableFont_wght";
            public const float FontSize = 12f;
            public static readonly Color32 Color = new Color32(0, 0, 0, 255); // чёрный
            public const float CharacterSpacing = 0f;
            public static float BoldMul = 1.0f; // <-- ДОБАВЬ ЭТУ СТРОКУ
            public const bool Bold = false;
        }

        // ═══════════════════════════════════════════════════════════
        // MAIN MENU TITLE: "Главное меню" — белый, НЕ жирный
        // ═══════════════════════════════════════════════════════════
        public static class MainMenuTitle
        {
            public const string FontPath = "Fonts/Seminaria";
            public const float FontSize = 20f;
            public static readonly Color32 Color = new Color32(255, 255, 255, 255);
            public const float CharacterSpacing = 30f;
            public const bool Bold = false;  // ✅ НЕ жирный
        }

        // ═══════════════════════════════════════════════════════════
        // WINDOW TITLE: "Настройки" (ближе к центру)
        // Seminaria, размер 20, белый, межбуквенное +30%
        // ═══════════════════════════════════════════════════════════
        public static class WindowTitle
        {
            public const string FontPath = "Fonts/Seminaria";
            public const float FontSize = 20f;
            public static readonly Color32 Color = new Color32(255, 255, 255, 255);
            public const float CharacterSpacing = 30f;
            public const bool Bold = false;  // ✅ ЖИРНЫЙ для Options
        }

        // ═══════════════════════════════════════════════════════════
        // GOLDEN TITLE: Золотой заголовок справа вверху
        // ═══════════════════════════════════════════════════════════
        public static class GoldenTitle
        {
            public const string FontPath = "Fonts/Seminaria";
            public const float FontSize = 20f;
            public static readonly Color32 Color = new Color32(218, 165, 32, 255); // золотой
            public const float CharacterSpacing = 0f;
            public const bool Bold = false;
        }

        // ═══════════════════════════════════════════════════════════
        // BUTTON: Accept/Cancel кнопки
        // ═══════════════════════════════════════════════════════════
        public static class Button
        {
            public const string FontPath = "Fonts/Slovic";
            public const float FontSize = 16f;
            public static readonly Color32 NormalColor = new Color32(255, 255, 255, 255);
            public static readonly Color32 HoverColor = new Color32(255, 220, 100, 255);
            public static readonly Color32 DisabledColor = new Color32(128, 128, 128, 255);
            public const float CharacterSpacing = 0f;
            public const bool Bold = false;
        }
    }
}