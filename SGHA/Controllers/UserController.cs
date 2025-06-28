using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using SGHA.DTO;
using System;

namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly string _connectionString;

        public UserController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
        }

        [HttpGet("ShowAll")]
        public async Task<ActionResult<IEnumerable<ShowUserDto>>> GetAllUsers()
        {
            var users = new List<ShowUserDto>();

            // SQL query to get all users along with account email, role name, and house name
            string query = @"
        SELECT u.UserID, u.UserName, u.PhoneNumber, a.EmailAddress AS AccountEmail, r.RoleName,
               u.CreatedAt, u.UpdatedAt, u.IsActive, h.HouseName
        FROM Sys_User u
        LEFT JOIN Sys_Account a ON u.AccountID = a.AccountID
        LEFT JOIN Sys_Role r ON u.RoleID = r.RoleID
        LEFT JOIN Sys_House h ON u.HouseID = h.HouseID"; // Assuming UserID is linked to OwnerID in Sys_House

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
                                var user = new ShowUserDto
                                {
                                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                    AccountEmail = reader.GetString(reader.GetOrdinal("AccountEmail")),
                                    RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                    HouseID = reader.IsDBNull(reader.GetOrdinal("HouseName")) ? null : reader.GetString(reader.GetOrdinal("HouseName")) // Getting HouseName
                                };

                                users.Add(user);
                            }
                        }
                    }
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

        [HttpGet("by-house/{houseId}")]
        public async Task<ActionResult<IEnumerable<ShowUserDto>>> GetUsersByHouseId(string houseId)
        {
            var users = new List<ShowUserDto>();

            string query = @"
                SELECT u.UserID, u.UserName, u.PhoneNumber, a.EmailAddress AS AccountEmail, r.RoleName, 
                       u.CreatedAt, u.UpdatedAt, u.IsActive, h.HouseName
                FROM Sys_User u
                INNER JOIN Sys_Account a ON u.AccountID = a.AccountID
                INNER JOIN Sys_Role r ON u.RoleID = r.RoleID
                LEFT JOIN Sys_House h ON u.HouseID = h.HouseID
                WHERE u.HouseID = @HouseID";

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
                                    HouseID = reader.IsDBNull(reader.GetOrdinal("HouseName")) ? null : reader.GetString(reader.GetOrdinal("HouseName"))
                                };

                                users.Add(user);
                            }
                        }
                    }
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

        // PATCH: Set user status to Active by UserID
        [HttpPatch("set-status-active/{userID}")]
        public async Task<IActionResult> SetUserStatusActive(int userID)
        {
            if (userID <= 0)
            {
                return BadRequest("Invalid UserID.");
            }

            string query = @"
                UPDATE Sys_User
                SET 
                    Status = 'Active',
                    UpdatedAt = @UpdatedAt
                WHERE UserID = @UserID";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add parameters
                        command.Parameters.AddWithValue("@UserID", userID);
                        command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok($"User with ID {userID} status updated to Active.");
                        }

                        return NotFound($"User with ID {userID} not found.");
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

        // PATCH: Set user status to Inactive by UserID
        [HttpPatch("set-status-inactive/{userID}")]
        public async Task<IActionResult> SetUserStatusInactive(int userID)
        {
            if (userID <= 0)
            {
                return BadRequest("Invalid UserID.");
            }

            string query = @"
                UPDATE Sys_User
                SET 
                    Status = 'Inactive',
                    UpdatedAt = @UpdatedAt
                WHERE UserID = @UserID";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add parameters
                        command.Parameters.AddWithValue("@UserID", userID);
                        command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok($"User with ID {userID} status updated to Inactive.");
                        }

                        return NotFound($"User with ID {userID} not found.");
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
        // Patch Update Sys_User
        [HttpPatch("Update-User/{userId}")]
        public async Task<IActionResult> PatchUser(int userId, [FromBody] UpdateUserDto userDto)
        {
            if (userDto == null) return BadRequest("Invalid data.");

            var query = @"
                UPDATE Sys_User
                SET 
                    UserName = CASE WHEN @UserName IS NOT NULL THEN @UserName ELSE UserName END,
                    PhoneNumber = CASE WHEN @PhoneNumber IS NOT NULL THEN @PhoneNumber ELSE PhoneNumber END,
                    RoleID = CASE WHEN @RoleID IS NOT NULL THEN @RoleID ELSE RoleID END,
                    IsActive = CASE WHEN @IsActive IS NOT NULL THEN @IsActive ELSE IsActive END,
                    HouseID = CASE WHEN @HouseID IS NOT NULL THEN @HouseID ELSE HouseID END,
                    Status = CASE WHEN @Status IS NOT NULL THEN @Status ELSE Status END,
                    UpdatedAt = @UpdatedAt
                WHERE UserID = @UserID";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@UserName", (object?)userDto.UserName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@PhoneNumber", (object?)userDto.PhoneNumber ?? DBNull.Value);
                    command.Parameters.AddWithValue("@RoleID", (object?)userDto.RoleID ?? DBNull.Value);
                    command.Parameters.AddWithValue("@IsActive", (object?)userDto.IsActive ?? DBNull.Value);
                    command.Parameters.AddWithValue("@HouseID", (object?)userDto.HouseID ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Status", (object?)userDto.Status ?? DBNull.Value);
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                    await connection.OpenAsync();
                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    return rowsAffected > 0 ? Ok("User updated successfully.") : NotFound("User not found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        // Create User//
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (string.IsNullOrEmpty(createUserDto.EmailAddress) || string.IsNullOrEmpty(createUserDto.AccountPassword))
                return BadRequest("Email address and password are required.");
            if (string.IsNullOrEmpty(createUserDto.HouseID) || string.IsNullOrEmpty(createUserDto.HouseID))
                return BadRequest("HouseID are required.");
            if (string.IsNullOrEmpty(createUserDto.UserName) || string.IsNullOrEmpty(createUserDto.PhoneNumber))
                return BadRequest("User name and phone number are required.");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        // Insert into Sys_Account
                        var insertAccountQuery = @"
                            INSERT INTO Sys_Account (EmailAddress, AccountPassword, CreatedAt, UpdatedAt)
                            VALUES (@EmailAddress, @AccountPassword, @CreatedAt, @UpdatedAt);
                            SELECT SCOPE_IDENTITY();";

                        var accountCommand = new SqlCommand(insertAccountQuery, connection, transaction);
                        accountCommand.Parameters.AddWithValue("@EmailAddress", createUserDto.EmailAddress);
                        accountCommand.Parameters.AddWithValue("@AccountPassword", createUserDto.AccountPassword);
                        accountCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        accountCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                        int accountId = Convert.ToInt32(await accountCommand.ExecuteScalarAsync());

                        // Insert into Sys_User
                        var insertUserQuery = @"
                            INSERT INTO Sys_User (UserName, PhoneNumber, AccountID,HouseID, RoleID, CreatedAt, UpdatedAt, IsActive)
                            VALUES (@UserName, @PhoneNumber, @AccountID,@HouseID, @RoleID, @CreatedAt, @UpdatedAt, @IsActive);";

                        var userCommand = new SqlCommand(insertUserQuery, connection, transaction);
                        userCommand.Parameters.AddWithValue("@UserName", createUserDto.UserName);
                        userCommand.Parameters.AddWithValue("@PhoneNumber", createUserDto.PhoneNumber);
                        userCommand.Parameters.AddWithValue("@AccountID", accountId);
                        userCommand.Parameters.AddWithValue("@HouseID", createUserDto.HouseID);
                        userCommand.Parameters.AddWithValue("@RoleID", createUserDto.RoleId);
                        userCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        userCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                        userCommand.Parameters.AddWithValue("@IsActive", true);

                        await userCommand.ExecuteNonQueryAsync();

                        // Commit transaction
                        await transaction.CommitAsync();

                        return Ok(new { Message = "User created successfully", AccountID = accountId });
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


        // Delete Sys_User
        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var query = "DELETE FROM Sys_User WHERE UserID = @UserID";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);

                    await connection.OpenAsync();
                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    return rowsAffected > 0 ? Ok("User deleted successfully.") : NotFound("User not found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        ////


        [HttpDelete("delete-user-and-account/{userId}")]
        public async Task<IActionResult> DeleteUserAndAccountByUserId(int userId)
        {
            var getAccountIdQuery = "SELECT AccountID FROM Sys_User WHERE UserID = @UserID";
            var deleteUserQuery = "DELETE FROM Sys_User WHERE UserID = @UserID";
            var deleteAccountQuery = "DELETE FROM Sys_Account WHERE AccountID = @AccountID";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Start a transaction to ensure atomicity
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int? accountId = null;

                            // Retrieve the AccountID associated with the provided UserID
                            using (var getAccountIdCommand = new SqlCommand(getAccountIdQuery, connection, transaction))
                            {
                                getAccountIdCommand.Parameters.AddWithValue("@UserID", userId);
                                var result = await getAccountIdCommand.ExecuteScalarAsync();
                                if (result != null)
                                {
                                    accountId = Convert.ToInt32(result);
                                }
                                else
                                {
                                    transaction.Rollback();
                                    return NotFound("User not found.");
                                }
                            }

                            // Delete the user using the UserID
                            using (var deleteUserCommand = new SqlCommand(deleteUserQuery, connection, transaction))
                            {
                                deleteUserCommand.Parameters.AddWithValue("@UserID", userId);
                                var userRowsAffected = await deleteUserCommand.ExecuteNonQueryAsync();

                                if (userRowsAffected == 0)
                                {
                                    transaction.Rollback();
                                    return NotFound("User not found.");
                                }
                            }

                            // Delete the associated account using the AccountID
                            if (accountId.HasValue)
                            {
                                using (var deleteAccountCommand = new SqlCommand(deleteAccountQuery, connection, transaction))
                                {
                                    deleteAccountCommand.Parameters.AddWithValue("@AccountID", accountId.Value);
                                    var accountRowsAffected = await deleteAccountCommand.ExecuteNonQueryAsync();

                                    if (accountRowsAffected == 0)
                                    {
                                        transaction.Rollback();
                                        return NotFound("Associated account not found.");
                                    }
                                }
                            }

                            // Commit the transaction if all deletions are successful
                            transaction.Commit();
                            return Ok("User and associated account deleted successfully.");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return StatusCode(500, $"Internal server error during deletion: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
       
    }
}