// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using grpc_wasm.Server.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.Extensions.Options;

internal class RandomClaimsFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
	public RandomClaimsFactory(UserManager<ApplicationUser> userManager, IOptions<IdentityOptions> optionsAccessor) : base(userManager, optionsAccessor)
	{
	}

	internal static ApiAuthorizationOptions ExposeClaims(ApiAuthorizationOptions options)
	{
		// Add claims when asking the openid scope
		options.IdentityResources["openid"].UserClaims.Add("random");

		// Add claims to the access token
		options.ApiResources.Single().UserClaims.Add("random");

		return options;
	}

	protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
	{
		var id = await base.GenerateClaimsAsync(user);
		id.AddClaim(new Claim("random", Random.Shared.NextInt64().ToString()));

		return id;
	}
}
