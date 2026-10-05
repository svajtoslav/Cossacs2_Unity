using System.Collections.Generic;

namespace Cossacks2Bridge.Core
{
    public sealed class UiDesk
    {
        public string SourcePath = "";
        public string XmlSource = "";

        // V392 source-bundle provenance.  Dependent runtime data (AI/ai.dat,
        // hero metadata, etc.) must come from this same data root.
        public string SourceBundleId = "";
        public string SourceDataRoot = "";

        public string ScreenId = "";
        public int ParsedNodeCount;
        public int GenericNodeCount;
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

        // V396A7R2: preserve the control's OWN XML state separately from the
        // inherited/effective state. Hidden modal parents (for example the
        // original profile-delete desk) are changed by SetFrameState at runtime;
        // descendants must then recover their own <Visible>true</Visible> values.
        public bool LocalVisible = true;
        public bool LocalEnabled = true;

        // Common source fields used by original Dialogs rendering. Color is the
        // game's AARRGGBB value (e.g. 73FFFFFF on the delete-modal blackout).
        public uint ColorArgb = 0xFFFFFFFFu;
        public bool DeepColor;
        public string HotKey = "NONE";

        // V396A7R4: original ParentFrame transform block.  These values are
        // source data, not Unity layout guesses.  In the game ParentFrame::GetMatrix
        // applies this matrix before a dialog and its children are drawn.
        public bool EnableTransform;
        public string PivotPosition = "Left";
        public float PivotDx;
        public float PivotDy;
        public float TransformScaleX = 1f;
        public float TransformScaleY = 1f;
        public float TransformAngle;
        public bool FlipX;
        public bool FlipY;

        // V388 unified XML provenance. Renderers can stay flat while state/runtime
        // code can still address the exact original hierarchy deterministically.
        public string SourceTag = "";
        public int SourceId = -1;
        public int ParentSourceId = -1;
        public int Depth;
        public int LocalX, LocalY;

        public readonly List<UiAction> Actions = new();
    }

    public sealed class UiAction
    {
        public string Name = "";
        public string Payload = "";
    }

    /// <summary>
    /// Original XML control/container for which a dedicated Unity adapter does
    /// not exist yet. It is deliberately preserved by the parser instead of
    /// being silently dropped or converted into a different control type.
    /// </summary>
    public sealed class UiGenericNode : UiNode
    {
        public string Kind = "";
    }

    
    // ═══════════════════════════════════════════════════════════
    // DIALOGS DESK (frame/background area)
    // ═══════════════════════════════════════════════════════════
    public sealed class UiDialogsDesk : UiNode
    {
        public string Border = "";
        // V396A7R5: preserve the original DialogsDesk scrolling contract.
        // These are runtime properties in Cossacks II, not cosmetic metadata.
        public bool EnableHorizontalScroller = false;
        public bool EnableVerticalScroller = false;
        public bool HideVScroller = false;
        public bool EnableMouseShift = false;
        public int XShift = 0;
        public int YShift = 0;
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
        // Original TextButton::SetMessage/OnDraw use MaxWidth to recalculate
        // multiline height and x1. Width from Position&Width is not equivalent.
        public int MaxWidth = 10000;
        public bool Vertical = false;
        
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
    // UiVitButton - УБРАЛИ Width/Height (они уже есть в UiNode!)
    public sealed class UiVitButton : UiNode
    {
        public string MessageKey = "";
        public string HintKey = "";
        public string GP_File = "";

        // Cossacks II 1.4 VitButton state.  The original XML stores the
        // sprite pair as SpritePassiveN/SpriteOverN where N == State.
        // -1 means that the passive state has no background sprite.
        public int State;
        public int SpritePassive = -1;
        public int SpriteActive = -1;
        public int SpriteDx;
        public bool OneSprited;
        public bool DisableCycling;

        // Text layout belongs to VitButton itself in the 1.4 dialogs.
        public string FontPassive = "";
        public string FontOver = "";
        public int FontDx;
        public int FontDy;
        public string Align = "Center";
    }

    // UiInputBox - УБРАЛИ Width/Height
    public sealed class UiInputBox : UiNode
    {
        public int MaxLen;
        public string Action;
        public string Font;
        // Width и Height УДАЛЕНЫ - наследуются от UiNode
    }

    // Остальное без изменений...
    public sealed class UiListDesk : UiNode
    {
        public string Border = "";
        public int ElementWidth;
        public int ElementHeight;
        public int MarginX = 3;
        public int MarginY = 3;
        public string Action = "";
        public UiListDeskElement ElementTemplate;
        public List<string> Items = new();
    }

    public sealed class UiListDeskElement
    {
        public string GP_File = "";
        public int SpritePassive = -1;
        public int SpriteOver = 0;
        public int SpriteSelected = 5;
        public int Width = 460;
        public int Height = 20;
        public string FontPassive = "BlackFont";
        public string FontOver = "RedFont";
        public int FontDx = 10;
        public int FontDy = 0;
        public string Align = "Left";
    }
}

