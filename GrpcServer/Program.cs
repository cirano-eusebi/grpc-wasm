// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IdentityModel.Tokens.Jwt;
using grpc_server.GrpcServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682


// Add services to the container.
builder.Services.AddGrpc();

const string CorsPolicy = "AllowAll";
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, builder => builder
	.AllowAnyOrigin()
	.AllowAnyMethod()
	.AllowAnyHeader()
	.WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding")
));

// Add Authz/n
var authSection = builder.Configuration.GetSection("Authentication");
builder.Services.AddAuthentication(options =>
	{
		options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
		options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
	})
	.AddJwtBearer(options =>
	{
		options.Authority = authSection["Authority"];
		options.Audience = authSection["Audience"];
		options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
		{
			ValidateIssuerSigningKey = false,
			ValidIssuers = authSection.GetValue<string[]>("Issuers")
		};
		options.SecurityTokenValidators.Clear();
		options.SecurityTokenValidators.Add(new JwtSecurityTokenHandler
		{
			MapInboundClaims = false
		});
		options.TokenValidationParameters.NameClaimType = "name";
		options.TokenValidationParameters.RoleClaimType = "role";
	});

builder.Services.AddAuthorization();

// Add MemoryCache service
builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseRouting();
app.UseGrpcWeb();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
app.UseEndpoints(endpoints => endpoints
	.MapGrpcService<GreeterService>()
	.EnableGrpcWeb()
	.RequireCors(CorsPolicy)
);

// Configure Http/2
//app.MapGrpcService<GreeterService>().RequireCors(CorsPolicy);
//app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
