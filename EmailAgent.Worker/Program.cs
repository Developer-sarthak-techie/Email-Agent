using EmailAgent.Worker;
using EmailAgent.Worker.Models;
using EmailAgent.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<ProcessingEngineOptions>(
    builder.Configuration.GetSection(ProcessingEngineOptions.SectionName));

builder.Services.AddSingleton<IImapConnectionThrottle, ImapConnectionThrottle>();

builder.Services.AddScoped<IEmailReaderService, EmailReaderService>();
builder.Services.AddScoped<IEmailLabelService, EmailLabelService>();
builder.Services.AddScoped<IEmailLabelResolverService, EmailLabelResolverService>();
builder.Services.AddScoped<IEmailMover, EmailMover>();
builder.Services.AddScoped<IEmailDraftService, EmailDraftService>();
builder.Services.AddScoped<IEmailProcessingValidatorService, EmailProcessingValidatorService>();

builder.Services.AddSingleton<FintechIntentEngine>();
builder.Services.AddSingleton<FintechDraftGenerator>();
builder.Services.AddSingleton<IntentScorer>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();