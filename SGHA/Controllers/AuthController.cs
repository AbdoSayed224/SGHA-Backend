using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MimeKit;
using SGHA.DTO;

namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
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


        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest("Email is required.");

            // Generate a random password
            string newPassword = GenerateRandomPassword(12); // 12 characters long

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Update the user's password in the database
                string updateQuery = @"
           Update Sys_Account
            SET AccountPassword= @AccountPassword, UpdatedAt = @UpdatedAt 
            WHERE EmailAddress = @Email";
                int rowsAffected = await connection.ExecuteAsync(updateQuery, new
                {
                    AccountPassword = newPassword,
                    UpdatedAt = DateTime.UtcNow,
                    Email = request.Email
                });

                if (rowsAffected == 0)
                    return NotFound("Email not found.");
            }

            // Send the new password via email
            await SendResetEmailAsync(request.Email, newPassword);

            return Ok("Password reset successfully. The new password has been sent to the email.");
        }


        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                                        .Select(s => s[random.Next(s.Length)])
                                        .ToArray());
        }

        private async Task SendResetEmailAsync(string email, string newPassword)
        {
            var smtpHost = "smtp.gmail.com"; // Gmail SMTP server
            var smtpPort = 465; // SSL port
            var smtpUser = "agha961555@gmail.com"; // Replace with your email
            var smtpPass = "anrq ijye bqhx rfgx"; // Replace with your app password

            // Create the email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Smart Green House", smtpUser));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Password Reset - Smart Green House";

            // Create the email body in HTML
            message.Body  = new TextPart("html")
            {
                Text = $@"
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
"};



            // Send the email using MailKit
            using (var smtpClient = new MailKit.Net.Smtp.SmtpClient())
            {
                try
                {
                    // Connect to the SMTP server
                    await smtpClient.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.SslOnConnect);

                    // Authenticate with the server
                    await smtpClient.AuthenticateAsync(smtpUser, smtpPass);

                    // Send the email
                    await smtpClient.SendAsync(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending email: {ex.Message}");
                    throw; // Optionally rethrow the exception
                }
                finally
                {
                    // Disconnect from the server
                    await smtpClient.DisconnectAsync(true);
                }
            }
        }

    }
}
