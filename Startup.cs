using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mobsites.Cosmos.Identity;
using PolarExpress3.Areas.Identity;
using PolarExpress3.Data;
using System;

namespace PolarExpress3
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
            /*services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();*/
            services.AddScoped(typeof(IGiftRegistry), typeof(GiftRegistry));
            services
                .AddCosmosStorageProvider(options =>
                {
                    //options.ConnectionString defaults to the default Azure Cosmos DB Emulator connection string, which is what is desired here for the sample.
                    options.ConnectionString = Configuration.GetValue<string>("Cosmos:ConnectionString");
                    options.CosmosClientOptions = new CosmosClientOptions
                    {
                        SerializerOptions = new CosmosSerializationOptions
                        {                            
                            IgnoreNullValues = false
                        }
                    };
                    options.DatabaseId = "PolarExpress";
                    options.ContainerProperties = new ContainerProperties
                    {
                        Id = "Auth",
                        //PartitionKeyPath defaults to "/PartitionKey", which is what is desired for the default setup.
                    };
                });

            // Add Cosmos Identity using the default storage provider and default identity models, passing in Identity options if any.
            services
                .AddDefaultCosmosIdentity<AppUser>(options =>
                {
                    // User settings
                    options.User.RequireUniqueEmail = true;

                    // Password settings
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = true;

                    // Lockout settings
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.MaxFailedAccessAttempts = 5;

                })
                // Add other IdentityBuilder methods.
                .AddDefaultUI()
                .AddDefaultTokenProviders();


            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<AppUser>>();
            services.AddSingleton<WeatherForecastService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())    
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
