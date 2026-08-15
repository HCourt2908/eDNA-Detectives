namespace EDNA.Investigation.Domain
{
    public enum EvidenceType
    {
        NewDetection = 0,
        NotDetectedInSample = 1,
        RepeatedDetection = 2,
        RepeatedNonDetection = 3,
        DepthShift = 4,
        LowQualityResult = 5,
        ContaminationWarning = 6,
        StableIndicator = 7
    }

    public enum AnomalyClaimType
    {
        NewArrival = 0,
        ExpectedButMissing = 1,
        DifferentDepth = 2,
        ResultWarning = 3,
        MatchesBaseline = 4
    }

    public enum EvidenceConfidence
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum HypothesisStatus
    {
        Unexplored = 0,
        Plausible = 1,
        Supported = 2,
        Contradicted = 3
    }

    public enum EvidenceAssignmentKind
    {
        Supports = 0,
        Opposes = 1
    }

    public enum ConclusionStatus
    {
        NotSubmitted = 0,
        InsufficientEvidence = 1,
        Incorrect = 2,
        Correct = 3
    }
}
