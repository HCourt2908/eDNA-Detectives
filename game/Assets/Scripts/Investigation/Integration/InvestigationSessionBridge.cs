using System;
using EDNA.Core;
using UnityEngine;

namespace EDNA.Investigation
{
    public static class InvestigationSessionBridge
    {
        public static InvestigationGameInput PendingInput { get; private set; }
        public static InvestigationGameResult LastResult { get; private set; }
        public static bool IsComplete => LastResult != null && LastResult.completed;

        // Subscribe before completion, or read IsComplete/LastResult when opening
        // an end screen afterwards. ClearResult starts the next attempt.
        public static event Action<InvestigationGameResult> GameCompleted;

        public static void SetInput(InvestigationGameInput input)
        {
            PendingInput = input;
        }

        public static void PublishResult(InvestigationGameResult result)
        {
            bool wasComplete = IsComplete;
            LastResult = result;
            if (!wasComplete && IsComplete) GameCompleted?.Invoke(result);
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            GameCompleted = null;
            Clear();
        }
    }
}
