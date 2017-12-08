using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Miniblog.Core.Services;
using System;
using WebEssentials.AspNetCore.OutputCaching;
using WebEssentials.AspNetCore.Pwa;
using WebMarkupMin.AspNetCore2;
using WebMarkupMin.Core;
using WilderMinds.MetaWeblog;
using Newtonsoft.Json;

using IWmmLogger = WebMarkupMin.Core.Loggers.ILogger;
using MetaWeblogService = Miniblog.Core.Services.MetaWeblogService;
using WmmNullLogger = WebMarkupMin.Core.Loggers.NullLogger;
using System.Linq;

namespace Miniblog.Core
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel(a => a.AddServerHeader = false)
                .ConfigureAppConfiguration((bc, builder) => {
                    builder.AddEnvironmentVariables();
                })
                .Build();

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            var rawConfigSettings = Configuration.Get<BlogSettings>();
            if (rawConfigSettings != null)
            {
                var blogSettingsOptions = new OptionsWrapper<BlogSettings>(rawConfigSettings);
                services.AddSingleton<IOptionsSnapshot<BlogSettings>>(new FixOptionsSnapshot<BlogSettings>(rawConfigSettings));
            }
            else
            {
                services.Configure<BlogSettings>(Configuration.GetSection("blog"));
            }

            // Progressive Web Apps https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker
            services.AddProgressiveWebApp(new WebEssentials.AspNetCore.Pwa.PwaOptions
            {
                OfflineRoute = "/shared/offline/"
            });

            if (TryGetWebManifestFromConfiguration(out WebManifest dynamicWebManifest))
            {
                var existingDI = services.FirstOrDefault(x => x.ServiceType == typeof(WebManifest));
                if (existingDI != null)
                    services.Remove(existingDI);

                services.AddScoped(sp => dynamicWebManifest);
            }
  
                
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMetaWeblog<MetaWeblogService>();
            services.AddScoped<IUserServices, BlogUserServices>();
            services.AddSingleton<IBlogService, FileBlogService>();

            // Output caching (https://github.com/madskristensen/WebEssentials.AspNetCore.OutputCaching)
            services.AddOutputCaching(options =>
            {
                options.Profiles["default"] = new OutputCacheProfile
                {
                    Duration = 3600
                };
            });

            // Cookie authentication.
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login/";
                    options.LogoutPath = "/logout/";
                });

            // HTML minification (https://github.com/Taritsyn/WebMarkupMin)
            services
                .AddWebMarkupMin(options =>
                {
                    options.AllowMinificationInDevelopmentEnvironment = true;
                    options.DisablePoweredByHttpHeaders = true;
                })
                .AddHtmlMinification(options =>
                {
                    options.MinificationSettings.RemoveOptionalEndTags = false;
                    options.MinificationSettings.WhitespaceMinificationMode = WhitespaceMinificationMode.Safe;
                });
            services.AddSingleton<IWmmLogger, WmmNullLogger>(); // Used by HTML minifier

            // Bundling, minification and Sass transpilation (https://github.com/ligershark/WebOptimizer)
            services.AddWebOptimizer(pipeline =>
            {
                pipeline.MinifyJsFiles();
                pipeline.CompileScssFiles()
                        .InlineImages(1);
            });
        }

        /// <summary>
        /// Tries to create the <see cref="WebManifest"/> from configuration values
        /// </summary>
        /// <returns><c>true</c>, if get web manifest was created, <c>false</c> otherwise.</returns>
        /// <param name="dynamicWebManifest">Dynamic web manifest.</param>
        bool TryGetWebManifestFromConfiguration(out WebManifest dynamicWebManifest)
        {
            dynamicWebManifest = null;

            WebManifest webManifest = new WebManifest()
            {
                Name = Configuration["blog_name"] ?? Configuration["blog:name"],
                Description = Configuration["blog_description"] ?? Configuration["blog:description"],
                ShortName = Configuration["blog_shortname"] ?? Configuration["blog:shortname"],
                BackgroundColor = "#fff",
                ThemeColor = "#fff",
                StartUrl = "/",
                Display = "standalone",
                Icons = new Icon[]
                    {
                        new Icon()
                        {
                            Src = "/img/icon192x192.png",
                            Sizes = "192x192"
                        },
                        new Icon()
                        {
                            Src = "/img/icon512x512.png",
                            Sizes = "512x512"
                        }
                    }
            };

            if (webManifest.IsValid(out var webManifestError))
            {
                // Do not send 'null' properties as they generate warnings on Chrome dev tools
                var rawJson = JsonConvert.SerializeObject(webManifest, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

                // Using reflection because the property setter for "RawJson" is internal
                webManifest.GetType().GetProperty("RawJson").SetValue(webManifest, rawJson);

                dynamicWebManifest = webManifest;

                return true;
            }

            return false;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
            }

            app.Use((context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                if (context.Request.IsHttps)
                {
                    context.Response.Headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains";
                }
                return next();
            });

            app.UseStatusCodePagesWithReExecute("/Shared/Error");
            app.UseWebOptimizer();

            app.UseStaticFilesWithCache();

            if (Configuration.GetValue<bool>("forcessl"))
            {
                app.UseRewriter(new RewriteOptions().AddRedirectToHttps());
            }

            app.UseMetaWeblog("/metaweblog");
            app.UseAuthentication();

            app.UseOutputCaching();
            app.UseWebMarkupMin();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Blog}/{action=Index}/{id?}");
            });
        }
    }
}
