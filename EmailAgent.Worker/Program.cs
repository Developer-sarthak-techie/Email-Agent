using EmailAgent.Worker;
using EmailAgent.Worker.Models;
using EmailAgent.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IEmailReaderService, EmailReaderService>();
builder.Services.AddScoped<IEmailLabelService, EmailLabelService>();
builder.Services.AddScoped<IEmailMover, EmailMover>();
builder.Services.AddSingleton<IntentScorer>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();