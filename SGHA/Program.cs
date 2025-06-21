
//using Fleck;

using SGHA.Hubs;

namespace SGHA
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

           
          
            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            //added for SignalR
            builder.Services.AddSignalR();


            var app = builder.Build();


            // Update the CORS configuration to ensure proper handling of preflight requests and allow specific origins.  
            app.UseCors(policyBuilder =>
            {
                policyBuilder.WithOrigins("http://localhost:4200", "https://greensync-sigma.vercel.app") // Allow the Angular app's origin.  
                             .AllowAnyHeader()
                             .AllowAnyMethod()
                             .AllowCredentials(); // Ensure credentials are allowed if needed.  
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }



            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapHub<ControlStatusHub>("/controlStatusHub");

            app.MapControllers();

            app.Run();
        }
    }
}
