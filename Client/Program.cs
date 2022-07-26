// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using grpc_wasm.Client;
using grpc_wasm.Grpc;
using Grpc.Net.Client.Web;
using grpc_wasm.GrpcClient;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("grpc_wasm.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
	.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

// Supply HttpClient instances that include access tokens when making requests to the server project
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("grpc_wasm.ServerAPI"));

builder.Services.AddApiAuthorization();

// Add Authenticated Grpc Server
var grpcSection = builder.Configuration.GetSection("Grpc-server");
builder.Services.AddScoped<UnauthorizedInterceptor>();
builder.Services.AddScoped<RedirectAuthorizationHandler>();
builder.Services.AddGrpcClient<Greeter.GreeterClient>(o =>
	{
		o.Address = new Uri(grpcSection["Url"]);
	})
	.AddInterceptor<UnauthorizedInterceptor>()
	.ConfigurePrimaryHttpMessageHandler(sp =>
	{
		var authorizationHandler = sp.GetRequiredService<RedirectAuthorizationHandler>().ConfigureHandler(
			innerHandler: new HttpClientHandler(),
			authorizedUrls: new[] { grpcSection["Url"] },
			scopes: new[] { "grpc-wasm.ServerAPI", "openid", "profile" },
			returnUrl: $"authentication/login"
		);
		return new GrpcWebHandler(GrpcWebMode.GrpcWeb, authorizationHandler);
	});

await builder.Build().RunAsync();
