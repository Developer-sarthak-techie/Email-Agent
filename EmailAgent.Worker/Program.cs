using EmailAgent.Worker;
using EmailAgent.Worker.Models;
using EmailAgent.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<InboundSourceOptions>(
    builder.Configuration.GetSection(InboundSourceOptions.SectionName));
builder.Services.Configure<ZohoDeskOptions>(
    builder.Configuration.GetSection(ZohoDeskOptions.SectionName));

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.PostConfigure<EmailSettings>(settings =>
{
    settings.ApplyProviderDefaults();
});
builder.Services.Configure<ProcessingEngineOptions>(
    builder.Configuration.GetSection(ProcessingEngineOptions.SectionName));

builder.Services.AddSingleton<IImapConnectionThrottle, ImapConnectionThrottle>();

builder.Services.AddHttpClient();

builder.Services.AddScoped<IEmailReaderService, EmailReaderService>();
builder.Services.AddScoped<IEmailLabelService, EmailLabelService>();
builder.Services.AddScoped<IEmailLabelResolverService, EmailLabelResolverService>();
builder.Services.AddScoped<IEmailMover, EmailMover>();
builder.Services.AddScoped<IEmailDraftService, EmailDraftService>();
builder.Services.AddScoped<IEmailProcessingValidatorService, EmailProcessingValidatorService>();

builder.Services.AddSingleton<IZohoDeskTokenProvider, ZohoDeskTokenProvider>();
builder.Services.AddScoped<IZohoDeskClient, ZohoDeskClient>();
builder.Services.AddScoped<IDeskTicketReader, DeskTicketReader>();
builder.Services.AddScoped<IDeskTicketActionService, DeskTicketActionService>();
builder.Services.AddScoped<IDeskProcessingService, DeskProcessingService>();

builder.Services.AddSingleton<FintechIntentEngine>();
builder.Services.AddSingleton<FintechDraftGenerator>();
builder.Services.AddSingleton<IntentScorer>();

// Choose inbound source at runtime via appsettings.
builder.Services.AddHostedService(sp =>
{
    var source = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<InboundSourceOptions>>().Value;
    return source.Type.Equals("ZohoDesk", StringComparison.OrdinalIgnoreCase)
        ? ActivatorUtilities.CreateInstance<DeskWorker>(sp)
        : ActivatorUtilities.CreateInstance<Worker>(sp);
});

var host = builder.Build();
host.Run();