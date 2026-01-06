// Assets/Cossacks2Bridge/UnityAdapters/IUiActionSink.cs

namespace Cossacks2Bridge.UnityAdapters
{
    /// <summary>
    /// Интерфейс для обработки UI действий из меню
    /// </summary>
    public interface IUiActionSink
    {
        /// <summary>
        /// Вызывается при клике на кнопку с действием
        /// </summary>
        /// <param name="buttonKey">Ключ кнопки (например "#Options_Window")</param>
        /// <param name="action">Действие с именем и payload</param>
        void OnAction(string buttonKey, Cossacks2Bridge.Core.UiAction action);
    }
}