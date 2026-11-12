using System;
using System.Collections.Generic;

namespace CatalogoMusicas.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string SenhaHash { get; set; } = null!;

    public string Perfil { get; set; } = null!;

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }
}
