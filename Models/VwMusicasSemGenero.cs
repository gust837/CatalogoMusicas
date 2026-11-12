using System;
using System.Collections.Generic;

namespace CatalogoMusicas.Models;

public partial class VwMusicasSemGenero
{
    public int Id { get; set; }

    public string Titulo { get; set; } = null!;

    public string? SpotifyId { get; set; }
}
