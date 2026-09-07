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
        public bool correct;
        public List<string> evidenceIds = new List<string>();
        public List<string> surveySpeciesIds = new List<string>();
        public int missteps;
        public int finalSubmissionAttempts;
        public bool completed;
    }
}
