# Contexto do Projeto — Catálogo de Músicas com Spotify API (Padrão SENAI)

> **Arquivo de contexto para continuar o projeto em outra IA.**
> Contém o histórico da conversa a partir da definição do padrão SENAI, além do resumo do banco de dados, requisitos atualizados e código já implementado.

---

## 1. Resumo do que já está pronto

- **Stack definida:**
  - **Banco de Dados:** SQL Server (`CatalogoMusicas` — script em `database/CatalogoMusicas.sql`, já testado e executado).
  - **Back-end:** C# ASP.NET Core (`.NET 10.0`) seguindo o **padrão SENAI** (`Database-First` com `Scaffold-DbContext`, `BdContextCatalogo`, `Models`, `DTO`, `Interfaces`, `Repositories`, `Controllers`, `Services`, `Utils`, `Swagger`, `BCrypt` e `JWT`).
  - **Front-end:** React + TypeScript.
  - **Integração Spotify:**
    - Busca e importação de metadados no back-end via **Spotify Web API** usando fluxo *Client Credentials* (`Services/SpotifyService.cs` já implementado e testado com sucesso).
    - Reprodução de áudio no front-end via **Spotify Embed** (`<iframe src="https://open.spotify.com/embed/track/{spotify_id}">`), sem exigir conta Premium do usuário.

---

## 2. Histórico da Conversa (a partir da definição do Padrão SENAI)

### Usuário:
> Sim irei seguir o padrao do SENAI

### Assistente:
Perfeito! Vi que no seu computador o banco `CatalogoMusicas` está rodando na instância **`localhost\MSSQLSERVER2`** (em casa, lembre-se de ajustar o nome do `Server=` caso sua instância local seja `localhost\SQLEXPRESS` ou `localhost`) e seguindo a mesma arquitetura que você já usa no SENAI (`Models`, `BdContext`, `Interfaces`, `Repositories`, `Controllers`, `DTO`, `Utils` + `Swagger` + `JWT` + `BCrypt`).

Aqui está o passo a passo exato para preparar a base do projeto no padrão do SENAI:

#### Passo 1: Instalar os pacotes NuGet
Abra o **Console do Gerenciador de Pacotes** no Visual Studio (*Ferramentas → Gerenciador de Pacotes do NuGet → Console do Gerenciador de Pacotes*) e rode:

```powershell
Install-Package Microsoft.EntityFrameworkCore.SqlServer
Install-Package Microsoft.EntityFrameworkCore.Tools
Install-Package Swashbuckle.AspNetCore
Install-Package BCrypt.Net-Next
Install-Package Microsoft.AspNetCore.Authentication.JwtBearer
```

#### Passo 2: Gerar as `Models` e o `Context` (`Scaffold-DbContext`)
No mesmo **Console do Gerenciador de Pacotes**, rode o comando abaixo (trocando `Server=...` pelo nome da instância do seu SQL Server caso seja diferente em casa):

```powershell
Scaffold-DbContext "Server=localhost\MSSQLSERVER2;Database=CatalogoMusicas;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -ContextDir BdContextCatalogo -Context CatalogoContext -Force
```

Isso vai criar automaticamente:
- **`BdContextCatalogo/CatalogoContext.cs`**
- **`Models/`** com as classes das tabelas (`Usuario`, `Genero`, `Artista`, `Albun`, `Musica`, `MusicaArtista`, `VwMusicasSemGenero`)

#### Passo 3: Colocar a Connection String no `appsettings.json` (ou User Secrets)
No seu `appsettings.json`, adicione a seção `ConnectionStrings` e `Jwt`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\MSSQLSERVER2;Database=CatalogoMusicas;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "catalogo-musicas-chave-secreta-senai-2026",
    "Issuer": "CatalogoMusicas.WebAPI",
    "Audience": "CatalogoMusicas.WebAPI"
  }
}
```

#### Passo 4: Estrutura de pastas do projeto (padrão SENAI)
Depois do Scaffold, vamos organizar o projeto com as pastas que você já conhece:

```text
CatalogoMusicas/
├── BdContextCatalogo/
│   └── CatalogoContext.cs        (gerado pelo Scaffold)
├── Models/                       (geradas pelo Scaffold)
├── DTO/                          (LoginDTO, MusicaCadastroDTO, SpotifyTrackDTO...)
├── Interfaces/                   (IUsuarioRepository, IGeneroRepository, IMusicaRepository)
├── Repositories/                 (UsuarioRepository, GeneroRepository, MusicaRepository)
├── Services/
│   └── SpotifyService.cs         (busca e obtém dados da faixa no Spotify)
├── Utils/
│   └── Criptografia.cs           (GerarHash e CompararHash com BCrypt)
├── Controllers/                  (LoginController, UsuarioController, GeneroController, MusicaController, SpotifyController)
└── Program.cs                    (DbContext, Repositories, Swagger, CORS, JWT, MapControllers)
```

---

### Usuário:
> Estou querendo saber como seria o layout do site para poder montar um figma, poderia me informar se a API do Spotify retorna as letras das musicas?

### Assistente:

#### 1. A API do Spotify retorna a letra das músicas?
**Não.** A API oficial do Spotify **não fornece letras** por questões de direitos autorais (no app oficial deles, as letras vêm de uma parceria fechada com a *Musixmatch*).

Se você quiser colocar letras no seu layout do Figma e no sistema, você tem **duas opções**:
1. **Usar a API gratuita do [LRCLIB](https://lrclib.net/) (Recomendado se quiser automático):**
   - Não precisa de chave nem de cadastro.
   - Você passa o nome da música e do artista que vieram do Spotify (`GET https://lrclib.net/api/get?track_name=Bohemian+Rhapsody&artist_name=Queen`) e ela devolve a letra inteira gratuitamente.
2. **Deixar um campo no banco (`letra` ou a própria `descricao`):**
   - O Admin cola a letra ou uma curiosidade sobre a faixa na hora de cadastrar/editar.

#### 2. Como estruturar o Layout no Figma (Telas do Projeto)
Com base nos seus requisitos (**RF01 a RF15**) e no que o Spotify retorna, você precisa de **5 telas principais** (ou 3 telas + 2 modais):

- **O que você tem disponível para mostrar em cada música:**
  - **Vem do Spotify:** Capa do álbum (`640x640`), Título da música, Nome(s) do(s) artista(s), Nome do álbum, Ano/Data de lançamento, Duração (`05:54`) e o **Player Embed**.
  - **Vem do seu banco:** Gênero(s) (em formato de *tags/badges*, ex.: `Rock`, `Pop`) e Descrição/Curiosidade.

- **Tela 1: Login (`RF01`)**
  - Card centralizado com logo do catálogo, campos de **E-mail** e **Senha**, e botão **Entrar**.
  - *Lembrete da regra de negócio:* não tem link de "Criar conta", pois só o Admin cadastra usuários.

- **Tela 2: Catálogo / Home (`RF03`, `RF04`, `RF05`, `RF06`) — *Visão de Todos***
  - **Topo (Header):**
    - Barra de pesquisa por título (`RF05`).
    - Avatar/Nome do usuário logado (clicar abre edição do próprio perfil `RF07` e botão Sair `RF02`).
    - Se for **Admin**, exibe no menu os botões: *"Gerenciar Músicas"*, *"Gêneros"* e *"Usuários"*.
  - **Filtros de Gênero (`RF04`):**
    - Pílulas/botões horizontais no topo da listagem: `[Todos] [Rock] [Pop] [MPB] [Hip Hop]...`
  - **Grade de Cards de Músicas (`RF03`):**
    - Cada card mostra: **Capa do Álbum** (`album.images[0].url`), **Título**, **Artista**, **Badge do Gênero** e um botão de **Play ▶**.
  - **Rodapé Fixo (Player Bar) ou Modal de Detalhes (`RF06`):**
    - Quando o usuário clica numa música, abre os detalhes (Álbum, Ano, Duração, Gêneros, Descrição/Letra) e carrega o **Player Embed do Spotify** (`height: 80px` ou `152px`) fixo no rodapé da tela para ele continuar navegando enquanto ouve.

- **Tela 3: Cadastrar / Importar Música do Spotify (`RF08`, `RF09`, `RF10`) — *Só Admin***
  1. **Busca no Spotify:** Uma barra *"Buscar música no Spotify..."*. Quando o Admin pesquisa, aparece uma lista com os 10 resultados vindos da API (com fotinho da capa, título, artista, álbum e botão **"Selecionar"**).
  2. **Formulário de complemento:** Ao clicar em "Selecionar", os dados do Spotify já aparecem preenchidos e travados (Capa, Título, Artista, Álbum, Duração, Ano), e o Admin só preenche o que falta do seu sistema:
     - **Gênero(s):** caixas de seleção (`Rock`, `Pop`...) — `RN05`
     - **Descrição:** campo de texto — `RN06`
     - Botão **"Salvar no Catálogo"**.

- **Tela 4: Gerenciar Gêneros (`RF11`) — *Só Admin***
  - Pode ser uma tela simples ou um modal com um campo *"Novo gênero"* + botão *Adicionar*, e abaixo a lista dos gêneros cadastrados com botão de *Editar*.

- **Tela 5: Gerenciar Usuários (`RF12`, `RF13`, `RF14`, `RF15`) — *Só Admin***
  - Tabela listando: **Nome**, **E-mail**, **Perfil** (`ADMIN` ou `USER`), **Status** (`Ativo` / `Inativo`) e botões de *Editar* e *Ativar/Desativar* (switch/toggle).
  - Botão no topo **"+ Novo Usuário"** que abre um modal com Nome, E-mail, Senha e Perfil.

- **Dica para o Player no Figma:**
  - Reserve um retângulo de **`100%` de largura por `80px` (compacto) ou `152px` (completo) de altura** com cantos arredondados (`12px`) para representar o `<iframe src="https://open.spotify.com/embed/track/...">`.

---

### Usuário:
> Consegue alterar o docs que eu mandei para a forma como o banco esta construido?

### Assistente:
Atualizei o documento Word (`Levantamento de Requisitos e Regras de Negócio.docx`) mantendo toda a formatação e as tabelas originais, ajustando o texto exatamente para o que foi construído no banco `CatalogoMusicas.sql`:

| Código | Texto atualizado (fiel ao banco atual) |
|---|---|
| **RF06** | Visualizar detalhes e reproduzir uma música (player Spotify Embed) |
| **RF08** | Importar músicas da API do Spotify ou cadastrar manualmente |
| **RF09** | Editar informações de músicas |
| **RF10** | Excluir músicas do catálogo (exclusão lógica) |
| **RNF03** | Comunicação entre front-end e back-end por API REST e integração com a Spotify Web API |
| **RN05** | Cada música deverá estar associada a pelo menos um artista e um gênero. |
| **RN06** | O título e a duração da música serão obrigatórios; a descrição, o álbum e o identificador do Spotify (`spotify_id`) serão opcionais. |
| **RN07** | A duração de uma música (em milissegundos) deverá ser maior que zero. |
| **RN08** | A data de lançamento do álbum, quando informada, não poderá ser anterior a `01/01/1900` e deverá registrar sua precisão (`ano`, `mês` ou `dia`). |
| **RN10** | Músicas, álbuns e artistas deverão possuir um identificador único interno e, quando importados do Spotify, um `spotify_id` único. |

---

## 3. Onde paramos / Próximos passos para a próxima IA

1. Rodar o `Install-Package` e o `Scaffold-DbContext` no projeto `CatalogoMusicas` para gerar `BdContextCatalogo/CatalogoContext.cs` e `Models/`.
2. Criar o `Utils/Criptografia.cs` (com `BCrypt.Net-Next`).
3. Criar as `Interfaces` e `Repositories` (`UsuarioRepository`, `GeneroRepository`, `MusicaRepository`) incluindo a lógica de importação do Spotify (verificar se o `spotify_id` do artista/álbum já existe antes de inserir).
4. Criar os `Controllers` (`LoginController`, `UsuarioController`, `GeneroController`, `MusicaController`, `SpotifyController`) e configurar o `Program.cs` com `Swagger`, `JWT` e `CORS`.
