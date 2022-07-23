// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using grpc_wasm.Client;
using grpc_wasm.Grpc;
using Grpc.Net.Client.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("grpc_wasm.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
	.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

// Supply HttpClient instances that include access tokens when making requests to the server project
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("grpc_wasm.ServerAPI"));

builder.Services.AddApiAuthorization();

// Add Grpc
builder.Services.AddGrpcClient<Greeter.GreeterClient>(o =>
{
	o.Address = new Uri("https://localhost:7278");
}).ConfigurePrimaryHttpMessageHandler(() => new GrpcWebHandler(new HttpClientHandler()));

await builder.Build().RunAsync();
