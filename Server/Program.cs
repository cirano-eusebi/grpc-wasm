// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using grpc_wasm.Server.Data;
using grpc_wasm.Server.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Add Identity server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlite(connectionString)
	//options.UseInMemoryDatabase("Users")
);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, RandomClaimsFactory>();
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
	{
		options.SignIn.RequireConfirmedAccount = false;
		options.SignIn.RequireConfirmedEmail = false;
	})
	.AddClaimsPrincipalFactory<RandomClaimsFactory>()
	.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddIdentityServer()
	.AddApiAuthorization<ApplicationUser, ApplicationDbContext>(options => RandomClaimsFactory.ExposeClaims(options));

builder.Services.AddAuthentication()
	.AddGoogle(options =>
	{
		var googleConfig = builder.Configuration.GetSection("Authentication:Google");
		options.ClientId = googleConfig["ClientId"];
		options.ClientSecret = googleConfig["ClientSecret"];

		options.SaveTokens = true;
		options.Events.OnCreatingTicket = ctx =>
		{
			var tokens = ctx.Properties.GetTokens().ToList();
			tokens.Add(new AuthenticationToken()
			{
				Name = "TicketCreated",
				Value = DateTime.UtcNow.ToString()
			});
			ctx.Properties.StoreTokens(tokens);

			return Task.CompletedTask;
		};
	})
	.AddIdentityServerJwt();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
	app.UseWebAssemblyDebugging();
}
else
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
