using BasicFinance.SharedServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

// Configure database for application
var basicFinanceDbUsername = builder.AddParameter("basicfinance-db-username", secret: true);
var basicFinanceDbPassword = builder.AddParameter("basicfinance-db-password", secret: true);
var basicFinanceDbServer = builder.AddPostgres(
    "basicfinancedbserver",
    basicFinanceDbUsername,
    basicFinanceDbPassword)
    .WithChildRelationship(basicFinanceDbUsername)
    .WithChildRelationship(basicFinanceDbPassword)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase(ServiceDiscoveryNames.BasicFinanceDb);

_ = builder.AddProject<Projects.BasicFinance_MigrationWorker>("migration")
       .WaitFor(basicFinanceDbServer)
       .WithReference(basicFinanceDbServer);

// Configure database for Identity Management
var keycloakDbUsername = builder.AddParameter("keycloak-db-username", secret: true);
var keycloakDbPassword = builder.AddParameter("keycloak-db-password", secret: true);
var keycloakDbServer = builder.AddPostgres(
    "keycloakdbserver",
    keycloakDbUsername,
    keycloakDbPassword)
    .WithChildRelationship(keycloakDbUsername)
    .WithChildRelationship(keycloakDbPassword)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("keycloakDb");

var keycloakAdminUsername = builder.AddParameter("keycloak-admin-username", secret: true);
var keycloakAdminPassword = builder.AddParameter("keycloak-admin-password", secret: true);
var keycloakGoogleClientId = builder.AddParameter("keycloak-google-clientid", secret: true);
var keycloakGoogleClientSecret = builder.AddParameter("keycloak-google-clientsecret", secret: true);
var keycloak = builder.AddKeycloak(
    ServiceDiscoveryNames.Keycloak,
    8080,
    keycloakAdminUsername,
    keycloakAdminPassword)
    .WithChildRelationship(keycloakAdminUsername)
    .WithChildRelationship(keycloakAdminPassword)
    .WithChildRelationship(keycloakGoogleClientId)
    .WithChildRelationship(keycloakGoogleClientSecret)
    .WithChildRelationship(keycloakDbServer)
    .WithEnvironment("KEYCLOAK-GOOGLE-CLIENTID", keycloakGoogleClientId)
    .WithEnvironment("KEYCLOAK-GOOGLE-CLIENTSECRET", keycloakGoogleClientSecret)
    .WithRealmImport("./Realms")
    .WithDataVolume()
    .WithPostgres(keycloakDbServer)
    .WithOtlpExporter()
    .WithExternalHttpEndpoints();

// Configure RabbitMq server to handle message queue functionality
var rabbitmqAdminUsername = builder.AddParameter("rabbitmq-admin-username", secret: true);
var rabbitmqAdminPassword = builder.AddParameter("rabbitmq-admin-password", secret: true);
var rabbitmq = builder.AddRabbitMQ(ServiceDiscoveryNames.RabbitMq, rabbitmqAdminUsername, rabbitmqAdminPassword)
    .WithChildRelationship(rabbitmqAdminUsername)
    .WithChildRelationship(rabbitmqAdminPassword)
    .WithDataVolume()
    .WithManagementPlugin();

var googleServiceAccountCredentialFile = builder.AddParameter("google-service-account-credential-file");
var api = builder.AddProject<Projects.BasicFinance_Api>("api")
    .WithChildRelationship(googleServiceAccountCredentialFile)
    .WithReference(keycloak)
    .WithReference(basicFinanceDbServer)
    .WithReference(rabbitmq)
    .WithReference(basicFinanceDbServer)
    .WithEnvironment("GOOGLE-APPLICATION-CREDENTIALS", googleServiceAccountCredentialFile)
    .WaitFor(basicFinanceDbServer)
    .WaitFor(keycloak)
    .WaitFor(basicFinanceDbServer)
    .WaitFor(rabbitmq)
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

_ = builder.AddProject<Projects.BasicFinance_DataProcessor>("dataProcessor")
    .WithChildRelationship(googleServiceAccountCredentialFile)
    .WithReference(basicFinanceDbServer)
    .WithReference(rabbitmq)
    .WaitFor(basicFinanceDbServer)
    .WithEnvironment("GOOGLE-APPLICATION-CREDENTIALS", googleServiceAccountCredentialFile)
    .WaitFor(rabbitmq)
    .PublishAsDockerFile();

var clientGoogleClientAuthority = builder.AddParameter("basicfinance-google-authority");
var clientGoogleClientId = builder.AddParameter("basicfinance-google-clientid");
_ = builder.AddJavaScriptApp("client", "../BasicFinance.Client", "start")
    .WithChildRelationship(clientGoogleClientAuthority)
    .WithChildRelationship(clientGoogleClientId)
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT", port: 4200)
    .WithEnvironment("BASICFINANCE-GOOGLE-AUTHORITY", clientGoogleClientAuthority)
    .WithEnvironment("BASICFINANCE-GOOGLE-CLIENTID", clientGoogleClientId)
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

var scalar = builder.AddScalarApiReference(options =>
{
    options.PreferHttpsEndpoint()
        .AddPreferredSecuritySchemes("Bearer")
        .AllowSelfSignedCertificates();
});

scalar.WithApiReference(api);

await builder.Build().RunAsync();