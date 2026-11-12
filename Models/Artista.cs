using System;
using System.Collections.Generic;

namespace CatalogoMusicas.Models;

public partial class Artista
{
    public int Id { get; set; }

    public string? SpotifyId { get; set; }

    public string Nome { get; set; } = null!;

    public string? ImagemUrl { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public virtual ICollection<MusicaArtista> MusicaArtista { get; set; } = new List<MusicaArtista>();
}
