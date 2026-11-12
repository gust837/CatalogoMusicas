using System;
using System.Collections.Generic;

namespace CatalogoMusicas.Models;

public partial class Genero
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Musica> Musicas { get; set; } = new List<Musica>();
}
