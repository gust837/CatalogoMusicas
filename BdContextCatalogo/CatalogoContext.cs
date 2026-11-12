using System;
using System.Collections.Generic;
using CatalogoMusicas.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusicas.BdContextCatalogo;

public partial class CatalogoContext : DbContext
{
    public CatalogoContext()
    {
    }

    public CatalogoContext(DbContextOptions<CatalogoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Albun> Albuns { get; set; }

    public virtual DbSet<Artista> Artistas { get; set; }

    public virtual DbSet<Genero> Generos { get; set; }

    public virtual DbSet<Musica> Musicas { get; set; }

    public virtual DbSet<MusicaArtista> MusicaArtistas { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<VwMusicasSemGenero> VwMusicasSemGeneros { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if ( !optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Albun>(entity =>
        {
            entity.ToTable("albuns");

            entity.HasIndex(e => e.SpotifyId, "UX_albuns_spotify_id")
                .IsUnique()
                .HasFilter("([spotify_id] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AtualizadoEm)
                .HasPrecision(0)
                .HasColumnName("atualizado_em");
            entity.Property(e => e.CapaUrl)
                .HasMaxLength(500)
                .HasColumnName("capa_url");
            entity.Property(e => e.CriadoEm)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_albuns_criado")
                .HasColumnName("criado_em");
            entity.Property(e => e.DataLancamento).HasColumnName("data_lancamento");
            entity.Property(e => e.DataLancamentoPrecisao)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("data_lancamento_precisao");
            entity.Property(e => e.SpotifyId)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasColumnName("spotify_id");
            entity.Property(e => e.Titulo)
                .HasMaxLength(250)
                .HasColumnName("titulo");
        });

        modelBuilder.Entity<Artista>(entity =>
        {
            entity.ToTable("artistas");

            entity.HasIndex(e => e.SpotifyId, "UX_artistas_spotify_id")
                .IsUnique()
                .HasFilter("([spotify_id] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AtualizadoEm)
                .HasPrecision(0)
                .HasColumnName("atualizado_em");
            entity.Property(e => e.CriadoEm)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_artistas_criado")
                .HasColumnName("criado_em");
            entity.Property(e => e.ImagemUrl)
                .HasMaxLength(500)
                .HasColumnName("imagem_url");
            entity.Property(e => e.Nome)
                .HasMaxLength(200)
                .HasColumnName("nome");
            entity.Property(e => e.SpotifyId)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasColumnName("spotify_id");
        });

        modelBuilder.Entity<Genero>(entity =>
        {
            entity.ToTable("generos");

            entity.HasIndex(e => e.Nome, "UQ_generos_nome").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome)
                .HasMaxLength(60)
                .HasColumnName("nome");
        });

        modelBuilder.Entity<Musica>(entity =>
        {
            entity.ToTable("musicas");

            entity.HasIndex(e => e.Titulo, "IX_musicas_titulo");

            entity.HasIndex(e => e.SpotifyId, "UX_musicas_spotify_id")
                .IsUnique()
                .HasFilter("([spotify_id] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AlbumId).HasColumnName("album_id");
            entity.Property(e => e.Ativo)
                .HasDefaultValue(true, "DF_musicas_ativo")
                .HasColumnName("ativo");
            entity.Property(e => e.AtualizadoEm)
                .HasPrecision(0)
                .HasColumnName("atualizado_em");
            entity.Property(e => e.CriadoEm)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_musicas_criado")
                .HasColumnName("criado_em");
            entity.Property(e => e.Descricao)
                .HasMaxLength(1000)
                .HasColumnName("descricao");
            entity.Property(e => e.DuracaoMs).HasColumnName("duracao_ms");
            entity.Property(e => e.SpotifyId)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasColumnName("spotify_id");
            entity.Property(e => e.Titulo)
                .HasMaxLength(250)
                .HasColumnName("titulo");

            entity.HasOne(d => d.Album).WithMany(p => p.Musicas)
                .HasForeignKey(d => d.AlbumId)
                .HasConstraintName("FK_musicas_albuns");

            entity.HasMany(d => d.Generos).WithMany(p => p.Musicas)
                .UsingEntity<Dictionary<string, object>>(
                    "MusicaGenero",
                    r => r.HasOne<Genero>().WithMany()
                        .HasForeignKey("GeneroId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_mg_generos"),
                    l => l.HasOne<Musica>().WithMany()
                        .HasForeignKey("MusicaId")
                        .HasConstraintName("FK_mg_musicas"),
                    j =>
                    {
                        j.HasKey("MusicaId", "GeneroId");
                        j.ToTable("musica_generos");
                        j.IndexerProperty<int>("MusicaId").HasColumnName("musica_id");
                        j.IndexerProperty<int>("GeneroId").HasColumnName("genero_id");
                    });
        });

        modelBuilder.Entity<MusicaArtista>(entity =>
        {
            entity.HasKey(e => new { e.MusicaId, e.ArtistaId });

            entity.ToTable("musica_artistas");

            entity.Property(e => e.MusicaId).HasColumnName("musica_id");
            entity.Property(e => e.ArtistaId).HasColumnName("artista_id");
            entity.Property(e => e.Ordem)
                .HasDefaultValue((byte)1, "DF_ma_ordem")
                .HasColumnName("ordem");

            entity.HasOne(d => d.Artista).WithMany(p => p.MusicaArtista)
                .HasForeignKey(d => d.ArtistaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ma_artistas");

            entity.HasOne(d => d.Musica).WithMany(p => p.MusicaArtista)
                .HasForeignKey(d => d.MusicaId)
                .HasConstraintName("FK_ma_musicas");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Email, "UQ_usuarios_email").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Ativo)
                .HasDefaultValue(true, "DF_usuarios_ativo")
                .HasColumnName("ativo");
            entity.Property(e => e.CriadoEm)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_usuarios_criado")
                .HasColumnName("criado_em");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Nome)
                .HasMaxLength(120)
                .HasColumnName("nome");
            entity.Property(e => e.Perfil)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("USER", "DF_usuarios_perfil")
                .HasColumnName("perfil");
            entity.Property(e => e.SenhaHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("senha_hash");
        });

        modelBuilder.Entity<VwMusicasSemGenero>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_musicas_sem_genero");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.SpotifyId)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasColumnName("spotify_id");
            entity.Property(e => e.Titulo)
                .HasMaxLength(250)
                .HasColumnName("titulo");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
