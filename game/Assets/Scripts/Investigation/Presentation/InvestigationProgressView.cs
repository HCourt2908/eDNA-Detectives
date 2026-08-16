using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationProgressView : MonoBehaviour
    {
        [SerializeField] private Text roundText;
        [SerializeField] private Text samplesText;
        [SerializeField] private Text findingsText;
        [SerializeField] private Text misstepsText;

        public string CurrentSummary { get; private set; } = string.Empty;

        public void ConfigureReferences(Text round, Text samples, Text findings, Text missteps)
        {
            roundText = round;
            samplesText = samples;
            findingsText = findings;
            misstepsText = missteps;
        }

        public void SetMetrics(int round, int samples, int findings, int totalFindings, int missteps)
        {
            SetMetric(roundText, "ROUND", round.ToString());
            SetMetric(samplesText, "SAMPLES", samples.ToString());
            SetMetric(findingsText, "FOUND", $"{findings}/{totalFindings}");
            SetMetric(misstepsText, "MISSTEPS", missteps.ToString());
            CurrentSummary = $"Round {round}. {samples} samples available. {findings} of {totalFindings} findings identified. {missteps} missteps.";
        }

        private static void SetMetric(Text target, string label, string value)
        {
            if (target == null) return;
            target.text = $"<size=10><color=#8EAEB5>{label}</color></size>\n<size=15><b><color=#F5E6BE>{value}</color></b></size>";
        }
    }
}
