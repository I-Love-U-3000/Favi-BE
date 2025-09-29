using Favi_BE.Auth;
using Favi_BE.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOpt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOpt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOpt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p
        .WithOrigins("http://localhost:3000", "http://localhost:*")
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

builder.Services.AddScoped<IJwtService, JwtService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
//Swagger is not used for this project as of now
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new() { Title = "Favi API", Version = "v1" });
//    c.AddSecurityDefinition("Bearer", new()
//    {
//        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
//        Description = "Enter: Bearer {token}",
//        Name = "Authorization",
//        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
//        Scheme = "bearer",
//        BearerFormat = "JWT"
//    });
//    c.AddSecurityRequirement(new()
//    {
//        {
//            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
//            Array.Empty<string>()
//        }
//    });
//});

var app = builder.Build();

//app.UseSwagger(); 
//app.UseSwaggerUI();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
