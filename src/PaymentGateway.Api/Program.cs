using System.Text.Json.Serialization;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<PaymentsRepository>();
builder.Services.AddTransient<IPaymentService, PaymentsService>();
builder.Services.AddTransient<IBankHttpClient, AcquiringBankHttpClient>();
builder.Services.AddHttpClient<IBankHttpClient, AcquiringBankHttpClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration.GetValue<string>("AcquiringBankClientUrl") ?? string.Empty);
    c.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
