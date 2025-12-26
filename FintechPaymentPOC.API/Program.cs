using FintechPaymentPOC.Application.Interfaces;
using FintechPaymentPOC.Application.Services;
using FintechPaymentPOC.Domain.Events;
using FintechPaymentPOC.Infrastructure.Data;
using FintechPaymentPOC.Infrastructure.Events;
using FintechPaymentPOC.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "FintechPaymentPOC API", 
        Version = "v1",
        Description = "A fintech payment processing POC using Clean Architecture"
    });
    // Only include XML comments if the file exists
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add logging
builder.Services.AddLogging();

// Configure Entity Framework Core with In-Memory Database
// Domain events will be dispatched automatically on SaveChangesAsync
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseInMemoryDatabase("FintechPaymentDb"));

// Register repositories
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// Register application services
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Register SWIFT MT103 → ISO 20022 conversion services
builder.Services.AddScoped<IMT103Parser, MT103Parser>();
builder.Services.AddScoped<IPaymentEnrichmentService, PaymentEnrichmentService>();
builder.Services.AddScoped<IPacs008Generator, Pacs008Generator>();
builder.Services.AddScoped<IPaymentToSwiftConverter, PaymentToSwiftConverter>();

// Register Domain Event Dispatcher
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

// Register domain event handlers - these will be automatically discovered and invoked
builder.Services.AddScoped<IDomainEventHandler<PaymentRequestedEvent>, PaymentEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<PaymentRequestedEvent>, NotificationEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<PaymentRequestedEvent>, AuditEventHandler>();

// Register event handlers (for backward compatibility if needed)
builder.Services.AddScoped<PaymentEventHandler>();
builder.Services.AddScoped<NotificationEventHandler>();
builder.Services.AddScoped<AuditEventHandler>();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        DatabaseSeeder.Seed(context);
        
        // Event handlers are automatically discovered and invoked when events are published
        // No manual subscription needed - handlers are registered in DI container
        Console.WriteLine("✅ Database seeded successfully!");
        Console.WriteLine("✅ Event handlers registered - will auto-invoke when events are published");
    }
    catch (ReflectionTypeLoadException ex)
    {
        Console.WriteLine($"ReflectionTypeLoadException occurred: {ex.Message}");
        if (ex.LoaderExceptions != null)
        {
            foreach (var loaderEx in ex.LoaderExceptions)
            {
                Console.WriteLine($"Loader Exception: {loaderEx?.Message}");
            }
        }
        throw;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during startup: {ex.Message}");
        throw;
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FintechPaymentPOC-API");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


app.Run();
