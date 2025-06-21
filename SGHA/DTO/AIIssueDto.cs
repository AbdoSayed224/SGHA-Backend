namespace SGHA.DTO
{
    public class AIIssueDto
    {

        public bool IsAnomaly { get; set; }         // true = problem detected, false = normal
        public string Action { get; set; }          // Action to solve the problem
        public string Parameter { get; set; }       // Sensor type
        public string Range { get; set; }           // Expected range
        public float Value { get; set; }            // Real value from the sensor
        public string Message { get; set; }         // Custom message explaining the issue
    }
}
