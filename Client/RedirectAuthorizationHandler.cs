// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace grpc_wasm.Client;

public class RedirectAuthorizationHandler : AuthorizationMessageHandler
{
	private readonly NavigationManager _navigation;
	private string? _returnUrl;
	public RedirectAuthorizationHandler(IAccessTokenProvider provider, NavigationManager navigation) : base(provider, navigation)
	{
		_navigation = navigation;
	}

	public RedirectAuthorizationHandler ConfigureHandler(HttpMessageHandler innerHandler,
			IEnumerable<string> authorizedUrls,
			IEnumerable<string>? scopes = null,
			string? returnUrl = null)
	{
		_returnUrl = returnUrl;
		InnerHandler = innerHandler;
		base.ConfigureHandler(authorizedUrls, scopes, returnUrl);
		return this;
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		try
		{
			return await base.SendAsync(request, cancellationToken);
		}
		catch (AccessTokenNotAvailableException)
		{
			var currentPage = _navigation.Uri;
			_navigation.NavigateTo($"{_returnUrl}?returnUrl={UrlEncoder.Default.Encode(currentPage)}");
			return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
		}
	}
}
