using System;
using System.Collections.Generic;

namespace CatalogoMusicas.Models;

public partial class Albun
{
    public int Id { get; set; }

    public string? SpotifyId { get; set; }

    public string Titulo { get; set; } = null!;

    public DateOnly? DataLancamento { get; set; }

    public string? DataLancamentoPrecisao { get; set; }

    public string? CapaUrl { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public virtual ICollection<Musica> Musicas { get; set; } = new List<Musica>();
}
