// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using grpc_wasm.Grpc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace grpc_server.GrpcServer.Services;

[Authorize]
public class GreeterService : Greeter.GreeterBase
{
	public ILogger<GreeterService> Logger { get; }
	public IMemoryCache Cache { get; }

	public GreeterService(ILogger<GreeterService> logger, IMemoryCache cache)
	{
		Logger = logger;
		Cache = cache;
	}

	public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
	{
		return Task.FromResult(new HelloReply
		{
			Message = context.GetHttpContext().User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown - Server"
		});
	}

	public override Task<HelloReply> SayGoodbye(HelloRequest request, ServerCallContext context)
	{
		return Task.FromResult(new HelloReply
		{
			Message = "Goodbye"
		});
	}

	public override Task<CounterResponse> GetCounter(Empty request, ServerCallContext context)
	{
		return Task.FromResult(new CounterResponse
		{
			Counter = GetCounter()
		});
	}

	public override Task<CounterResponse> AddOne(Empty request, ServerCallContext context)
	{
		int current;
		lock (Cache)
		{
			current = GetCounter();
			current += 1;
			Cache.Set("grpc-counter", current);
		}
		return Task.FromResult(new CounterResponse { Counter = current });
	}

	public override async Task SubscribeToCounter(UpdateInterval request, IServerStreamWriter<CounterResponse> responseStream, ServerCallContext context)
	{
		var lastCount = 0;
		while (true)
		{
			var currentCount = GetCounter();
			if (currentCount != lastCount)
			{
				await responseStream.WriteAsync(new CounterResponse
				{
					Counter = currentCount
				});
				lastCount = currentCount;
			}

			await Task.Delay(request.Delay);
		}
	}

	private int GetCounter() => Cache.GetOrCreate("grpc-counter", k => 0);
}
