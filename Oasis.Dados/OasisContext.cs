﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Oasis.Dominio.Entidades;
using Oasis.Dominio.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oasis.Dados
{
    public class OasisContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public OasisContext(DbContextOptions<OasisContext> options) : base(options) { }

        public DbSet<ApplicationUser> Utilizadores { get; set; }
        public DbSet<ComentarioPostUtilizador> ComentariosPostsUtilizadores { get; set; }
        public DbSet<Contacto> Contactos { get; set; }
        public DbSet<Disciplina> Disciplinas { get; set; }
        public DbSet<Equipamento> Equipamentos { get; set; }
        public DbSet<Escola> Escolas { get; set; }
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<GrupoAluno> GruposAlunos { get; set; }
        public DbSet<Notificacao> Notificacoes { get; set; }
        public DbSet<PaginaPrincipal> ConteudoPaginaPrincipalEscolas { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostGostoUtilizador> PostsGostosUtilizadores { get; set; }
        public DbSet<PostUtilizadorGuardado> PostsUtilizadoresGuardados { get; set; }
        public DbSet<Reacao> Reacoes { get; set; }
        public DbSet<RequisicaoEquipamento> RequisicaoEquipamentos { get; set; }
        public DbSet<RespostaContacto> RespostasContactos { get; set; }
        public DbSet<Tema> Temas { get; set; }
        public DbSet<TipoPost> TiposPosts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
                   .Ignore(user => user.AccessFailedCount)
                   .Ignore(user => user.LockoutEnabled)
                   .Ignore(user => user.LockoutEnd)
                   .Ignore(user => user.TwoFactorEnabled)
                   .Ignore(user => user.PhoneNumber)
                   .Ignore(user => user.PhoneNumberConfirmed)
                   .ToTable("Utilizadores")
                   .Property(user => user.DataCriacao)
                   .HasDefaultValueSql("getdate()");

            // Inserção dos tipos de Utilizador
            int indexTiposUtilizador = 0;
            builder.Entity<IdentityRole<int>>()
                   .ToTable("TiposUtilizador")
                   .HasData(Enum.GetValues<TipoUtilizador>()
                                .Select(tipoUtilizador => new IdentityRole<int>
                                {
                                    Id = ++indexTiposUtilizador,
                                    Name = tipoUtilizador.ToString(),
                                    NormalizedName = tipoUtilizador.ToString().ToUpper()
                                }));


            // Inserção de temas
            int indexTemas = 0;
            builder.Entity<Tema>()
                   .HasData(new Dictionary<string, string>
                   {
                       ["Minty"] = "https://cdn.jsdelivr.net/npm/bootswatch@4.5.2/dist/minty/bootstrap.min.css",
                       ["Cerulean"] = "https://cdn.jsdelivr.net/npm/bootswatch@4.5.2/dist/cerulean/bootstrap.min.css",
                       ["Darkly"] = "https://cdn.jsdelivr.net/npm/bootswatch@4.5.2/dist/litera/bootstrap.min.css",
                       ["Sketchy"] = "https://cdn.jsdelivr.net/npm/bootswatch@4.5.2/dist/sketchy/bootstrap.min.css",
                       ["Cyborg"] = "https://cdn.jsdelivr.net/npm/bootswatch@4.5.2/dist/litera/bootstrap.min.css",
                       ["Journal"] = "https://cdn.jsdelivr.net/npm/bootswatch@4.5.2/dist/journal/bootstrap.min.css",
                       ["Solar"] = "https://cdn.jsdelivr.net/npm/bootswatch@4.5.2/dist/lux/bootstrap.min.css"
                   }.Select(tema => new Tema
                   {
                       Id = ++indexTemas,
                       Nome = tema.Key,
                       LinkCdn = tema.Value
                   }));

            //Inserção de reações
             int indexReacoes = 0;
            builder.Entity<Reacao>()
                   .HasData(new Dictionary<string, string>
                   {
                       ["Gosto"] = "assets/iconesReacoes/gosto.png",
                       ["Em Cheio!"] = "assets/iconesReacoes/emCheio.png",
                       ["Contente"] = "assets/iconesReacoes/contente.png",
                       ["Adoro"] = "assets/iconesReacoes/adoro.png",
                       ["Interessante"] = "assets/iconesReacoes/interessante.png",
                       ["Supreendente"] = "assets/iconesReacoes/surpreendente.png"
                   }.Select(reacao => new Reacao
                   {
                       Id = ++indexReacoes,
                       Titulo = reacao.Key,
                       Icone = reacao.Value
                   }));


            // Comentarios Posts Utilizadores

            builder.Entity<ComentarioPostUtilizador>()
                   .HasOne(cpu => cpu.Utilizador)
                   .WithMany(user => user.Comentarios)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ComentarioPostUtilizador>()
                   .HasOne(cpu => cpu.Post)
                   .WithMany(post => post.Comentarios)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ComentarioPostUtilizador>()
                   .Property(cpu => cpu.DataCriacao)
                   .HasDefaultValueSql("getdate()");

            // Grupos Alunos
            builder.Entity<GrupoAluno>()
                   .HasKey(grupoAluno => new { grupoAluno.ApplicationUserId, grupoAluno.GrupoId });

            builder.Entity<GrupoAluno>()
                   .HasOne(grupoAluno => grupoAluno.Grupo)
                   .WithMany(grupo => grupo.Alunos)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<GrupoAluno>()
                   .HasOne(grupoAluno => grupoAluno.Aluno)
                   .WithMany(aluno => aluno.GruposOndeTemAulas)
                   .OnDelete(DeleteBehavior.NoAction);

           builder.Entity<GrupoAluno>()
                  .Property(grupoAluno => grupoAluno.DataInsercao)
                  .HasDefaultValueSql("getdate()");

            // Posts Gostos Utilizadores
            builder.Entity<PostGostoUtilizador>()
                   .HasKey(pgu => new { pgu.ApplicationUserId, pgu.PostId });

            builder.Entity<PostGostoUtilizador>()
                   .HasOne(pgu => pgu.Reacao)
                   .WithMany(reacao => reacao.PostsQueFizeramUsoDestaReacao)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<PostGostoUtilizador>()
                   .HasOne(pgu => pgu.Post)
                   .WithMany(post => post.UtilizadoresQueGostaram)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<PostGostoUtilizador>()
                   .HasOne(pgu => pgu.Utilizador)
                   .WithMany(user => user.PostsGostados)
                   .OnDelete(DeleteBehavior.NoAction);

            // Posts Utilizadores Guardados
            builder.Entity<PostUtilizadorGuardado>()
                   .HasKey(pug => new { pug.ApplicationUserId, pug.PostId });

            builder.Entity<PostUtilizadorGuardado>()
                   .HasOne(pug => pug.Utilizador)
                   .WithMany(user => user.PostsGuardados)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<PostUtilizadorGuardado>()
                   .HasOne(pug => pug.Post)
                   .WithMany(post => post.UtilizadoresQueGuardaram)
                   .OnDelete(DeleteBehavior.NoAction);


            // Grupos
            builder.Entity<Grupo>()
                   .HasOne(grupo => grupo.Professor)
                   .WithMany(professor => professor.GruposOndeEnsina)
                   .OnDelete(DeleteBehavior.NoAction);

            // Posts
            builder.Entity<Post>()
                   .HasOne(post => post.Criador)
                   .WithMany(criador => criador.PostsCriados)
                   .OnDelete(DeleteBehavior.NoAction);


            builder.Entity<Post>()
                   .Property(post => post.DataCriacao)
                   .HasDefaultValueSql("getdate()");


            // Contactos
            builder.Entity<Contacto>()
                   .Property(contacto => contacto.DataContacto)
                   .HasDefaultValueSql("getdate()");

            // Notificações
            builder.Entity<Notificacao>()
                   .Property(notificacao => notificacao.DataCriacao)
                   .HasDefaultValueSql("getdate()");

            // Requisições dos Equipamentos
            builder.Entity<RequisicaoEquipamento>()
                   .Property(requisicao => requisicao.DataRequisicao)
                   .HasDefaultValueSql("getdate()");

            builder.Entity<RequisicaoEquipamento>()
                   .HasOne(requisicao => requisicao.Aluno)
                   .WithMany(aluno => aluno.RequisicoesEquipamento)
                   .OnDelete(DeleteBehavior.NoAction);

            // Paginas Principais
            builder.Entity<PaginaPrincipal>()
                   .Property(pagina => pagina.DataCriacao)
                   .HasDefaultValueSql("getdate()");

            // Equipamentos
            builder.Entity<Equipamento>()
                   .Property(equipamento => equipamento.Quantidade)
                   .HasDefaultValue(0);



            // Escolas
            builder.Entity<Escola>()
                   .Property(escola => escola.DataCriacao)
                   .HasDefaultValueSql("getdate()");

            builder.Entity<Escola>()
                   .HasMany(escola => escola.Membros)
                   .WithOne(membro => membro.Escola)
                   .OnDelete(DeleteBehavior.NoAction);

           // Disciplinas
           builder.Entity<Disciplina>()
                  .Property(disciplina => disciplina.DataCriacao)
                  .HasDefaultValueSql("getdate()");

       
           builder.Entity<Disciplina>()
                  .HasOne(disciplina => disciplina.CriadorDirecao)
                  .WithMany(criadorDirecao => criadorDirecao.DisciplinasCriadas)
                  .OnDelete(DeleteBehavior.NoAction);

           builder.Entity<Disciplina>()
                  .HasOne(disciplina => disciplina.Escola)
                  .WithMany(escola => escola.Disciplinas)
                  .OnDelete(DeleteBehavior.NoAction);


           builder.Entity<Disciplina>()
                  .HasMany(disciplina => disciplina.Grupos)
                  .WithOne(grupo => grupo.Disciplina)
                  .OnDelete(DeleteBehavior.NoAction);


           // RespostasContactos
           builder.Entity<RespostaContacto>()
                  .Property(respostaContacto => respostaContacto.DataResposta)
                  .HasDefaultValueSql("getdate()");

            builder.Entity<Post>()
                   .HasOne(post => post.TipoPost)
                   .WithMany(tipoPost => tipoPost.Posts)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
