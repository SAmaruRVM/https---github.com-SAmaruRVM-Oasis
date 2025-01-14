﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Oasis.Aplicacao.Extensions;
using Oasis.Dados;
using Oasis.Dominio.Entidades;
using Oasis.Web.Areas.Administrador.ViewModels;
using Oasis.Web.Extensions;
using Oasis.Web.Http;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Oasis.Web.Areas.Administrador.Controllers
{
    public class ContactosController : BaseAdministradorController
    {
        private readonly OasisContext _context;
        private readonly IConfiguration _configuration;
        public ContactosController(OasisContext context, IConfiguration configuration)
        => (_context, _configuration) = (context, configuration);

        [HttpGet]
        public async Task<ViewResult> Index() => View(model: new ContactosViewModel
        {
            Contactos = await _context.Contactos
                                      .AsNoTracking()
                                      .OrderByDescending(contacto => contacto.DataContacto)
                                      .ToListAsync()
        });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RespostaContacto([FromForm] ContactosViewModel contactosViewModel)
        {
            if (!(ModelState.IsValid))
            {
                return Json(new Ajax
                {
                    Titulo = "Erro ao adicionar à resposta ao contacto pretendido!",
                    Descricao = "Os dados indicados não se encontram num formato válido!",
                    OcorreuAlgumErro = true,
                    UrlRedirecionar = string.Empty
                });
            }

            Contacto contacto = await _context.Contactos
                                              .FindAsync(contactosViewModel.RespostaContactoAdicionar.ContactoId);


            if (contacto is null)
            {
                return Json(new Ajax
                {
                    Titulo = "Ocorreu um erro na da resposta ao contacto selecionado!",
                    Descricao = "Pedimos desculpa pelo incómodo. Já foi enviado a informação aos nossos técnicos. Por favor, tente novamente mais tarde.",
                    OcorreuAlgumErro = true,
                    UrlRedirecionar = string.Empty
                });
            }


            RespostaContacto respostaContacto = new()
            {
                Resposta = contactosViewModel.RespostaContactoAdicionar.Resposta,
                ContactoId = contactosViewModel.RespostaContactoAdicionar.ContactoId
            };

            contacto.Respondido = true;

            _context.Contactos.Update(contacto);
            _context.RespostasContactos.Add(respostaContacto);
            await _context.SaveChangesAsync();


            using SmtpClient client = new();
            await client.EnviarEmailAsync($"{_configuration["Projeto:Nome"]} - Resposta ao seu contacto", $"Caro/a {contacto.PrimeiroNome} {contacto.Apelido}, o seu contacto já foi visto pelos nossos colaboradores e temos a seguinte resposta:<hr/>{respostaContacto.Resposta}<hr/><strong>Obrigado!</strong>", contacto.EmailContactante, client.ConfiguracoesEmail(_configuration));

            return Json(new
            {
                Ajax = new Ajax
                {
                    Titulo = "A resposta ao contacto foi adicionada com sucesso!",
                    Descricao = "Foi enviado um email ao contactante com a sua resposta. Obrigado!",
                    OcorreuAlgumErro = false,
                    UrlRedirecionar = string.Empty
                },
                Contacto = contacto
            });
        }


        [HttpGet("[area]/[controller]/[action]/{id}")]
        public async Task<JsonResult> Id(int id) => Json(await _context.Contactos.AsNoTracking().Include(contacto => contacto.RespostaContacto).SingleOrDefaultAsync(contacto => contacto.Id == id));

    }
}
