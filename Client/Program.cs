﻿using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using grpc_wasm.Client;
using grpc_wasm.Grpc;
using Grpc.Net.Client.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddGrpcClient<Greeter.GreeterClient>(o =>
{
    o.Address = new Uri("https://localhost:7278");
}).ConfigurePrimaryHttpMessageHandler(() => new GrpcWebHandler(new HttpClientHandler()));

await builder.Build().RunAsync();
