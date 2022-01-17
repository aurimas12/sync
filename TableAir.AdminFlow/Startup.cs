using MatBlazor;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TableAir.AdminFlow.Model;

namespace TableAir.AdminFlow
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddMatBlazor();

            services.AddHttpContextAccessor();
            services.AddAuthorizationCore();
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultAuthenticateScheme =
                    CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultSignInScheme =
                    CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme =
                    OpenIdConnectDefaults.AuthenticationScheme;
            })
                .AddCookie()
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Configuration = new OpenIdConnectConfiguration
                    {
                        // prod
                        // AuthorizationEndpoint = "https://api.tableair.lt/o/authorize/",
                        // TokenEndpoint = "https://api.tableair.lt/o/token/",
                        // UserInfoEndpoint = "https://api.tableair.lt/o/userinfo",
                        // Issuer = "https://api.tableair.lt/o",

                        // dev
                        AuthorizationEndpoint = "http://api.tableair.org/o/authorize/",
                        TokenEndpoint = "http://api.tableair.org/o/token/",
                        UserInfoEndpoint = "http://api.tableair.org/o/userinfo/",
                        Issuer = "http://api.tableair.org/o/",
                    };
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new JsonWebKeySet(@"
{
keys: [
{
alg: ""RS256"",
use: ""sig"",
kid: ""Bn0nbuiIBi8Gw-OIP-b7b8xL4S5nET7xdI77yA8Ef40"",
e: ""AQAB"",
kty: ""RSA"",
n: ""3Dzq5xtKNmH79W4JQ8nKeE-RegQ6OpZqTG7bfWHkafBVgz9WE8YFozGGKc9k1RaGzkTuS02IyFNLdoHYUOV1j-57Y-q_rQnQ4PHYeJ9MoMLgv2RKjMfzaz8sWe0jGDnsM-J7lhyMXMRYM-mJlGMeDNQdWC3iuul86x5w6M3V5Ij713O4a_ZJ9myRxeQ8mXMyHkLGbjUhPdYV_hbjKVWzR8sutF4CD7ev3f4y6eLkX4ok5QI6fT3ATMZ51-ppE11SL9OhbstkzFp0f5Rc5qHXljr2p-VrsCP_s0gt-04dr5dIm1JlB0BcL3bRzhDZCogMAatNBHk5m9haSeOVYpHLfQ""
}]}").Keys.First(),
                        ValidateAudience = false,
                        ValidateIssuer = false,
                    };
                    
                    options.ClientId = "YrvOOOi6LpakcgFJZ3IuLMvuuzBfdbAn6pC5Hvvx";//dev
                    // options.ClientId = "FZaWN7TnILftKTLUC6vbvenxaoLjrodVxpduT3MM";//prod
                    options.ClientSecret = "s1lIItbKYPxxncPMApP6cwb2QBV5t3HDx6JDB4xjWOtBQ1LfjzmDyZLzivZXtnrX4UVCNUm51cyLup5B3kZZ6ECAn9rxHUUNAQMlA46hahrPd6KwvJrZYTxoQh9gqH1B"; //dev
                    // options.ClientSecret = "0QcVeIod1NC8ctIgbeRzFO9geVoIoOf1OasMZSzIOEWm3Qwa9eDO1b44dqhuCF1dYSSTSC14900K1FPjkYHTDDul7asStg1Z9wkcVJs75zr2NbeJ9y5B9qAhE2xGaLiF";//prod
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.UseTokenLifetime = false;
                    options.RequireHttpsMetadata = false;

                    options.Scope.Clear();
                    // options.Scope.Add(MicrosoftSync.TeamUrl); //TODO
                    options.Scope.Add("openid");

                    //options.Scope.Add("profile");
                    options.GetClaimsFromUserInfoEndpoint = true;

                    options.SaveTokens = true;

                    options.Events = new OpenIdConnectEvents
                    {
                        OnRemoteFailure = context => {
                            
                            context.Response.Redirect($"/{MicrosoftSync.TeamUrl}/Error");
                            context.HandleResponse();
                            System.Console.WriteLine(context);
                            return Task.FromResult(0);
                        },
                    };
                });

// //             // Server Side Blazor doesn't register HttpClient by default
            if (!services.Any(x => x.ServiceType == typeof(HttpClient)))
            {
                // Setup HttpClient for server side in a client side compatible fashion
                services.AddScoped(s =>
                {
                    // Creating the URI helper needs to wait until the JS Runtime is initialized, so defer it.
                    var uriHelper = s.GetRequiredService<NavigationManager>();
                    return new HttpClient
                    {
                        BaseAddress = new Uri(uriHelper.BaseUri)
                    };
                });
            }

            MicrosoftSync.Start();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.

        public static class DotEnv
        {
            public static void Load(string filePath)
            {
                if (!File.Exists(filePath))
                    return;

                foreach (var line in File.ReadAllLines(filePath))
                {
                    var parts = line.Split(
                        '=',
                        StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 2)
                        continue;
                    Environment.SetEnvironmentVariable(parts[0], parts[1]);
                }
            }
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");
            DotEnv.Load(dotenv);

            // System.Console.WriteLine("1 " +Environment.GetEnvironmentVariable("TEAM_ID"));

            
            if (env.IsDevelopment())
            {
                System.Console.WriteLine(env.EnvironmentName);
                MicrosoftSync.TeamId=Convert.ToInt32(Environment.GetEnvironmentVariable("TEAM_ID"));
                app.UseDeveloperExceptionPage();
            }

            else if (env.IsStaging())
            {
                System.Console.WriteLine(env.EnvironmentName);
                app.UseDeveloperExceptionPage();
            }

            else if (env.IsProduction())
            {
                System.Console.WriteLine(env.EnvironmentName);
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UsePathBase($"/{MicrosoftSync.TeamUrl}");
            app.UseForwardedHeaders();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();
            // env
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
