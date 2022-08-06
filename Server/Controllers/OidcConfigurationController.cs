﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Mvc;

namespace grpc_wasm.Server.Controllers
{
	public class OidcConfigurationController : Controller
	{
		private readonly ILogger<OidcConfigurationController> _logger;

		public OidcConfigurationController(IClientRequestParametersProvider clientRequestParametersProvider, ILogger<OidcConfigurationController> logger)
		{
			ClientRequestParametersProvider = clientRequestParametersProvider;
			_logger = logger;
		}

		public IClientRequestParametersProvider ClientRequestParametersProvider { get; }

		[HttpGet("_configuration/{clientId}")]
		public IActionResult GetClientRequestParameters([FromRoute] string clientId)
		{
			var parameters = ClientRequestParametersProvider.GetClientParameters(HttpContext, clientId);
			return Ok(parameters);
		}
	}
}