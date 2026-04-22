namespace ClientAssignmentOptimizer.Domain
{
    public class CustomerInfo
    {
        public string FullName { get; set; }
        public string NpcId { get; set; }
        public string AssignedDealerName { get; set; }
        public bool IsPlayerAssigned => AssignedDealerName == null;
        public float CurrentAddiction { get; set; }
        public float MinWeeklySpend { get; set; }
        public float MaxWeeklySpend { get; set; }
        public string Standards { get; set; }
    }
}
