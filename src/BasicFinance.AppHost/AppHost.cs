using Aspire.Hosting.JavaScript;
using BasicFinance.SharedServiceDefaults;
using Microsoft.Extensions.DependencyInjection;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Configure database for application
IResourceBuilder<ParameterResource> basicFinanceDbUsername = builder.AddParameter("basicfinance-db-username", secret: true);
IResourceBuilder<ParameterResource> basicFinanceDbPassword = builder.AddParameter("basicfinance-db-password", secret: true);
IResourceBuilder<PostgresDatabaseResource> basicFinanceDbServer = builder.AddPostgres(
    ServiceDiscoveryNames.BasicFinanceDb,
    basicFinanceDbUsername,
    basicFinanceDbPassword)
    .WithChildRelationship(basicFinanceDbUsername)
    .WithChildRelationship(basicFinanceDbPassword)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("basicFinanceDb");

// Configure database for Identity Management
// Todo: Move to seperate repo so that additional repos/projects can use instance
IResourceBuilder<ParameterResource> keycloakDbUsername = builder.AddParameter("keycloak-db-username", secret: true);
IResourceBuilder<ParameterResource> keycloakDbPassword = builder.AddParameter("keycloak-db-password", secret: true);
IResourceBuilder<PostgresDatabaseResource> keycloakDbServer = builder.AddPostgres(
    "keycloakdbserver",
    keycloakDbUsername,
    keycloakDbPassword)
    .WithChildRelationship(keycloakDbUsername)
    .WithChildRelationship(keycloakDbPassword)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("keycloakDb");

IResourceBuilder<ParameterResource> keycloakAdminUsername = builder.AddParameter("keycloak-admin-username", secret: true);
IResourceBuilder<ParameterResource> keycloakAdminPassword = builder.AddParameter("keycloak-admin-password", secret: true);
IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak(
    ServiceDiscoveryNames.Keycloak,
    8080,
    keycloakAdminUsername,
    keycloakAdminPassword)
    .WithChildRelationship(keycloakAdminUsername)
    .WithChildRelationship(keycloakAdminPassword)
    .WithChildRelationship(keycloakDbServer)
    .WithDataVolume()
    .WithPostgres(keycloakDbServer)
    .WithExternalHttpEndpoints();

// Configure RabbitMq server to handle message queue functionality 
// Todo: Move to seperate repo so that additional repos/projects can use the same instance
IResourceBuilder<ParameterResource> rabbitmqAdminUsername = builder.AddParameter("rabbitmq-admin-username", secret: true);
IResourceBuilder<ParameterResource> rabbitmqAdminPassword = builder.AddParameter("rabbitmq-admin-password", secret: true);
IResourceBuilder<RabbitMQServerResource> rabbitmq = builder.AddRabbitMQ(ServiceDiscoveryNames.RabbitMq, rabbitmqAdminUsername, rabbitmqAdminPassword)
    .WithChildRelationship(rabbitmqAdminUsername)
    .WithChildRelationship(rabbitmqAdminPassword)
    .WithDataVolume()
    .WithManagementPlugin();

IResourceBuilder<ProjectResource> api = builder.AddProject<Projects.BasicFinance_Api>("api")
    .WithReference(keycloak)
    .WithReference(basicFinanceDbServer)
    .WithReference(rabbitmq)
    .WaitFor(keycloak)
    .WaitFor(basicFinanceDbServer)
    .WaitFor(rabbitmq)
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

IResourceBuilder<ProjectResource> dataProcessorService = builder.AddProject<Projects.BasicFinance_DataProcessor>("dataProcessor")
    .WithReference(basicFinanceDbServer)
    .WithReference(rabbitmq)
    .WaitFor(basicFinanceDbServer)
    .WaitFor(rabbitmq)
    .PublishAsDockerFile();

IResourceBuilder<JavaScriptAppResource> client = builder.AddJavaScriptApp("client", "../BasicFinance.Client", "start")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT", port: 4200)
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

//IResourceBuilder<YarpResource> gateway = builder.AddYarp("gateway")
//    .WaitFor(client)
//    .WaitFor(dataProcessorService)
//    .WithConfiguration(yarp =>
//    {
//        // Add catch-all route for frontend service 
//        yarp.AddRoute(client);

//        // Add specific path route with transforms
//        yarp.AddRoute("/api/{**catch-all}", api);
//    })
//    .WithExternalHttpEndpoints();

builder.Build().Run();
