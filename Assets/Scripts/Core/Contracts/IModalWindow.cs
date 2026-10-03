namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract for modal windows that participate in the LIFO ModalStackManager.
    /// </summary>
    public interface IModalWindow
    {
        bool IsOpen { get; }
        void OpenModal();
        void CloseModal();
    }

    /// <summary>
    /// Service managing active modal windows in strict LIFO order.
    /// </summary>
    public interface IModalStackService
    {
        int OpenModalCount { get; }
        bool HasActiveModal { get; }
        void Push(IModalWindow modal);
        void Pop();
        void PopSpecific(IModalWindow modal);
        void CloseAll();
    }
}
