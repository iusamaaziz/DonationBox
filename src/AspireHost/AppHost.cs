var builder = DistributedApplication.CreateBuilder(args);

// Add AuthService
var sqlServer = builder.AddSqlServer("sql-server");

var authDb = sqlServer
    .AddDatabase("AuthDb");

var authService = builder.AddProject<Projects.AuthService>("authService")
    .WithReference(authDb)
    .WaitFor(authDb);

// Add OrganizationService
var orgDb = sqlServer
    .AddDatabase("OrganizationDb");

var organizationService = builder.AddProject<Projects.OrganizationService>("organizationService")
    .WithReference(orgDb)
    .WaitFor(orgDb)
    .WithReference(authService)
    .WaitFor(authService);

// Add DonationService
var donationDb = sqlServer
    .AddDatabase("DonationDb");

var donationService = builder.AddProject<Projects.DonationService>("donationService")
    .WithReference(donationDb)
    .WaitFor(donationDb)
    .WithReference(organizationService)
    .WaitFor(organizationService)
    .WithReference(authService)
    .WaitFor(authService);

// Add CampaignService
var campaignDb = sqlServer
    .AddDatabase("CampaignDb");

var campaignService = builder.AddProject<Projects.CampaignService>("campaignService")
    .WithReference(campaignDb)
    .WaitFor(campaignDb)
    .WithReference(authService)
    .WaitFor(authService);

// Add ApiGateway
var apiGateway = builder.AddProject<Projects.ApiGateway>("apiGateway")
    .WithReference(authService)
    .WaitFor(authService)
    .WithReference(organizationService)
    .WaitFor(organizationService)
    .WithReference(campaignService)
    .WaitFor(campaignService)
    .WithReference(donationService)
    .WaitFor(donationService);

builder.Build().Run();
