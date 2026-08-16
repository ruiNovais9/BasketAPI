using BasketAPI.Interfaces;
using BasketAPI.Middleware;
using BasketAPI.Services;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders
        | HttpLoggingFields.RequestBody
        | HttpLoggingFields.ResponsePropertiesAndHeaders
        | HttpLoggingFields.ResponseBody;
});

builder.Services.AddSingleton<IProductService, ProductService>();
builder.Services.AddSingleton<IBasketService, BasketService>();

builder.Services.AddHttpClient<IImpactApiClient, ImpactApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ImpactApi:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

app.UseHttpLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionMiddleware();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
