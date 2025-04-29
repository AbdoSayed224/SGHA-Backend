using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGHA.DTO;

namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HouseController : ControllerBase
    {
        private readonly string _connectionString;

        public HouseController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
        }
        //Show all GreeenHouse
        [HttpGet("AllHouse")]
        public async Task<ActionResult<IEnumerable<ShowHouseDto>>> GetAllHouses()
        {
            var houses = new List<ShowHouseDto>();

            string query = @"
        SELECT h.HouseID, h.HouseName, h.Location, h.SizeSquareMeters, h.Status, 
               u.UserName AS OwnerUserName, h.LastMaintenance, h.CreatedAt, h.UpdatedAt
        FROM Sys_House h
        LEFT JOIN Sys_User u ON h.OwnerID = u.UserID";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var house = new ShowHouseDto
                                {
                                    HouseID = reader.GetInt32(reader.GetOrdinal("HouseID")),
                                    HouseName = reader.GetString(reader.GetOrdinal("HouseName")),
                                    Location = reader.GetString(reader.GetOrdinal("Location")),
                                    SizeSquareMeters = reader.GetDouble(reader.GetOrdinal("SizeSquareMeters")),
                                    Status = reader.GetString(reader.GetOrdinal("Status")),
                                    NameOwner = reader.IsDBNull(reader.GetOrdinal("OwnerUserName")) ? null : reader.GetString(reader.GetOrdinal("OwnerUserName"))
                                };

                                houses.Add(house);
                            }
                        }
                    }
                }

                if (houses.Count == 0)
                {
                    return NotFound("No houses found.");
                }

                return Ok(houses);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Users by GreenHouse ID
        [HttpGet("ByHouseId/{houseId}")]
        public async Task<ActionResult<IEnumerable<ShowUserDto>>> GetUsersByHouseId(int houseId)
        {
            var users = new List<ShowUserDto>();

            string query = @"
        SELECT u.UserID, u.UserName, u.PhoneNumber, a.EmailAddress AS AccountEmail, r.RoleName, 
               u.CreatedAt, u.UpdatedAt, u.IsActive, h.HouseName
        FROM Sys_User u
        INNER JOIN Sys_Account a ON u.AccountID = a.AccountID
        INNER JOIN Sys_Role r ON u.RoleID = r.RoleID
        LEFT JOIN Sys_House h ON u.UserID = h.OwnerID
        WHERE h.HouseID = @HouseID";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@HouseID", houseId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var user = new ShowUserDto
                                {
                                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                    AccountEmail = reader.GetString(reader.GetOrdinal("AccountEmail")),
                                    RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                    HouseID = reader.IsDBNull(reader.GetOrdinal("HouseID")) ? null : reader.GetString(reader.GetOrdinal("HouseID"))
                                };

                                users.Add(user);
                            }
                        }
                    }
                }

                if (users.Count == 0)
                {
                    return NotFound("No users found for the provided HouseID.");
                }

                return Ok(users);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Patch Sys_House
        [HttpPatch("Update-House/{houseId}")]
        public async Task<IActionResult> PatchHouse(int houseId, [FromBody] UpdateHouseDto houseDto)
        {
            if (houseDto == null) return BadRequest("Invalid data.");

            var query = @"
                UPDATE Sys_House
                SET 
                    HouseName = CASE WHEN @HouseName IS NOT NULL THEN @HouseName ELSE HouseName END,
                    Location = CASE WHEN @Location IS NOT NULL THEN @Location ELSE Location END,
                    SizeSquareMeters = CASE WHEN @SizeSquareMeters IS NOT NULL THEN @SizeSquareMeters ELSE SizeSquareMeters END,
                    Status = CASE WHEN @Status IS NOT NULL THEN @Status ELSE Status END,
                    OwnerID = CASE WHEN @OwnerID IS NOT NULL THEN @OwnerID ELSE OwnerID END,
                    LastMaintenance = CASE WHEN @LastMaintenance IS NOT NULL THEN @LastMaintenance ELSE LastMaintenance END,
                    UpdatedAt = @UpdatedAt
                WHERE HouseID = @HouseID";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@HouseID", houseId);
                    command.Parameters.AddWithValue("@HouseName", (object?)houseDto.HouseName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Location", (object?)houseDto.Location ?? DBNull.Value);
                    command.Parameters.AddWithValue("@SizeSquareMeters", (object?)houseDto.SizeSquareMeters ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Status", (object?)houseDto.Status ?? DBNull.Value);
                    command.Parameters.AddWithValue("@OwnerID", (object?)houseDto.OwnerID ?? DBNull.Value);
                    command.Parameters.AddWithValue("@LastMaintenance", (object?)houseDto.LastMaintenance ?? DBNull.Value);
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                    await connection.OpenAsync();
                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    return rowsAffected > 0 ? Ok("House updated successfully.") : NotFound("House not found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        // Make a House Maintenance
        [HttpPatch("maintenance")]
        public async Task<IActionResult> SetHouseToMaintenance([FromQuery] int houseId)
        {
            if (houseId <= 0)
            {
                return BadRequest("Invalid HouseID.");
            }

            string query = @"
        UPDATE Sys_House
        SET 
            Status = @Status,
            LastMaintenance = @LastMaintenance,
            UpdatedAt = @UpdatedAt
        WHERE HouseID = @HouseID";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@HouseID", houseId);
                        command.Parameters.AddWithValue("@Status", "Maintenance");
                        command.Parameters.AddWithValue("@LastMaintenance", DateTime.Now);
                        command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok($"House with ID {houseId} updated to 'Maintenance' successfully.");
                        }

                        return NotFound($"House with ID {houseId} not found.");
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        // make a House as Active
        [HttpPatch("{houseId}/activate")]
        public async Task<IActionResult> ActivateHouse(int houseId)
        {
            if (houseId <= 0)
            {
                return BadRequest("Invalid HouseID.");
            }

            string query = "UPDATE Sys_House SET Status = 'Active', UpdatedAt = @UpdatedAt WHERE HouseID = @HouseID";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                        command.Parameters.AddWithValue("@HouseID", houseId);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("House has been activated successfully.");
                        }

                        return NotFound("House not found.");
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        // make a House as inActive


        [HttpPatch("{houseId}/inactive")]
        public async Task<IActionResult> MarkHouseAsNoActive(int houseId)
        {
            if (houseId <= 0)
            {
                return BadRequest("Invalid HouseID.");
            }

            string query = "UPDATE Sys_House SET Status = @Status, UpdatedAt = @UpdatedAt WHERE HouseID = @HouseID";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Status", "No_Active");
                        command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                        command.Parameters.AddWithValue("@HouseID", houseId);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok($"House with ID {houseId} marked as No_Active successfully.");
                        }

                        return NotFound($"House with ID {houseId} not found.");
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        //Add new GreenHouse
        [HttpPost("AddHouse")]
        public async Task<IActionResult> InsertHouse([FromBody] AddHouseDto houseDto)
        {
            if (houseDto == null)
            {
                return BadRequest("Invalid house data.");
            }

            // SQL query to insert a new house
            string query = @"
                INSERT INTO Sys_House (HouseName, Location, SizeSquareMeters, Status, OwnerID, LastMaintenance, CreatedAt, UpdatedAt)
                VALUES (@HouseName, @Location, @SizeSquareMeters, @Status, @OwnerID, @LastMaintenance, @CreatedAt, @UpdatedAt)";

            try
            {
                // Open connection and execute query asynchronously
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add parameters to the command
                        command.Parameters.AddWithValue("@HouseName", houseDto.HouseName);
                        command.Parameters.AddWithValue("@Location", houseDto.Location);
                        command.Parameters.AddWithValue("@SizeSquareMeters", houseDto.SizeSquareMeters);
                        command.Parameters.AddWithValue("@Status", houseDto.Status);
                        command.Parameters.AddWithValue("@OwnerID", houseDto.OwnerID);
                        command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                        // Execute the command asynchronously
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("House inserted successfully.");
                        }

                        return StatusCode(500, "An error occurred while inserting the house.");
                    }
                }
            }
            catch (SqlException ex)
            {
                // Catch SQL exceptions (e.g., connectivity issues, invalid query)
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Catch general exceptions (e.g., invalid data, network issues)
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}