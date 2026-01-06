using System.Collections.Generic;

namespace Cossacks2Bridge.Core
{
    public sealed class UiDesk
    {
        public string SourcePath = "";
        public readonly List<UiNode> Children = new();
    }

    // ═══════════════════════════════════════════════════════════
    // TEXT STYLE ENUM — НОВОЕ
    // ═══════════════════════════════════════════════════════════
    
    public enum UiTextStyle
    {
        Default,           // Стандартный стиль
        SectionTitle,      // "Настройки Видео", "Настройки Аудио" - Georgia, #881203, 14
        OptionLabel,       // "Громкость звука", "Разрешение" - PlayfairDisplay, черный, 12
        WindowTitle,       // "Настройки" (центр) - Seminaria, белый, 20, spacing +30%
        MainMenuTitle,
        GoldenTitle,       // Золотой заголовок справа вверху
        Button             // Кнопки Accept/Cancel
    }

    // ═══════════════════════════════════════════════════════════
    // BASE NODE
    // ═══════════════════════════════════════════════════════════
    public abstract class UiNode
    {
        public string Name = "";
        public string Hint = "";
        public int X, Y, Width, Height;
        public bool Visible = true;
        public bool Enabled = true;
        public readonly List<UiAction> Actions = new();
    }

    public sealed class UiAction
    {
        public string Name = "";
        public string Payload = "";
    }

    // ═══════════════════════════════════════════════════════════
    // PICTURES
    // ═══════════════════════════════════════════════════════════
    
    public sealed class UiBitPicture : UiNode
    {
        public string FileName = "";
    }

    public sealed class UiGPPicture : UiNode
    {
        public string FileID = "";
        public int SpriteID = 0;
    }

    // ═══════════════════════════════════════════════════════════
    // BUTTONS / TEXT
    // ═══════════════════════════════════════════════════════════
    
    public sealed class UiTextButton : UiNode
    {
        public string MessageKey = "";
        public string HintKey = "";
        public string PassiveFont = "";
        public string ActiveFont = "";
        public string DisabledFont = "";
        public string Align = "Left";
        
        // ✅ НОВОЕ: стиль текста
        public UiTextStyle Style = UiTextStyle.Default;
    }

    public sealed class UiGPTextButton : UiNode
    {
        public string MessageKey = "";
        public string FileID = "";
        public int Sprite = 0;     // обычно "Active"
        public int Sprite1 = 0;    // обычно "Passive"

        public string PassiveFont = "";
        public string ActiveFont = "";
        public string DisabledFont = "";

        public bool Center = false;
        public int FontDx = 0;
        public int FontDy = 0;

        // ✅ стиль текста для ApplyTextStyle()
        public UiTextStyle Style = UiTextStyle.Button;
    }

    // ═══════════════════════════════════════════════════════════
    // CONTROLS
    // ═══════════════════════════════════════════════════════════

    public sealed class UiCheckBox : UiNode
    {
        public string GP_File = "";
        public bool State = false;
        public int GroupIndex = 0;
    }

    public sealed class UiComboBox : UiNode
    {
        public string GP_File = "";
        public string ActiveFont = "";
        public string PassiveFont = "";

        // original params (Options)
        public int FontDx;
        public int FontDy;
        public int OneDx;
        public int OneDy;
        public int Center;
        public int MaxLY;
    }

    public sealed class UiSlider : UiNode
    {
        public int Position;
        public int MaxPosition;
        public int SliderPos;
        public int GroupIndex;

        // from original DialogsSystem.xml (Options)
        public int LineLx; // length of the line (bar)
        public int LineLy; // thickness/height
        public int ScrDx;  // knob offset X
        public int ScrDy;  // knob offset Y

        public string GP_File = "";
    }
}

namespace Cossacks2Bridge.Core
{
    public sealed class UiVitButton : UiNode
    {
        public string GP_File;
        public int SpritePassive;
        public int SpriteActive;
        public int Width;
        public int Height;
    }

    public sealed class UiInputBox : UiNode
    {
        public int MaxLen;
        public string Action;
        public string Font;
        public int Width;
        public int Height;
    }

    public sealed class UiListDesk : UiNode
    {
        public int ElementWidth;
        public int ElementHeight;
        public string Action;
    }
}
