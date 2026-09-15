using System;
using System.Collections;
using System.Collections.Generic;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private static Color PredictionStateColor(PredictionState state)
        {
            if (state == PredictionState.Increase || state == PredictionState.DepthShift) return InvestigationTheme.Primary;
            if (state == PredictionState.Decrease || state == PredictionState.Stable) return InvestigationTheme.TextSecondary;
            return InvestigationTheme.Unknown;
        }

        private static string PredictionStateSymbol(PredictionState state)
        {
            switch (state)
            {
                case PredictionState.Increase: return "↑";
                case PredictionState.Decrease: return "↓";
                case PredictionState.DepthShift: return "↕";
                case PredictionState.Unknown: return "?";
                default: return "—";
            }
        }

    }
}
