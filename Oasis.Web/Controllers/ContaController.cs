﻿using System;
using Identity = Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Oasis.Web.ViewModels;
using System.Threading.Tasks;
using Oasis.Dados;
using Microsoft.Extensions.Configuration;
using Oasis.Dominio.Entidades;
using Microsoft.AspNetCore.Identity;
using Oasis.Web.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Oasis.Aplicacao.Extensions;
using Oasis.Dominio.Enums;
using Microsoft.AspNetCore.Authorization;
using Oasis.Web.Extensions;
using System.Linq;
using System.Net.Mail;

namespace Oasis.Web.Controllers
{
    [Route("[controller]")]
    public class ContaController : Controller
    {
        private readonly OasisContext _context;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContaController(IConfiguration configuration, OasisContext context, SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
            => (_configuration, _context, _signInManager, _userManager) = (configuration, context, signInManager, userManager);


        [HttpPost("[action]")]
        public async Task<JsonResult> Login([FromForm] LoginViewModel loginViewModel)
        {


            var utilizadorTentativaLogin = await _context.Utilizadores
                                                         .SingleOrDefaultAsync(utilizador => utilizador.Email == loginViewModel.Email);


            if (utilizadorTentativaLogin is null)
            {
                return Json(new Ajax
                {
                    Titulo = "Os dados fornecidos para autenticação não estão corretos!",
                    Descricao = "Por favor, introduza o email e a password corretamente.",
                    OcorreuAlgumErro = true,
                    UrlRedirecionar = string.Empty
                });
            }

            Identity.SignInResult result = await _signInManager.PasswordSignInAsync(user: utilizadorTentativaLogin, password: loginViewModel.Password, isPersistent: loginViewModel.LembrarDeMim, lockoutOnFailure: false);

            if (!(result.Succeeded))
            {
                return Json(new Ajax
                {
                    Titulo = "Os dados fornecidos para autenticação não estão corretos!",
                    Descricao = "Por favor, introduza o email e a password corretamente.",
                    OcorreuAlgumErro = true,
                    UrlRedirecionar = string.Empty
                });
            }


            if (!(utilizadorTentativaLogin.EmailConfirmed))
            {
                return Json(new Ajax
                {
                    Titulo = "O seu email ainda não foi confirmado.",
                    Descricao = "Para que possa entrar na sua conta, terá que ter o seu email confirmado. Por favor, verifique o seu email ou entre em contacto connosco.",
                    OcorreuAlgumErro = true,
                    UrlRedirecionar = string.Empty
                });
            }

            if (utilizadorTentativaLogin.DataUltimoLogin is null)
            {
                HttpContext.Session.SetString("PrimeiraVezLogin", true.ToString());
            }

            utilizadorTentativaLogin.DataUltimoLogin = DateTime.Now;
            await _userManager.UpdateAsync(utilizadorTentativaLogin);

            return Json(new Ajax
            {
                Titulo = "Login foi realizado com sucesso!",
                Descricao = "Será agora redirecionado para a página da sua escola.",
                OcorreuAlgumErro = false,
                UrlRedirecionar = (await _userManager.IsInRoleAsync(user: utilizadorTentativaLogin, role: TipoUtilizador.Administrador.ToString())) ? $"administrador/" : $"escola"
            });
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<RedirectToActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(actionName: "Index", controllerName: "Home");
        }


        [HttpGet("[action]")]
        public async Task<JsonResult> EmailEValido(string email) => Json(await _userManager.FindByEmailAsync(email) is null);

        [HttpGet("confirmacao-email/{emailEncriptado}")]
        public ContentResult ConfirmacaoEmail(string emailEncriptado)
         => Content($"<form action='{Request.Scheme}://{Request.Host}/conta/confirmacao-email' id='form-confirmacao-email' method='post'><input type='hidden' name='emailDesencriptado' value='{emailEncriptado.Decrypt()}'/></form><script>document.getElementById('form-confirmacao-email').submit();</script>", "text/html");


        [HttpPost("confirmacao-email")]
        public async Task<IActionResult> ConfirmacaoEmailPost([FromForm] string emailDesencriptado)
        {
            HttpContext.Session.SetString("EmailConfirmado", true.ToString());

            var user = await _userManager.FindByEmailAsync(emailDesencriptado);

            if (user is null)
            {
                return NotFound();
            }

            user.EmailConfirmed = true;

            await _userManager.UpdateAsync(user);

            return RedirectToAction(actionName: "Index", controllerName: "Home", fragment: "Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AlterarPassword([FromForm] EscolaViewModel escolaViewModel)
        {
            if (!(ModelState.IsValid))
            {
                return Json(new Ajax
                {
                    Titulo = "Os dados indicados não se encontram num formato válido!",
                    Descricao = "Por favor, introduza os dados corretamente.",
                    OcorreuAlgumErro = true,
                    UrlRedirecionar = string.Empty
                });
            }

            ApplicationUser user = await _context.Utilizadores
                                                       .Include(utilizador => utilizador.Escola)
                                                       .SingleOrDefaultAsync(utilizador => utilizador.Email == User.Identity.Name);

            var alterarPassword = await _userManager.ChangePasswordAsync(
                user: user,
                currentPassword: escolaViewModel.AlterarPasswordViewModel.Password,
                newPassword: escolaViewModel.AlterarPasswordViewModel.NovaPassword
            );


            if (!(alterarPassword.Succeeded))
            {
                return Json(new Ajax
                {
                    Titulo = "Não foi possível alterar a sua password!",
                    Descricao = "A password indicada não corresponde à password que está associada à sua conta.",
                    OcorreuAlgumErro = true,
                    UrlRedirecionar = string.Empty
                });
            }

            return Json(new Ajax
            {
                Titulo = "A sua password foi alterada com sucesso!",
                Descricao = "Recomendamos que altere a sua password com alguma frequência.",
                OcorreuAlgumErro = false,
                UrlRedirecionar = string.Empty
            });
        }

        [HttpGet("[action]")]
        [Authorize]
        public async Task<ViewResult> Perfil()
        {
            var utilizadorLogado = await _context.GetLoggedInApplicationUser(User.Identity.Name);

            IOrderedEnumerable<DisciplinaGruposAlunos> disciplinaGruposAlunos;
            if (await _userManager.IsInRoleAsync(utilizadorLogado, TipoUtilizador.Professor.ToString()))
            {
                disciplinaGruposAlunos = utilizadorLogado.GruposOndeEnsina
                                                .GroupBy(grupo => grupo.Disciplina.Nome)
                                                .Select(disciplina => new DisciplinaGruposAlunos
                                                {
                                                    NomeDisciplina = disciplina.Key,
                                                    GruposOndeEnsina = disciplina
                                                })
                                                .OrderBy(disciplina => disciplina.NomeDisciplina);
            }
            else
            {

                disciplinaGruposAlunos = utilizadorLogado.GruposOndeTemAulas
                                                        .GroupBy(grupo => grupo.Grupo.Disciplina.Nome)
                                                        .Select(disciplina => new DisciplinaGruposAlunos
                                                        {
                                                            NomeDisciplina = disciplina.Key,
                                                            GruposOndeEnsina = disciplina.Select(d => d.Grupo)
                                                        })
                                                          .OrderBy(disciplina => disciplina.NomeDisciplina);
            }


            return View(model: new PerfilViewModel
            {
                EscolaViewModel = new EscolaViewModel
                {
                    DisciplinaGruposAlunos = disciplinaGruposAlunos
                }
            });
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AtualizarPerfil([FromForm] PerfilViewModel perfilViewModel)
        {
            if (!(ModelState.IsValid))
            {
                return Json(new Ajax
                {
                    Titulo = "Os dados indicados não se encontram num formato válido!",
                    Descricao = "Por favor, introduza os dados corretamente.",
                    OcorreuAlgumErro = true,
                    UrlRedirecionar = string.Empty
                });
            }




            var userLogado = await _context.GetLoggedInApplicationUser(User.Identity.Name);


            var stream = perfilViewModel.ImagemPerfil.OpenReadStream();

            userLogado.ImagemPerfil = await stream.ToCharArrayAsync();

            userLogado.DescricaoPerfil = perfilViewModel.Descricao;


            await _userManager.UpdateAsync(user: userLogado);


            return Json(new
            {
                Ajax = new Ajax
                {
                    Titulo = "O seu perfil foi atualizado com sucesso!",
                    Descricao = string.Empty,
                    OcorreuAlgumErro = false,
                    UrlRedirecionar = string.Empty
                },
                ImagemUser = $"data:image/png;base64,{Convert.ToBase64String(userLogado.ImagemPerfil)}"
            });
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RecuperarPassword([FromForm] LoginViewModel loginViewModel)
        {

            var passwordGerada = Guid.NewGuid();

            var user = await _userManager.FindByEmailAsync(loginViewModel.EmailRecuperacaoPassword);

            if (user is null)
            {
                return Json(new Ajax
                {
                    Titulo = "Não foi encontrado nenhuma conta com o email facultado!",
                    Descricao = "Por favor, introduza os dados corretamente.",
                    OcorreuAlgumErro = true,
                    UrlRedirecionar = string.Empty
                });
            }

            await _userManager.RemovePasswordAsync(user);
            await _userManager.AddPasswordAsync(user, passwordGerada.ToString());
            


            using SmtpClient client = new();

            await client.EnviarEmailAsync($"{_configuration["Projeto:Nome"]} - O seu pedido de recuperação de password", $"Como requisitado, foi gerada uma password temporária de modo a que se possa autenticar e alterar a sua password:<hr/><h3>Password gerada:<h3/>{passwordGerada}<hr/><h2>Recomendamos que altere a sua password com a maior brevidade possível.<br/>Obrigado,<br/>{_configuration["Projeto:Nome"]}</h2>", loginViewModel.EmailRecuperacaoPassword, client.ConfiguracoesEmail(_configuration));




            return Json(new Ajax
            {
                Titulo = "Foi-lhe enviado um email para que possa recuperar a password.",
                Descricao = string.Empty,
                OcorreuAlgumErro = false,
                UrlRedirecionar = string.Empty
            });
        }





        [HttpPost("[action]")]
        public async Task<JsonResult> MarcarNotificacoesComoVistas()
        {
            var userLogado = await _context.GetLoggedInApplicationUser(User.Identity.Name);

            foreach (Notificacao notificacao in userLogado.Notificacoes)
            {
                notificacao.FoiVista = true;
            }

            _context.Notificacoes.UpdateRange(userLogado.Notificacoes);

            await _context.SaveChangesAsync();

            return Json(new Ajax
            {
                Titulo = string.Empty,
                Descricao = string.Empty,
                OcorreuAlgumErro = false,
                UrlRedirecionar = string.Empty
            });
        }
    }
}
