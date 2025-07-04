﻿using Microsoft.AspNetCore.Http.Features;
using SGHA.Hubs;
using SGHA.Interfaces;
using SGHA.Services;

namespace SGHA
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddScoped<IEmailService, EmailService>();


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            
            builder.Services.AddSwaggerGen();
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 104857600; // 100MB or any size
            });
          
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
            app.MapHub<AiHub>("/aiHub");

            app.MapControllers();

            app.Run();
        }
    }
}
