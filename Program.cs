using RedisDemo.Repositories;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connectors.Redis;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Info;

var builder = WebApplication.CreateBuilder(args);

// --- Steeltoe Cloud Foundry Config Provider ---
// Reads VCAP_APPLICATION and VCAP_SERVICES into configuration on TAS.
// For this Demo, this has been commented out. Not needed here unless you need to access
// the config separately. AddRedis() will access and pull down vcap::services credentials
// and populate connectionString.
//builder.AddCloudFoundryConfiguration();

// --- Steeltoe v4 Redis Connector ---
builder.AddRedis();

// Register our repository
builder.Services.AddSingleton<ItemRepository>();

// Controllers
builder.Services.AddControllers();

// Steeltoe v4 actuator endpoints — CloudFoundry actuator is required on TAS
builder.Services.AddCloudFoundryActuator();
builder.Services.AddHealthActuator();
builder.Services.AddInfoActuator();

var app = builder.Build();

app.MapControllers();

app.Run();
