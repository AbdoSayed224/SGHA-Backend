using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using SGHA.DTO;
using SGHA.Hubs;

namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AI_IssuesController : ControllerBase
    {
        private readonly string _connectionString;

        private readonly IHubContext<AiHub> _hubContext;
        private readonly ILogger<AI_IssuesController> _logger;

        public AI_IssuesController(IConfiguration configuration, IHubContext<AiHub> hubContext, ILogger<AI_IssuesController> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("Default");
            _hubContext = hubContext;
        }

        private async Task NotifyClients(List<AIIssueDto> data)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveIssues", data);
        }

        private async Task<List<AIIssueDto>> GetIssuesAsync(int houseId)
        {
            var result = new List<AIIssueDto>();
            string query = "SELECT * FROM Sys_AI_Issues WHERE HouseID = @HouseID ORDER BY CreatedAt DESC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@HouseID", houseId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new AIIssueDto
                {
                    IsAnomaly = reader.GetBoolean(reader.GetOrdinal("IsAnomaly")),
                    Action = reader["Action"].ToString(),
                    Parameter = reader["Parameter"].ToString(),
                    Range = reader["Range"].ToString(),
                    Value = Convert.ToSingle(reader["Value"]),
                    Message = reader["Message"].ToString(),
                });
            }


            return result;
        }

        // GET: api/AI_Issues/{houseId}
        [HttpGet("{houseId}")]
        public async Task<IActionResult> GetIssuesByHouseId(int houseId)
        {
            try
            {
                var issues = await GetIssuesAsync(houseId);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("{houseId}")]
        public async Task<IActionResult> PostIssue(int houseId, [FromBody] AIIssueDto dto)
        {
            string query = @"
            INSERT INTO Sys_AI_Issues (HouseID, IsAnomaly, Action, Parameter, Range, Value, Message, CreatedAt)
            VALUES (@HouseID, @IsAnomaly, @Action, @Parameter, @Range, @Value, @Message, @CreatedAt)";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@HouseID", houseId);
                command.Parameters.AddWithValue("@IsAnomaly", dto.IsAnomaly);
                command.Parameters.AddWithValue("@Action", dto.Action ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Parameter", dto.Parameter ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Range", dto.Range ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Value", dto.Value);
                command.Parameters.AddWithValue("@Message", dto.Message ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                await connection.OpenAsync();
                int rows = await command.ExecuteNonQueryAsync();

                if (rows == 0)
                {
                    return BadRequest("Failed to insert.");
                }
                else
                {                       
                    var allIssues = await GetIssuesAsync(1);
                    if (allIssues != null)
                    {
                        await NotifyClients(allIssues); 
                    }
                    return Ok("Issue recorded and notification sent.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
