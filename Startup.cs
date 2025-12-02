using AccessPeople.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YourProject.Services;

namespace AccessPeople
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        //public void ConfigureServices(IServiceCollection services)
        //{
        //    services.AddControllers();

        //    // Register DBcontext and Service
        //    services.AddSingleton<DBcontext>();
        //    services.AddSingleton<AssessPeopleService>();
        //    services.AddHttpClient();

        //    // Swagger
        //    services.AddSwaggerGen(c =>
        //    {
        //        c.SwaggerDoc("v1", new OpenApiInfo { Title = "AccessPeople API", Version = "v1" });

        //        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        //        {
        //            Name = "Authorization",
        //            Type = SecuritySchemeType.ApiKey,
        //            Scheme = "Bearer",
        //            In = ParameterLocation.Header,
        //            Description = "Enter: Bearer {your token}"
        //        });

        //        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        //{
        //    {
        //        new OpenApiSecurityScheme
        //        {
        //            Reference = new OpenApiReference
        //            {
        //                Type = ReferenceType.SecurityScheme,
        //                Id = "Bearer"
        //            }
        //        },
        //        new string[] {}
        //    }
        //});
        //    });
        //}
        //public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        //{
        //    if (env.IsDevelopment())
        //    {
        //        app.UseDeveloperExceptionPage();
        //        app.UseSwagger();

        //        app.UseSwaggerUI(c =>
        //        {
        //            c.SwaggerEndpoint("/swagger/v1/swagger.json", "AccessPeople API v1");
        //            c.RoutePrefix = "swagger";  // <--- THIS makes Swagger available at /swagger
        //        });
        //    }

        //    app.UseHttpsRedirection();
        //    app.UseRouting();
        //    app.UseAuthorization();

        //    app.UseEndpoints(endpoints =>
        //    {
        //        endpoints.MapControllers();
        //    });
        //}

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Register DBcontext and Service
            services.AddSingleton<DBcontext>();
            services.AddSingleton<AssessPeopleService>();
            services.AddHttpClient();

            // Enable CORS for Angular
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", builder =>
                {
                    builder.WithOrigins("http://localhost:4200")
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AccessPeople API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    In = ParameterLocation.Header,
                    Description = "Enter: Bearer {your token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AccessPeople API v1");
                    c.RoutePrefix = "swagger";
                });
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // Enable CORS before Authorization
            app.UseCors("AllowAngular");

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }


    }
}
