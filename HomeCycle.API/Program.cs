using HomeCycle.API.Hubs;
using HomeCycle.API.Middlewares;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Services.Negotiates;
using HomeCycle.Application.Services.Negotiates;
using HomeCycle.Infrastructure;
using HomeCycle.Infrastructure.DbContexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

namespace HomeCycle.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddSignalR();
            builder.Services.AddScoped<IMessageService, MessageService>();
            builder.Services.AddSingleton<IChatRealtimePublisher, SignalRChatRealtimePublisher>();

            builder.Services.AddScoped<IAuthorizationHandler, ActiveUserHandler>();

            // Config CORS
            //builder.Services.AddCors(options =>
            //{
            //    options.AddPolicy("AllowAll", policy =>
            //    {
            //        policy.AllowAnyOrigin()
            //              .AllowAnyMethod()
            //              .AllowAnyHeader()
            //              .AllowCredentials(); // truyền Connection ID
            //    });
            //});

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policy =>
                {
                    policy.SetIsOriginAllowed(origin => true) // Chấp nhận mọi origin động nhưng vẫn hợp lệ với AllowCredentials
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            

            // Add services to the container.

            builder.Services.AddControllers();

            // read enums to text
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            //builder.Services.AddControllers(options =>
            //{
            //    options.ModelBinderProviders.Insert(0, new JsonModelBinderProvider());
            //});
            builder.Services.AddControllers(options =>
            {
                options.ModelBinderProviders.Insert(0, new JsonModelBinderProvider());
            });

            builder.Services.AddEndpointsApiExplorer();

            //Thêm JWT Security Definition
            //builder.Services.AddSwaggerGen();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "HomeCycle API",
                    Version = "v1"
                });

                // Kích hoạt đọc thuộc tính [SwaggerOperation] từ Swashbuckle.AspNetCore.Annotations
                options.EnableAnnotations();

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter Access Token"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                        Array.Empty<string>()
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            //Khai báo DI
            builder.Services.AddInfrastructure(builder.Configuration);

            // Add DbContext with PostgreSQL configuration
            builder.Services.AddDbContext<HomeCycleDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                ));

            // Config JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                //Config SignalR
                options.Events ??= new JwtBearerEvents();

                options.Events.OnMessageReceived = context =>
                {
                    var accessToken =
                        context.Request.Query["access_token"].ToString();

                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrWhiteSpace(accessToken) &&
                        path.StartsWithSegments(ChatHub.Route))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                };
            });

            ////builder.Services.AddAuthorization();
            //builder.Services.AddAuthorization(options =>
            //{
            //    options.FallbackPolicy = new AuthorizationPolicyBuilder()
            //        .RequireAuthenticatedUser()
            //        .AddRequirements(new ActiveUserRequirement())
            //        .Build();
            //    // FallbackPolicy áp dụng cho MỌI endpoint có [Authorize] (không có tên Policy cụ thể) —
            //    // tự động phủ toàn bộ Controller hiện có, không cần sửa từng Controller một.
            //});

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "HomeCycle API V1");
                c.RoutePrefix = "swagger"; // Đường dẫn truy cập sẽ là /swagger
            });

            if (!app.Environment.IsProduction()) // Hoặc if (app.Environment.IsDevelopment() && !Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER").Equals("true"))
            {
                // Tốt nhất nếu chạy Docker hoàn toàn thì comment hẳn dòng dưới này lại:
                app.UseHttpsRedirection();
            }

            app.UseWebSockets();

            app.UseRouting();

            app.UseCors("CorsPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<ChatHub>(ChatHub.Route).RequireAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
