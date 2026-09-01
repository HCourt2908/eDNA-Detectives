using EDNA.Core;

namespace EDNA.Investigation
{
    public static class InvestigationSessionBridge
    {
        public static InvestigationGameInput PendingInput { get; private set; }
        public static InvestigationGameResult LastResult { get; private set; }

        public static void SetInput(InvestigationGameInput input)
        {
            PendingInput = input;
        }

        public static void PublishResult(InvestigationGameResult result)
        {
            LastResult = result;
        }

        public static void ClearResult()
        {
            LastResult = null;
        }

        public static void Clear()
        {
            PendingInput = null;
            ClearResult();
        }
    }
}
