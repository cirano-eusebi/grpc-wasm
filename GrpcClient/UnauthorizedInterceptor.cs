// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace grpc_wasm.GrpcClient;

public class UnauthorizedInterceptor : Interceptor
{
	private readonly ILogger<UnauthorizedInterceptor> _logger;

	public UnauthorizedInterceptor(ILogger<UnauthorizedInterceptor> logger)
	{
		_logger = logger;
	}

	public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
		AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
	{
		_logger.LogInformation("In the interceptor");
		var call = continuation(request, context);

		return new AsyncUnaryCall<TResponse>(
			HandleResponse(call.ResponseAsync, context),
			call.ResponseHeadersAsync,
			call.GetStatus,
			call.GetTrailers,
			call.Dispose);
	}

	private async Task<TResponse> HandleResponse<TRequest, TResponse>(Task<TResponse> inner, ClientInterceptorContext<TRequest, TResponse> context)
		where TResponse : class
		where TRequest : class
	{
		try
		{
			return await inner;
		}
		catch (RpcException ex)
		{
			// Only handle unauthenticated errors
			if (ex.StatusCode == StatusCode.Unauthenticated)
			{
				_logger.LogError(ex, $"Unauthenticated Error thrown by {context.Method}");
#pragma warning disable CS8603 // Handler needs to return null in order to be considered a failed request
				return null;
#pragma warning restore CS8603
			}
			throw;
		}
	}
}
