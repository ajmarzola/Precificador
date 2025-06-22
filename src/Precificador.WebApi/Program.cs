using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Trace;

using Precificador.Application.Services;
using Precificador.Domain.Repository;
using Precificador.Infrastructure.Data;
using Precificador.Infrastructure.Repository;
using Precificador.WebApi.Infra;

using OpenTelemetry;
using OpenTelemetry.Exporter;

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

            builder.Services.AddCors();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            if (!app.Environment.IsEnvironment("Docker"))
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }

        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            builder.Services.AddCorrelationIdGenerator();
            builder.Services.AddTransient(typeof(BaseLogger<>));

            builder.Services.AddOpenTelemetry().WithTracing(tracing =>
            {
                var endpoint = builder.Configuration["OTLP_ENDPOINT"] ?? string.Empty;

                tracing.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddSqlClientInstrumentation()
                       .AddProcessor(new SimpleActivityExportProcessor(new OtlpTraceExporter(new OtlpExporterOptions
                       {
                           Endpoint = new Uri(endpoint),
                           Headers = builder.Configuration["OTLP_HEADERS"]
                       })));
            });
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
            builder.Services.AddTransient<IColecaoService, ColecaoService>();
            builder.Services.AddTransient<IColecaoProdutoService, ColecaoProdutoService>();
            builder.Services.AddTransient<IGrupoService, GrupoService>();
            builder.Services.AddTransient<IMateriaPrimaService, MateriaPrimaService>();
            builder.Services.AddTransient<IPesquisaPrecoService, PesquisaPrecoService>();
            builder.Services.AddTransient<IProdutoMateriaPrimaService, ProdutoMateriaPrimaService>();
            builder.Services.AddTransient<IProdutoService, ProdutoService>();
            builder.Services.AddTransient<IUnidadeMedidaService, UnidadeMedidaService>();
        }

        private static void ConfigureRepositories(WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<IColecaoRepository, ColecaoRepository>();
            builder.Services.AddTransient<IColecaoProdutoRepository, ColecaoProdutoRepository>();
            builder.Services.AddTransient<IGrupoRepository, GrupoRepository>();
            builder.Services.AddTransient<IMateriaPrimaRepository, MateriaPrimaRepository>();
            builder.Services.AddTransient<IPesquisaPrecoRepository, PesquisaPrecoRepository>();
            builder.Services.AddTransient<IProdutoMateriaPrimaRepository, ProdutoMateriaPrimaRepository>();
            builder.Services.AddTransient<IProdutoRepository, ProdutoRepository>();
            builder.Services.AddTransient<IUnidadeMedidaRepository, UnidadeMedidaRepository>();
        }

        private static void ConfigureDatabase(WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
        }
    }
}