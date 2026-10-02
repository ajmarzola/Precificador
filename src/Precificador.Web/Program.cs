using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Precificador.Web.Autenticacao;
using Precificador.Web.Email;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Empresas;
using Precificador.Web.Autorizacao;
using Precificador.Web.Precificacao;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Insumos", NomesAutorizacao.EmpresaAtiva);
    options.Conventions.AuthorizeFolder("/Produtos", NomesAutorizacao.EmpresaAtiva);
    options.Conventions.AuthorizeFolder("/Configuracoes", NomesAutorizacao.EmpresaAtiva);
    options.Conventions.AuthorizeFolder("/Dashboard", NomesAutorizacao.EmpresaAtiva);
    options.Conventions.AuthorizeFolder("/Admin", NomesAutorizacao.SystemAdmin);
    options.Conventions.AuthorizeFolder("/Usuarios", NomesAutorizacao.AdministradorEmpresa);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddScoped<EmpresaContext>();
builder.Services.AddScoped<IEmpresaContext>(provider => provider.GetRequiredService<EmpresaContext>());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IDataOperacionalEmpresa, DataOperacionalEmpresa>();
builder.Services.AddScoped<PrecificacaoProdutoAtual>();
builder.Services.AddScoped<ResumoPrecificacaoProdutosAtual>();
builder.Services.AddDbContext<PrecificadorDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Precificador"),
        sql => sql.EnableRetryOnFailure()));
builder.Services.AddIdentity<UsuarioAplicacao, IdentityRole>(options =>
{
    options.Tokens.PasswordResetTokenProvider = TokensConta.Recuperacao;
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
}).AddEntityFrameworkStores<PrecificadorDbContext>().AddDefaultTokenProviders()
    .AddTokenProvider<TokenAtivacaoProvider>(TokensConta.Ativacao)
    .AddTokenProvider<TokenRecuperacaoProvider>(TokensConta.Recuperacao);
builder.Services.AddDataProtection().SetApplicationName("Precificador").PersistKeysToDbContext<PrecificadorDbContext>();
builder.Services.AddSingleton<IValidateOptions<OpcoesSmtp>, ValidacaoSmtp>();
builder.Services.AddSingleton<IValidateOptions<OpcoesAplicacao>, ValidacaoAplicacao>();
builder.Services.AddOptions<OpcoesSmtp>().BindConfiguration("Email:Smtp").ValidateOnStart();
builder.Services.AddOptions<OpcoesAplicacao>().BindConfiguration("Aplicacao")
    .PostConfigure(options => options.UrlPublica = options.UrlPublica.TrimEnd('/')).ValidateOnStart();
builder.Services.AddScoped<IEmailSenderAplicacao, SmtpEmailSender>();
builder.Services.AddScoped<ServicoConta>();
builder.Services.AddScoped<Precificador.Web.Administracao.ServicoAdministracao>();
builder.Services.AddScoped<Precificador.Web.Administracao.ServicoUsuariosEmpresa>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.LoginPath = "/Conta/Login";
    options.AccessDeniedPath = "/Conta/Login";
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(NomesAutorizacao.EmpresaAtiva, policy => policy.Requirements.Add(new EmpresaAtivaRequirement()));
    options.AddPolicy(NomesAutorizacao.SystemAdmin, policy => policy.RequireRole(NomesAutorizacao.SystemAdmin));
    options.AddPolicy(NomesAutorizacao.AdministradorEmpresa, policy => policy.Requirements.Add(new AdministradorEmpresaRequirement()));
});
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, EmpresaAtivaHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, AdministradorEmpresaHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
    await context.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Erro/500");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseWhen(Precificador.Web.Erros.NavegacaoHtml.AceitaHtml, branch =>
    branch.UseStatusCodePagesWithReExecute("/Erro/{0}"));

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program;
