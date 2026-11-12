using System;
using System.Collections.Generic;

namespace CatalogoMusicas.Models;

public partial class Musica
{
    public int Id { get; set; }

    public string? SpotifyId { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descricao { get; set; }

    public int? AlbumId { get; set; }

    public int DuracaoMs { get; set; }

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public virtual Albun? Album { get; set; }

    public virtual ICollection<MusicaArtista> MusicaArtista { get; set; } = new List<MusicaArtista>();

    public virtual ICollection<Genero> Generos { get; set; } = new List<Genero>();
}
