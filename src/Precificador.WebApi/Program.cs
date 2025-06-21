using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Precificador.Application.Services;
using Precificador.Domain.Repository;
using Precificador.Infrastructure.Data;
using Precificador.Infrastructure.Repository;
using Serilog;
using Serilog.Sinks.Http;

namespace Precificador.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureDatabase(builder);
            ConfigureRepositories(builder);
            ConfigureApplicationServices(builder);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            ConfigureSwagger(builder);
            ConfigureLogging(builder);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            var newRelicApiKey = builder.Configuration.GetValue<string>("NewRelicApiKey");
            var newRelicApiName = builder.Configuration.GetValue<string>("NewRelicApiName");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.DurableHttpUsingFileSizeRolledBuffers(
                    requestUri: "https://log-api.newrelic.com/log/v1",
                    textFormatter: new Serilog.Formatting.Json.JsonFormatter(),
                    batchSizeLimitBytes: 1000000,
                    period: TimeSpan.FromSeconds(10),
                    bufferFileSizeLimitBytes: 10000000,
                    bufferBaseFileName: "./logs/buffer",
                    httpClient: new Serilog.Sinks.Http.HttpClients.JsonHttpClient()).CreateLogger();

            builder.Services.AddLogging();
        }

        private static void ConfigureSwagger(WebApplicationBuilder builder)
        {
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Precificador WebAPI", Version = "v1" });
            });
        }

        private static void ConfigureApplicationServices(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IColecaoService, ColecaoService>();
            builder.Services.AddScoped<IColecaoProdutoService, ColecaoProdutoService>();
            builder.Services.AddScoped<IGrupoService, GrupoService>();
            builder.Services.AddScoped<IMateriaPrimaService, MateriaPrimaService>();
            builder.Services.AddScoped<IPesquisaPrecoService, PesquisaPrecoService>();
            builder.Services.AddScoped<IProdutoMateriaPrimaService, ProdutoMateriaPrimaService>();
            builder.Services.AddScoped<IProdutoService, ProdutoService>();
            builder.Services.AddScoped<IUnidadeMedidaService, UnidadeMedidaService>();
        }

        private static void ConfigureRepositories(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IColecaoRepository, ColecaoRepository>();
            builder.Services.AddScoped<IColecaoProdutoRepository, ColecaoProdutoRepository>();
            builder.Services.AddScoped<IGrupoRepository, GrupoRepository>();
            builder.Services.AddScoped<IMateriaPrimaRepository, MateriaPrimaRepository>();
            builder.Services.AddScoped<IPesquisaPrecoRepository, PesquisaPrecoRepository>();
            builder.Services.AddScoped<IProdutoMateriaPrimaRepository, ProdutoMateriaPrimaRepository>();
            builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
            builder.Services.AddScoped<IUnidadeMedidaRepository, UnidadeMedidaRepository>();
        }

        private static void ConfigureDatabase(WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
        }
    }
}