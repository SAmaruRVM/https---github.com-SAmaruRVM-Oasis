﻿using System;
using System.ComponentModel.DataAnnotations;

namespace Oasis.Dominio.Entidades
{
    public class ComentarioPostUtilizador
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O comentário tem que ser obrigatóriamente preenchido!")]
        [StringLength(200, ErrorMessage = "O comentário {0} tem que ter no máximo {1} caracteres!")]
        [Display(Prompt = "Escreve merda aqui")]
        public string Comentario { get; set; }

        // default getdate() <<-- SQL
        public DateTime DataCriacao { get; set; }

        public Post Post { get; set; }

        public ApplicationUser Utilizador { get; set; }

        public int PostId { get; set; }

        public int ApplicationUserId { get; set; }
    }
}
