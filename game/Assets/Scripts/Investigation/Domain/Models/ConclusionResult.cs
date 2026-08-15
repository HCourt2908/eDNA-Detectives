namespace EDNA.Investigation.Domain
{
    public sealed class ConclusionResult
    {
        public ConclusionResult(ConclusionStatus status, string feedback)
        {
            Status = status;
            Feedback = feedback;
        }

        public ConclusionStatus Status { get; }
        public string Feedback { get; }
    }
}
