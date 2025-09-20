var builder = DistributedApplication.CreateBuilder(args);

// Add AuthService
var sqlServer = builder.AddSqlServer("sql-server");

var authDb = sqlServer
    .AddDatabase("AuthDb");

var authService = builder.AddProject<Projects.AuthService>("authService")
    .WithHttpHealthCheck("/health")
    .WithReference(authDb)
    .WaitFor(authDb);

// Add OrganizationService
var orgDb = sqlServer
    .AddDatabase("OrganizationDb");

var organizationService = builder.AddProject<Projects.OrganizationService>("organizationService")
    //.WithHealthCheck("/health")
    .WithReference(orgDb)
    .WaitFor(orgDb)
    .WithReference(authService)
    .WaitFor(authService);

builder.Build().Run();
