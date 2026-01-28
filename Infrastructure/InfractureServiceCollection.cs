using Application.Abstractions;
using Infrastructure.Ai;
using Infrastructure.CvText;
using Infrastructure.Github;
using Infrastructure.LinkedIn;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
        {
            services.AddDbContext<AppDbContext>(o =>
                o.UseSqlite(cfg.GetConnectionString("Sqlite")));

            services.AddScoped<ICandidateRepository, CandidateRepository>();

            services.AddSingleton<CvTextExtractor>();

            // ✅ Ollama (FIXED): use named client + factory registration
            services.AddHttpClient("Ollama", c =>
            {
                var baseUrl = cfg["Ollama:BaseUrl"] ?? "http://localhost:11434";
                c.BaseAddress = new Uri(baseUrl);
            });

            services.AddScoped<IAiCvExtractor>(sp =>
            {
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var http = httpFactory.CreateClient("Ollama");

                var model = cfg["Ollama:Model"] ?? "gpt-oss:120b-cloud";
                return new OllamaAiCvExtractor(http, model);
            });

            // GitHub
            services.AddHttpClient<IGithubScanner, GithubScanner>(c =>
            {
                c.BaseAddress = new Uri("https://api.github.com");
            });

            services.AddScoped<ILinkedInProfileService, LinkedInProfileService>();

            return services;
        }
    }
}
