using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGHA.DTO;
using System.Data;
using System.Threading.Tasks;
namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AI_IssuesController : ControllerBase
    {
        private readonly string _connectionString;

        public AI_IssuesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
        // GET: api/AI_Issues/{houseId}
        [HttpGet("{houseId}")]
        public async Task<IActionResult> GetIssuesByHouseId(int houseId)
        {
            string query = "SELECT * FROM Sys_AI_Issues WHERE HouseID = @HouseID ORDER BY CreatedAt DESC";
            var result = new List<AIIssueDto>();

            try
            {
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

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        // POST: api/AI_Issues/{houseId}
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

                return rows > 0 ? Ok("Issue recorded.") : BadRequest("Failed to insert.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


    }
}
