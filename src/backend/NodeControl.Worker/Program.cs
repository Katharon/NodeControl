using NodeControl.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddNodeControlWorker(builder.Configuration);

var host = builder.Build();
host.Run();
