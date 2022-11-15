using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

using surveyProject.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using System.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(opt => opt.AddPolicy("CorsPolicy", c =>
{
    c.AllowAnyOrigin()
       .AllowAnyHeader()
       .AllowAnyMethod();
}));
// Add services to the container.
builder.Services.AddControllers()
           .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddControllersWithViews()
                .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection("ApplicationSettings"));
builder.Services.AddControllers();
builder.Services.AddDbContext<AuthenticationContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityConnection")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthenticationContext>()
    .AddDefaultTokenProviders();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
}
            );
// ADD JWT Authentication
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.RequireHttpsMetadata = false;
    o.Events = new JwtBearerEvents();
    o.Events.OnTokenValidated = context =>
    {
        context.Response.StatusCode = 200;
        return Task.CompletedTask;
    };
    o.Events.OnAuthenticationFailed = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
    o.Events.OnChallenge = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
    o.Events.OnMessageReceived = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
    var key = Encoding.UTF8.GetBytes(builder.Configuration["jwtConfig:Key"]);
    o.SaveToken = true;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["jwtConfig:Issuer"],
        ClockSkew = TimeSpan.Zero,
        //ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});
builder.Services.AddAuthorization(auth =>
{
    auth.AddPolicy("Bearer", new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser().Build());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseForwardedHeaders();

app.UseCors(builder => builder.WithOrigins("http://localhost:4200")/*.AllowAnyOrigin()*/.AllowAnyHeader().AllowAnyMethod());
app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
