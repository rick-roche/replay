#pragma warning disable ASPIRECOMPUTE003
#pragma warning disable ASPIRECERTIFICATES001

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("env");

var endpoint = builder.AddParameter("registry-endpoint");
var repository = builder.AddParameter("registry-repository");

var registry = builder.AddContainerRegistry("container-registry", endpoint, repository);

var server = builder.AddProject<Projects.RePlay_Server>("server")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithExternalHttpEndpoints()
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
