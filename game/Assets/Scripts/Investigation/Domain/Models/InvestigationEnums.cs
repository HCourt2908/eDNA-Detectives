namespace EDNA.Investigation.Domain
{
    public enum InvestigationPhase
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

    public enum InvestigationDifficulty
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

    public enum EvidenceConfidence
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

    public enum PredictionTargetKind
    {
        Species = 0,
        // Value 1 is retired; keep the remaining serialized target IDs stable.
        Seafloor = 2,
        PhysicalConfirmation = 3
    }

    public enum ComparisonProgressRole
    {
        ContextOnly = 0,
        AlternativeCauseCheck = 1,
        FoodWebCascade = 2,
        SharedPrediction = 3,
        BenthicDiscriminator = 4
    }

    public enum EvidenceCategory
    {
        General = 0,
        FoodWeb = 1,
        Benthic = 2,
        Confirmation = 3,
        Environmental = 4,
        Alternative = 5
    }

    public enum InvestigationConclusionStatus
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
        Mussel = 4,
        NameOnly = 5
    }

    public enum InvestigationTrophicRole
    {
        Unknown = 0,
        PrimaryProducer = 1,
        PrimaryConsumer = 2,
        SecondaryConsumer = 3,
        Predator = 4,
        ApexPredator = 5,
        Scavenger = 6,
        Decomposer = 7,
        HabitatForming = 8
    }

    public enum ThreatGlyphKind
    {
        // Value 0 is retired; keep the remaining serialized glyph IDs stable.
        Plastic = 1,
        LongLine = 2,
        BottomTrawling = 3
    }
}
