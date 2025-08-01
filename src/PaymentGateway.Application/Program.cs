using System.Text.Json.Serialization;

using PaymentGateway.Application.Mappers;
using PaymentGateway.Application.UseCases;
using PaymentGateway.Domain.Interfaces.Repositories;
using PaymentGateway.Domain.Interfaces.Services;
using PaymentGateway.Infrastructure.HttpClients;
using PaymentGateway.Infrastructure.Repository;

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
builder.Services.AddAutoMapper(x => x.AddProfile(new PaymentMappingProfile()));

builder.Services.AddSingleton<IPaymentRepository, PaymentRepository>();
builder.Services.AddTransient<IPaymentUseCase, PaymentUseCase>();
builder.Services.AddTransient<IAcquiringBankService, AcquiringBankService>();
builder.Services.AddHttpClient<IAcquiringBankService, AcquiringBankService>(c =>
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
