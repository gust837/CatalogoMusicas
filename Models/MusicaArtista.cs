using System;
using System.Collections.Generic;

namespace CatalogoMusicas.Models;

public partial class MusicaArtista
{
    public int MusicaId { get; set; }

    public int ArtistaId { get; set; }

    public byte Ordem { get; set; }

    public virtual Artista Artista { get; set; } = null!;

    public virtual Musica Musica { get; set; } = null!;
}
