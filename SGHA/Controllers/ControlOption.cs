using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGHA.DTO;
using System.Data;
using System.Threading.Tasks;
namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ControlOptionController : ControllerBase
    {
        private readonly string _connectionString;

        public ControlOptionController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }


        [HttpGet("by-house/{houseId}")]
        public async Task<ActionResult<IEnumerable<ControlOptionDto>>> GetControlOptionsByHouseId(int houseId)
        {
            var results = new List<ControlOptionDto>();
            string query = "SELECT * FROM Sys_SensorControl WHERE HouseID = @HouseID";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@HouseID", houseId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new ControlOptionDto
                    {
                        ControlID = reader.GetInt32(reader.GetOrdinal("ControlID")),
                        HouseID = reader.GetInt32(reader.GetOrdinal("HouseID")),
                        FanStatus = reader.GetInt32(reader.GetOrdinal("FanStatus")),
                        LightStatus = reader.GetInt32(reader.GetOrdinal("LightStatus")),
                        WaterStatus = reader.GetInt32(reader.GetOrdinal("WaterStatus")),
                        Note = reader.IsDBNull(reader.GetOrdinal("Note")) ? null : reader.GetString(reader.GetOrdinal("Note")),
                        IsAutomated = reader.GetInt32(reader.GetOrdinal("IsAutomated")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    });
                }

                return results.Count > 0 ? Ok(results) : NotFound("No control options found for the provided HouseID.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
        [HttpPatch("water/on/{houseId}")]
        public async Task<IActionResult> PatchWaterOn(int houseId)
        {
            string query = @"
        UPDATE Sys_SensorControl
        SET WaterStatus = 1,
            UpdatedAt = @UpdatedAt
        WHERE HouseID = @HouseID";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@HouseID", houseId);

                await connection.OpenAsync();
                int result = await command.ExecuteNonQueryAsync();

                return result > 0 ? Ok(new { WaterStatus = 1 }) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPatch("water/off/{houseId}")]
        public async Task<IActionResult> PatchWaterOff(int houseId)
        {
            string query = @"
        UPDATE Sys_SensorControl
        SET WaterStatus = 0,
            UpdatedAt = @UpdatedAt
        WHERE HouseID = @HouseID";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@HouseID", houseId);

                await connection.OpenAsync();
                int result = await command.ExecuteNonQueryAsync();

                return result > 0 ? Ok(new { WaterStatus = 0 }) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPatch("light/on/{houseId}")]
        public async Task<IActionResult> PatchLightOn(int houseId)
        {
            string query = @"
        UPDATE Sys_SensorControl
        SET LightStatus = 1,
            UpdatedAt = @UpdatedAt
        WHERE HouseID = @HouseID";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@HouseID", houseId);

                await connection.OpenAsync();
                int result = await command.ExecuteNonQueryAsync();

                return result > 0 ? Ok(new { LightStatus = 1 }) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPatch("light/off/{houseId}")]
        public async Task<IActionResult> PatchLightOff(int houseId)
        {
            string query = @"
        UPDATE Sys_SensorControl
        SET LightStatus = 0,
            UpdatedAt = @UpdatedAt
        WHERE HouseID = @HouseID";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@HouseID", houseId);

                await connection.OpenAsync();
                int result = await command.ExecuteNonQueryAsync();

                return result > 0 ? Ok(new { LightStatus = 0 }) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPatch("fan/on/{houseId}")]
        public async Task<IActionResult> PatchFanOn(int houseId)
        {
            string query = @"
        UPDATE Sys_SensorControl
        SET FanStatus = 1,
            UpdatedAt = @UpdatedAt
        WHERE HouseID = @HouseID";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@HouseID", houseId);

                await connection.OpenAsync();
                int result = await command.ExecuteNonQueryAsync();

                return result > 0 ? Ok(new { FanStatus = 1 }) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPatch("fan/off/{houseId}")]
        public async Task<IActionResult> PatchFanOff(int houseId)
        {
            string query = @"
        UPDATE Sys_SensorControl
        SET FanStatus = 0,
            UpdatedAt = @UpdatedAt
        WHERE HouseID = @HouseID";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@HouseID", houseId);

                await connection.OpenAsync();
                int result = await command.ExecuteNonQueryAsync();

                return result > 0 ? Ok(new { FanStatus = 0 }) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPatch("automated/on/{houseId}")]
        public async Task<IActionResult> PatchIsAutomatedOn(int houseId)
        {
            string query = @"
        UPDATE Sys_SensorControl
        SET IsAutomated = 1,
            UpdatedAt = @UpdatedAt
        WHERE HouseID = @HouseID";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@HouseID", houseId);

                await connection.OpenAsync();
                int result = await command.ExecuteNonQueryAsync();

                return result > 0 ? Ok(new { IsAutomated = 1 }) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPatch("automated/off/{houseId}")]
        public async Task<IActionResult> PatchIsAutomatedOff(int houseId)
        {
            string query = @"
        UPDATE Sys_SensorControl
        SET IsAutomated = 0,
            UpdatedAt = @UpdatedAt
        WHERE HouseID = @HouseID";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@HouseID", houseId);

                await connection.OpenAsync();
                int result = await command.ExecuteNonQueryAsync();

                return result > 0 ? Ok(new { IsAutomated = 0 }) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }



    }
}
