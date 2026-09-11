using System;
using System.Collections.Generic;

namespace EDNA.Core
{
    [Serializable]
    public sealed class InvestigationGameResult
    {
        public string caseId = string.Empty;
        public string surveyId = string.Empty;
        public string siteId = string.Empty;
        public string selectedHypothesisId = string.Empty;
        // Explanations explicitly reviewed by the player; fact hovers do not count.
        public List<string> reviewedHypothesisIds = new List<string>();
        public List<string> compatibleHypothesisIds = new List<string>();
        // Case-authored priorities; these are not computed probabilities.
        public string primaryHypothesisId = string.Empty;
        public List<string> alternativeHypothesisIds = new List<string>();
        public bool correct;
        public List<string> evidenceIds = new List<string>();
        public List<string> surveySpeciesIds = new List<string>();
        public int missteps;
        public int finalSubmissionAttempts;
        public bool completed;
    }
}
