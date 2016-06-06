using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Repro.Data;
using Repro.Models;
using Repro.Services;

namespace Repro
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();

                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddEntityFramework().AddDbContext<ApplicationDbContext>(options =>
                 options.UseInMemoryDatabase(o => o.IgnoreTransactions()));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddMvc();

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();
        }


        private void Seed(IApplicationBuilder app)
        {            
            var db = app.ApplicationServices.GetService<ApplicationDbContext>();
            db.Books.Add(new Book { Name = "eBook - Agricultural, Biological, and Food Sciences 2016" });
            db.Books.Add(new Book { Name = "eBook - Biochemistry, Genetics and Molecular Biology 2016" });
            db.Books.Add(new Book { Name = "eBook - Biomedical Science and Medicine 2016" });
            db.Books.Add(new Book { Name = "eBook - Chemical Engineering 2016" });
            db.Books.Add(new Book { Name = "eBook - Chemistry 2016" });
            db.Books.Add(new Book { Name = "eBook - Computer Science 2016" });
            db.Books.Add(new Book { Name = "eBook - Earth and Planetary Sciences 2016" });
            db.Books.Add(new Book { Name = "eBook - Energy 2016" });
            db.Books.Add(new Book { Name = "eBook - Engineering 2016" });
            db.Books.Add(new Book { Name = "eBook - Environmental Science 2016" });
            db.Books.Add(new Book { Name = "eBook - Finance 2016" });
            db.Books.Add(new Book { Name = "eBook - Forensics and Security 2016" });
            db.Books.Add(new Book { Name = "eBook - Health Professions 2016" });
            db.Books.Add(new Book { Name = "eBook - Immunology and Microbiology 2016" });
            db.Books.Add(new Book { Name = "eBook - Materials Science 2016" });
            db.Books.Add(new Book { Name = "eBook - Mathematics 2016" });
            db.Books.Add(new Book { Name = "eBook - Neuroscience 2016" });
            db.Books.Add(new Book { Name = "eBook - Pharmacology, Toxicology and Pharmaceutical Science 2016" });
            db.Books.Add(new Book { Name = "eBook - Physics and Astronomy 2016" });
            db.Books.Add(new Book { Name = "eBook - Psychology 2016" });
            db.Books.Add(new Book { Name = "eBook - Social Sciences 2016" });
            db.Books.Add(new Book { Name = "eBook - Specialty Medicine 2016" });
            db.Books.Add(new Book { Name = "eBook - Veterinary Medicine 2016" });
            db.SaveChanges();

        }    
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseApplicationInsightsRequestTelemetry();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseApplicationInsightsExceptionTelemetry();

            app.UseStaticFiles();

            app.UseIdentity();

            // Add external authentication middleware below. To configure them please see http://go.microsoft.com/fwlink/?LinkID=532715

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            Seed(app);
        }
    }
}
