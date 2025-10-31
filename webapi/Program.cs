using webapi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<IIdempotencyKeyService, IdempotencyKeyService>();
builder.Services.AddSingleton<INaturalIdempotencyService, NaturalIdempotencyService>();
builder.Services.AddSingleton<IVersionBasedIdempotencyService, VersionBasedIdempotencyService>();
builder.Services.AddSingleton<ITokenBasedIdempotencyService, TokenBasedIdempotencyService>();
builder.Services.AddSingleton<ITimestampDeduplicationService, TimestampDeduplicationService>();
builder.Services.AddSingleton<IContentBasedDeduplicationService, ContentBasedDeduplicationService>();

builder.Services.AddSingleton<MessageQueueService>();

builder.Services.AddHostedService<MessageConsumerBackgroundService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API de Idempotência",
        Version = "v1",
        Description = @"",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "API Educacional"
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
