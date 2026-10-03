using System;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining daily operations report modal UI interactions.
    /// </summary>
    public interface IDailyReportService
    {
        bool IsOpen { get; }
        void ShowReport(int completedDay, Action onContinue);
    }
}
