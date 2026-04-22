namespace ClientAssignmentOptimizer.Domain
{
    public class DealerInfo
    {
        public string FullName { get; set; }
        public bool IsRecruited { get; set; }
        public int AssignedCustomerCount { get; set; }
        public float Cash { get; set; }
        public float Cut { get; set; }
        public string DealerType { get; set; }
    }
}
