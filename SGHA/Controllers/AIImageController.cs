using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGHA.DTO;

namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class AIImageController : Controller
    {

        private readonly string _connectionString;

        public AIImageController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("Default");
        }

        [HttpGet("image-file/{fileId}")]
        public async Task<IActionResult> GetImageFile(int fileId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand("SELECT FileData FROM Sys_AIImageFiles WHERE Id = @FileId", connection);
            cmd.Parameters.AddWithValue("@FileId", fileId);

            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
                return NotFound("Image not found.");

            byte[] imageData = (byte[])result;

            return File(imageData, "image/jpeg");
        }

        [HttpGet("by-house/{houseId}")]
        public async Task<IActionResult> GetAllAIImagesByHouse(int houseId)
        {
            var images = new List<AIImageDto>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get all AI image records for the house
            var selectImagesCmd = new SqlCommand(@"
        SELECT Id, HouseID, CreatedAt 
        FROM Sys_AIImage
        WHERE HouseID = @HouseID
        ORDER BY Id DESC;", connection);
            selectImagesCmd.Parameters.AddWithValue("@HouseID", houseId);

            using var imageReader = await selectImagesCmd.ExecuteReaderAsync();
            while (await imageReader.ReadAsync())
            {
                images.Add(new AIImageDto
                {
                    AIImageId = imageReader.GetInt32(0),
                    HouseID = imageReader.GetInt32(1),
                    CreatedAt = imageReader.GetDateTime(2)
                });
            }
            imageReader.Close();

            if (!images.Any())
                return NotFound("No data found.");

            foreach (var img in images)
            {
                // Get results
                var resultCmd = new SqlCommand(@"
            SELECT Model, Detection, IsRipe, IsHealthy, IsPest, ActionNeeded
            FROM Sys_AIImageResult
            WHERE AIImageId = @AIImageId", connection);
                resultCmd.Parameters.AddWithValue("@AIImageId", img.AIImageId);

                using var resultReader = await resultCmd.ExecuteReaderAsync();
                while (await resultReader.ReadAsync())
                {
                    img.Results.Add(new AIImageResultDto
                    {
                        Model = resultReader["Model"]?.ToString(),
                        Detection = resultReader["Detection"]?.ToString(),
                        IsRipe = resultReader["IsRipe"] as bool?,
                        IsHealthy = resultReader["IsHealthy"] as bool?,
                        IsPest = resultReader["IsPest"] as bool?,
                        ActionNeeded = resultReader["ActionNeeded"]?.ToString()
                    });
                }
                resultReader.Close();

                // Get ALL images as URLs
                var fileCmd = new SqlCommand(@"
            SELECT Id 
            FROM Sys_AIImageFiles 
            WHERE AIImageId = @AIImageId
            ORDER BY Id ASC", connection);
                fileCmd.Parameters.AddWithValue("@AIImageId", img.AIImageId);

                using var fileReader = await fileCmd.ExecuteReaderAsync();
                while (await fileReader.ReadAsync())
                {
                    int fileId = fileReader.GetInt32(0);
                    string imageUrl = Url.Action(
                        action: "GetImageFile",
                        controller: "AIImage",
                        values: new { fileId = fileId },
                        protocol: Request.Scheme
                    );

                    if (!string.IsNullOrEmpty(imageUrl))
                        img.ImageUrls.Add(imageUrl);
                }
                fileReader.Close();
            }

            return Ok(images);
        }



        [HttpPost("{houseId}")]
        public async Task<IActionResult> ReceiveAIResult(int houseId, [FromBody] AIImageUploadDto dto)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Insert main AI image record
                var aiImageId = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO Sys_AIImage (HouseID) 
              VALUES (@HouseID); 
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { HouseID = houseId }, transaction);

                // 2. Insert AI analysis results
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

                // 3. Insert each image as binary data
                foreach (var base64Image in dto.Images)
                {
                    byte[] imageBytes;

                    try
                    {
                        imageBytes = Convert.FromBase64String(base64Image);
                    }
                    catch
                    {
                        transaction.Rollback();
                        return BadRequest("One or more images are not valid Base64 strings.");
                    }

                    await connection.ExecuteAsync(
                        @"INSERT INTO Sys_AIImageFiles (AIImageId, FileData)
                  VALUES (@AIImageId, @FileData);",
                        new { AIImageId = aiImageId, FileData = imageBytes }, transaction);
                }

                transaction.Commit();
                return Ok(new { Message = "AI image results saved successfully", AIImageId = aiImageId });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, new { Error = ex.Message });
            }
        }
        [HttpGet("by-id/{aiImageId}")]
        public async Task<IActionResult> GetAIImageById(int aiImageId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get AI image record by Id
            var selectImageCmd = new SqlCommand(@"
                SELECT Id, HouseID, CreatedAt 
                FROM Sys_AIImage
                WHERE Id = @AIImageId;", connection);
            selectImageCmd.Parameters.AddWithValue("@AIImageId", aiImageId);

            AIImageDto? image = null;
            using (var imageReader = await selectImageCmd.ExecuteReaderAsync())
            {
                if (await imageReader.ReadAsync())
                {
                    image = new AIImageDto
                    {
                        AIImageId = imageReader.GetInt32(0),
                        HouseID = imageReader.GetInt32(1),
                        CreatedAt = imageReader.GetDateTime(2)
                    };
                }
            }

            if (image == null)
                return NotFound("No data found.");

            // Get results
            var resultCmd = new SqlCommand(@"
                SELECT Model, Detection, IsRipe, IsHealthy, IsPest, ActionNeeded
                FROM Sys_AIImageResult
                WHERE AIImageId = @AIImageId", connection);
            resultCmd.Parameters.AddWithValue("@AIImageId", aiImageId);

            using (var resultReader = await resultCmd.ExecuteReaderAsync())
            {
                while (await resultReader.ReadAsync())
                {
                    image.Results.Add(new AIImageResultDto
                    {
                        Model = resultReader["Model"]?.ToString(),
                        Detection = resultReader["Detection"]?.ToString(),
                        IsRipe = resultReader["IsRipe"] as bool?,
                        IsHealthy = resultReader["IsHealthy"] as bool?,
                        IsPest = resultReader["IsPest"] as bool?,
                        ActionNeeded = resultReader["ActionNeeded"]?.ToString()
                    });
                }
            }

            // Get ALL images as URLs
            var fileCmd = new SqlCommand(@"
                SELECT Id 
                FROM Sys_AIImageFiles 
                WHERE AIImageId = @AIImageId
                ORDER BY Id ASC", connection);
            fileCmd.Parameters.AddWithValue("@AIImageId", aiImageId);

            using (var fileReader = await fileCmd.ExecuteReaderAsync())
            {
                while (await fileReader.ReadAsync())
                {
                    int fileId = fileReader.GetInt32(0);
                    string imageUrl = Url.Action(
                        action: "GetImageFile",
                        controller: "AIImage",
                        values: new { fileId = fileId },
                        protocol: Request.Scheme
                    );

                    if (!string.IsNullOrEmpty(imageUrl))
                        image.ImageUrls.Add(imageUrl);
                }
            }

            return Ok(image);
        }

        public class AIImageUploadDto
        {
            public List<AIImageResultDto>? Results { get; set; }
            public List<string>? Images { get; set; }
        }

        public class AIImageDto
        {
            public int? AIImageId { get; set; }
            public int? HouseID { get; set; }
            public DateTime? CreatedAt { get; set; }
            public List<AIImageResultDto>? Results { get; set; } = new();
            public List<string>? ImageUrls { get; set; } = new();  // بدل MainImageUrl
        }

        public class AIImageResultDto
        {
            public string? Model { get; set; }
            public string? Detection { get; set; }
            public bool? IsRipe { get; set; }
            public bool? IsHealthy { get; set; }
            public bool? IsPest { get; set; }
            public string? ActionNeeded { get; set; }
        }
    }
}