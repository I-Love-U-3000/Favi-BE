using Favi_BE.Common;
using Favi_BE.Data;
using Favi_BE.Data.Repositories;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Repositories;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOpt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        //options.Authority = builder.Configuration["Supabase:Url"]; // https://<project>.supabase.co
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, // Supabase tokens không có issuer chuẩn
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true, // Supabase cung cấp JWKS để ASP.NET tự fetch
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["SupabaseSecret"]!)),
            NameClaimType = "sub",
            RoleClaimType = "account_role"
        }; 
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("JWT auth failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var claims = context.Principal?.Claims.Select(c => $"{c.Type}:{c.Value}");
                Console.WriteLine("Token validated: " + string.Join(", ", claims ?? []));
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireUser", policy => policy.RequireClaim("account_role", "user"));
    options.AddPolicy("RequireAdmin", policy => policy.RequireClaim("account_role", "admin"));
});
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p
        .WithOrigins("http://localhost:3000", "http://localhost:*")
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IFollowRepository, FollowRepository>();
builder.Services.AddScoped<IPostCollectionRepository, PostCollectionRepository>();
builder.Services.AddScoped<IPostMediaRepository, PostMediaRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostTagRepository, PostTagRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IReactionRepository, ReactionRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ISocialLinkRepository, SocialLinkRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddHttpClient<ISupabaseAuthService, SupabaseAuthService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Supabase:Url"]);
    client.DefaultRequestHeaders.Add("apikey", builder.Configuration["Supabase:ApiKey"]);
});
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IPrivacyGuard, PrivacyGuard>();
builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection("Supabase"));

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // (Khuyến nghị) chờ Postgres sẵn sàng + retry vài lần
    var retries = 0;
    const int maxRetries = 10;
    while (true)
    {
        try
        {
            db.Database.Migrate(); // <- áp dụng tất cả migrations đang có
            break;
        }
        catch (Exception ex) when (retries < maxRetries)
        {
            retries++;
            Console.WriteLine($"[Migrate] retry {retries}/{maxRetries}: {ex.Message}");
            await Task.Delay(2000);
        }
    }
}

//app.UseSwagger(); 
//app.UseSwaggerUI();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
