namespace EDNA.Investigation.V2.Domain
{
    public enum InvestigationV2Phase
    {
        Observe = 0,
        Simulate = 1,
        Report = 2
    }

    public enum SurveyEra
    {
        Historical = 0,
        Current = 1
    }

    public enum InvestigationV2Difficulty
    {
        Easy = 0,
        Hard = 1
    }

    public enum ObservationSource
    {
        EDNA = 0,
        CTDLog = 1,
        ROV = 2,
        Methodology = 3
    }

    public enum EvidenceUnlockStage
    {
        Observe = 0,
        OnThreatRun = 1,
        AfterProvisional = 2,
        Always = 3
    }

    public enum V2EvidenceConfidence
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum ObservationClaimType
    {
        NewDetection = 0,
        NotDetected = 1,
        ChangedDepthOrDistribution = 2,
        ResultWarning = 3,
        MatchesBaseline = 4,
        EnvironmentalReading = 5,
        PhysicalObservation = 6,
        MethodologicalLimitation = 7
    }

    public enum PredictionState
    {
        Increase = 0,
        Decrease = 1,
        Stable = 2,
        DepthShift = 3,
        Unknown = 4
    }

    public enum ComparisonJudgement
    {
        Match = 0,
        Mismatch = 1,
        NotEnoughEvidence = 2
    }

    public enum ComparisonEvaluationOutcome
    {
        Accepted = 0,
        AcceptedWithCaveat = 1,
        Incorrect = 2
    }

    public enum InvestigationV2ConclusionStatus
    {
        NotSubmitted = 0,
        InsufficientEvidence = 1,
        Incorrect = 2,
        Correct = 3
    }

    public enum SpeciesGlyphKind
    {
        Shark = 0,
        Tuna = 1,
        Krill = 2,
        SeaStar = 3,
        Mussel = 4
    }

    public enum ThreatGlyphKind
    {
        Warming = 0,
        Plastic = 1,
        LongLine = 2,
        BottomTrawling = 3
    }
}
