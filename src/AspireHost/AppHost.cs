var builder = DistributedApplication.CreateBuilder(args);

// Add AuthService
var authDb = builder.AddSqlServer("sqlserver")
    .AddDatabase("AuthDb");

builder.AddProject<Projects.AuthService>("authservice")
    .WithHttpHealthCheck("/health")
    .WithReference(authDb)
    .WaitFor(authDb);

builder.Build().Run();
