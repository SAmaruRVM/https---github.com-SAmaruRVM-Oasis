﻿using Microsoft.AspNetCore.Mvc.Rendering;
using Oasis.Dominio.Entidades;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Oasis.Web.Areas.Direcao.ViewModels
{
    public sealed class DisciplinasViewModel
    {
        public IEnumerable<Disciplina> Disciplinas { get; set; }
        public Disciplina Disciplina { get; set; }
        public Grupo Grupo { get; set; }

        public IEnumerable<SelectListItem> DropdownListProfessores { get; set; }

        [Range(minimum: 1, maximum: int.MaxValue)]
        public int ProfessorId { get; set; }
        public int DisciplinaId { get; set; }
    }
}
