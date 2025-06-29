using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGHA.DTO;

namespace SGHA.Controllers
{
    public class AIImageController : Controller
    {

        private readonly string _connectionString;

        public AIImageController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        [HttpPost("{houseId}")]
        public async Task<IActionResult> ReceiveAIResult(int houseId, [FromBody] AIImageUploadDto dto)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var aiImageId = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO Sys_AIImage (HouseID) 
                      VALUES (@HouseID); 
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { HouseID = houseId }, transaction);

                foreach (var result in dto.Results)
                {
                    await connection.ExecuteAsync(
                        @"INSERT INTO Sys_AIImageResult 
                          (AIImageId, Model, Detection, IsRipe, IsHealthy, IsPest, ActionNeeded)
                          VALUES 
                          (@AIImageId, @Model, @Detection, @IsRipe, @IsHealthy, @IsPest, @ActionNeeded);",
                        new
                        {
                            AIImageId = aiImageId,
                            result.Model,
                            result.Detection,
                            result.IsRipe,
                            result.IsHealthy,
                            result.IsPest,
                            result.ActionNeeded
                        }, transaction);
                }

                foreach (var image in dto.Images)
                {
                    await connection.ExecuteAsync(
                        @"INSERT INTO Sys_AIImageFiles (AIImageId, FileName)
                          VALUES (@AIImageId, @FileName);",
                        new { AIImageId = aiImageId, FileName = image }, transaction);
                }

                transaction.Commit();
                return Ok(new { Message = "AI image results saved successfully", AIImageId = aiImageId });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


    }
}