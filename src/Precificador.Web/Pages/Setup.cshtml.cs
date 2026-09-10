using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages;

public sealed class SetupModel(PrecificadorDbContext context, UserManager<UsuarioAplicacao> userManager) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public async Task<IActionResult> OnGetAsync() => await userManager.Users.AnyAsync() ? NotFound() : Page();
    public async Task<IActionResult> OnPostAsync()
    {
        if (await userManager.Users.AnyAsync()) return NotFound();
        if (!ModelState.IsValid) return Page();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var empresa = await context.Empresas.SingleAsync(empresa => empresa.NomeNormalizado == "EMPRESA INICIAL");
        try { empresa.Renomear(Input.NomeEmpresa); }
        catch (ArgumentException exception) { ModelState.AddModelError("Input.NomeEmpresa", exception.Message); return Page(); }
        var usuario = new UsuarioAplicacao { UserName = Input.Email, Email = Input.Email };
        var resultado = await userManager.CreateAsync(usuario, Input.Senha);
        if (!resultado.Succeeded) { foreach (var erro in resultado.Errors) ModelState.AddModelError(string.Empty, erro.Description); return Page(); }
        context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = empresa.Id, Ativo = true });
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return RedirectToPage("/Conta/Login");
    }
    public sealed class InputModel
    {
        [Required, Display(Name = "Nome da empresa")] public string NomeEmpresa { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)] public string Senha { get; set; } = string.Empty;
        [Required, DataType(DataType.Password), Compare(nameof(Senha), ErrorMessage = "As senhas não coincidem.")] [Display(Name = "Confirmação da senha")] public string ConfirmacaoSenha { get; set; } = string.Empty;
    }
}
