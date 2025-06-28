using Dapper;
using MailKit.Net.Smtp;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
//using MimeKit;
//using NETCore.MailKit.Core;
using SGHA.DTO;
using SGHA.Interfaces;
using System.Text;

namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;


        public AuthController(IConfiguration configuration, IEmailService emailService)
        {
            _connectionString = configuration.GetConnectionString("Default");
            _config = configuration;
            _emailService = emailService;
        }

        private string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
            StringBuilder newPassword = new StringBuilder();
            Random random = new Random();

            while (0 < length--)
            {
                newPassword.Append(validChars[random.Next(validChars.Length)]);
            }

            return newPassword.ToString();
        }

        [HttpPost("sendEmail")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequestDto emailRequest)
        {
            if (emailRequest == null || string.IsNullOrEmpty(emailRequest.To) || string.IsNullOrEmpty(emailRequest.Subject) || string.IsNullOrEmpty(emailRequest.Body))
            {
                return BadRequest("Invalid email request.");
            }

            try
            {
                await _emailService.SendEmailAsync(emailRequest);
                return Ok("Email sent successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (loginDto == null || string.IsNullOrEmpty(loginDto.EmailAddress) || string.IsNullOrEmpty(loginDto.AccountPassword))
                return BadRequest("Email address and password are required.");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
            SELECT u.UserID, u.UserName, u.PhoneNumber, r.RoleName, u.IsActive ,u.HouseID
            FROM Sys_User u
            INNER JOIN Sys_Account a ON u.AccountID = a.AccountID
            INNER JOIN Sys_Role r ON u.RoleID = r.RoleID
            WHERE a.EmailAddress = @EmailAddress AND a.AccountPassword = @AccountPassword AND u.IsActive = 'true'";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@EmailAddress", loginDto.EmailAddress);
                        command.Parameters.AddWithValue("@AccountPassword", loginDto.AccountPassword);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Create a response object
                                var response = new
                                {
                                    UserID = reader["UserID"] as int?,
                                    HouseID = reader["HouseID"] as int?,
                                    UserName = reader["UserName"],
                                    RoleName = reader["RoleName"]
                                };

                                return Ok(response); // Return the user details
                            }
                            else
                            {
                                return Unauthorized("Invalid email or password.");
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Catch database-related errors
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordRequest)
        {
            if (resetPasswordRequest == null || string.IsNullOrEmpty(resetPasswordRequest.Email))
            {
                return BadRequest(new { success = false, message = "Invalid request." });
            }

            string email;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT EmailAddress FROM Sys_Account WHERE EmailAddress = @EmailAddress";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@EmailAddress", resetPasswordRequest.Email);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return NotFound(new { success = false, message = "Username not found." });
                            }
                            email = reader["EmailAddress"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }

            var newPassword = GenerateRandomPassword();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "UPDATE Sys_Account SET AccountPassword = @Password WHERE EmailAddress = @EmailAddress";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Password", newPassword);
                        command.Parameters.AddWithValue("@EmailAddress", resetPasswordRequest.Email);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            return NotFound(new { success = false, message = "Username not found." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }

            try
            {
                await _emailService.SendEmailAsync(new EmailRequestDto
                {
                    To = email ?? "",
                    Subject = "Password Reset",
                    Body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{
                                margin: 0;
                                padding: 0;
                                font-family: Arial, sans-serif;
                            }}
                        </style>
                    </head>
                    <body>
                        <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f4f4; padding: 20px;'>
                            <tr>
                                <td align='center'>
                                    <table width='600px' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border: 1px solid #ddd;'>
                                        <tr>
                                            <td align='center' style='background-color: #4CAF50; color: #ffffff; padding: 20px; font-size: 24px; font-weight: bold;'>
                                                Password Reset Successful
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 20px; color: #333333; font-size: 16px;'>
                                                <p>Dear User,</p>
                                                <p>Your password has been successfully reset. Below is your new password:</p>
                                                <p style='text-align: center; background-color: #f9f9f9; padding: 15px; border: 1px solid #ddd; font-size: 18px; font-weight: bold;'>
                                                    {newPassword}
                                                </p>
                                                <p>
                                                    Please use this password to log in to your account. For your security, it is strongly recommended that you change your password after logging in.
                                                </p>
                                                <p>
                                                    If you did not request a password reset, please contact our support team immediately.
                                                </p>
                                                <p>Thank you,<br><strong>Smart Green House Team</strong></p>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td align='center' style='color: #777777; font-size: 14px; padding: 20px;'>
                                                This is an automated email. Please do not reply to this email.
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                        </table>
                    </body>
                    </html>
                    "
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Failed to send email: {ex.Message}" });
            }

            return Ok(new { success = true, message = "Password has been reset." });
        }
    }
}