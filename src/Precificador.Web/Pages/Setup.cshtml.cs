using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Autorizacao;
using System.Security.Cryptography;
using System.Text;

namespace Precificador.Web.Pages;

public sealed class SetupModel(
    PrecificadorDbContext context,
    UserManager<UsuarioAplicacao> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public async Task<IActionResult> OnGetAsync()
    {
        if (await ExisteSystemAdminAsync()) return NotFound();
        return ChaveConfigurada() ? Page() : ConfiguracaoIndisponivel();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (await ExisteSystemAdminAsync()) return NotFound();
        if (!ChaveConfigurada()) return ConfiguracaoIndisponivel();
        if (!ModelState.IsValid) return Page();
        if (!ChaveValida())
        {
            ModelState.AddModelError(string.Empty, "A configuração inicial não pôde ser concluída.");
            return Page();
        }

        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (await ExisteSystemAdminAsync()) return NotFound();

            if (!await roleManager.RoleExistsAsync(NomesAutorizacao.SystemAdmin))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(NomesAutorizacao.SystemAdmin));
                if (!roleResult.Succeeded)
                {
                    foreach (var erro in roleResult.Errors) ModelState.AddModelError(string.Empty, erro.Description);
                    return Page();
                }
            }

            var usuario = new UsuarioAplicacao { UserName = Input.Email, Email = Input.Email };
            var resultado = await userManager.CreateAsync(usuario, Input.Senha);
            if (!resultado.Succeeded) { foreach (var erro in resultado.Errors) ModelState.AddModelError(string.Empty, erro.Description); return Page(); }
            var membershipResult = await userManager.AddToRoleAsync(usuario, NomesAutorizacao.SystemAdmin);
            if (!membershipResult.Succeeded)
            {
                foreach (var erro in membershipResult.Errors) ModelState.AddModelError(string.Empty, erro.Description);
                return Page();
            }
            await transaction.CommitAsync();
            return RedirectToPage("/Conta/Login");
        });
    }

    private async Task<bool> ExisteSystemAdminAsync() => await context.UserRoles.Join(
            context.Roles,
            membership => membership.RoleId,
            role => role.Id,
            (membership, role) => role.Name)
        .AnyAsync(nome => nome == NomesAutorizacao.SystemAdmin);

    private bool ChaveConfigurada() => !string.IsNullOrWhiteSpace(configuration["Bootstrap:SystemAdminKey"]);

    private static ContentResult ConfiguracaoIndisponivel() => new()
    {
        Content = "A configuração inicial está indisponível.",
        ContentType = "text/plain",
        StatusCode = StatusCodes.Status503ServiceUnavailable
    };

    private bool ChaveValida()
    {
        var esperada = configuration["Bootstrap:SystemAdminKey"]!;
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(esperada), Encoding.UTF8.GetBytes(Input.ChaveConfiguracao));
    }

    public sealed class InputModel
    {
        [Required, DataType(DataType.Password), Display(Name = "Chave de configuração")] public string ChaveConfiguracao { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)] public string Senha { get; set; } = string.Empty;
        [Required, DataType(DataType.Password), Compare(nameof(Senha), ErrorMessage = "As senhas não coincidem.")] [Display(Name = "Confirmação da senha")] public string ConfirmacaoSenha { get; set; } = string.Empty;
    }
}
