using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.OpenApi.Models;
using Npgsql;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = true;
        options.AccessDeniedPath = "/Forbidden/";
    });

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = BearerTokenDefaults.AuthenticationScheme; // this is the default scheme to be used
        options.DefaultChallengeScheme = BearerTokenDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = BearerTokenDefaults.AuthenticationScheme;
    }).AddBearerToken();



builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});


var connectionString = builder.Configuration.GetConnectionString("PostgreDB");
builder.Services.AddScoped((provider) => new NpgsqlConnection(connectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});


builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<SwaggerTryItOutDefaulValue>(); // чтобы работало предзаполнение атрибутов дефолтными значениями
    options.SwaggerDoc("v1", new OpenApiInfo() { Title = "PPSN API", Version = "v1" });
    options.AddSecurityDefinition(BearerTokenDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Description = @"Bearer token in header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.
                      <br />Example: '12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = BearerTokenDefaults.AuthenticationScheme
    });

    options.OperationFilter<AuthorizeCheckOperationFilter>();
});

builder.Services.AddHttpLogging(options => {
    options.LoggingFields = HttpLoggingFields.All;
});

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 70 * 1024 * 1024);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpLogging();
}

app.UseHttpsRedirection();

app.UseRouting();
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();



app.Run();
