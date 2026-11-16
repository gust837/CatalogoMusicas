# Contexto do Projeto — Catálogo de Músicas & Player com Spotify, LRCLIB, Gemini e YouTube (Padrão SENAI)

> **Documento de acompanhamento e contexto do projeto (Casa / SENAI).**
> Contém a visão do produto, o resumo completo de tudo o que já foi implementado na Web API (.NET 10.0), o estado atual do Front-end (React + TypeScript), como rodar o projeto nos computadores do SENAI e os próximos passos.

---

## 1. Conceito e Identidade do Produto (Catálogo Musical + Player)

O sistema **não é apenas um player de música**, mas sim um **Catálogo Musical Interativo e Enciclopédico** integrado a um Player com letras sincronizadas:

* **Foco no Catálogo e na Ficha Técnica da Música:**
  * A **Descrição** de cada música não é um texto raso: ela apresenta **curiosidades, história de bastidores, detalhes de composição/gravação**, além de contextualizar **a qual álbum a música pertence** e **qual faixa (*track number*) ela é dentro daquele álbum** (junto com capa em alta resolução, data/ano de lançamento, artistas em ordem e gêneros musicais).
* **Acesso Público para Visitantes (`[AllowAnonymous]`):**
  * Qualquer visitante (mesmo sem estar logado) pode navegar pelo catálogo, pesquisar músicas por título, filtrar por gêneros, abrir a página de detalhes completos da música (lendo sua história, curiosidades e informações do álbum/faixa) e, caso queira, iniciar a reprodução da música acompanhando a letra sincronizada e sua tradução.
* **Reprodução Completa sem exigir Spotify Premium:**
  * Os metadados oficiais (título, álbum, capa, data de lançamento, duração exata em milissegundos e artistas) são importados da **Spotify Web API**.
  * O áudio completo é reproduzido gratuitamente via **YouTube IFrame Player API** (iniciando pausado com `autoplay: 0`, tocando apenas quando o usuário clica no botão de iniciar).
  * A sincronia entre áudio e legenda é garantida cruzando a duração oficial do Spotify (`DuracaoMs`) tanto na busca da letra sincronizada (**LRCLIB**) quanto na seleção da faixa de estúdio no **YouTube** (`"Provided to YouTube"` / `"official audio"`).

---

## 2. O que já está 100% pronto (Back-end Web API)

A **Web API em ASP.NET Core (.NET 10.0)** está completa, compilando sem erros ou avisos (`0 Aviso(s), 0 Erro(s)`) e seguindo o padrão arquitetural do SENAI:

### Estrutura implementada no projeto (`CatalogoMusicas-main`):
* **`database/CatalogoMusicas.sql`**
  * Script SQL Server completo com tabelas (`usuarios`, `generos`, `artistas`, `albuns`, `musicas`, `musica_artistas`, `musica_generos`), *constraints*, índices únicos filtrados (`WHERE spotify_id IS NOT NULL`), `vw_musicas_sem_genero` e massa de dados inicial (incluindo *Bohemian Rhapsody* com `spotify_id` real).
* **`BdContextCatalogo/CatalogoContext.cs` & `Models/`**
  * Gerados via `Scaffold-DbContext` (`Albun`, `Artista`, `Genero`, `Musica`, `MusicaArtista`, `Usuario`, `VwMusicasSemGenero`).
  * `OnConfiguring` ajustado com `if (!optionsBuilder.IsConfigured)` para respeitar a Connection String do `appsettings.json`.
* **`Utils/`**
  * `Criptografia.cs`: Geração e verificação de hash de senhas com **BCrypt** (`BCrypt.Net-Next`) — atende **`RNF02`**.
  * `JwtSettings.cs`: Mapeamento da seção `"Jwt"` do `appsettings.json`.
* **`DTOs/`**
  * `LoginDTO.cs`: Validação de entrada do login (`RF01`).
  * `GeneroDTO.cs`: Validação de cadastro e edição de gêneros (`RF11`).
  * `UsuarioDTO.cs`: Cadastro/edição de usuários pelo Admin (`RF13`, `RF14`).
  * `UsuarioAutenticacaoDTO.cs`: Edição do próprio perfil pelo usuário logado sem permitir alterar `Perfil` ou `Ativo` (`RF07`, `RN09`).
  * `UsuarioRespostaDTO.cs`: Record de retorno seguro omitindo `SenhaHash`.
  * `MusicaDTO.cs`: Cadastro manual e edição de músicas com `IValidatableObject` (`RF08`, `RF09`, `RN05` a `RN08`).
  * `MusicaImportacaoSpotify.cs`: Recebe `SpotifyTrackId`, `Descricao` (curiosidade/detalhe da música e faixa do álbum) e `IdGeneros` para importação automática (`RF08`).
  * `MusicRespostaDTO.cs`: Retorno formatado com dados da música, descrição detalhada, álbum, capa, data de lançamento, lista ordenada de artistas e gêneros (`RF03`, `RF06`).
  * `LetraRespostaDTO.cs`: Retorno contendo `MusicaId`, `Titulo`, `Artista`, `YoutubeVideoId`, `TemSincronia`, `LetraOriginal`, `TraducaoPortugues` e a lista `LinhasSincronizadas` (`TempoSegundos`, `TempoFormatado`, `Texto`).
* **`Interfaces/` & `Repositories/`**
  * `IGeneroRepository.cs` / `GeneroRepository.cs`: CRUD de gêneros e busca por lista de IDs.
  * `IUsuarioRepository.cs` / `UsuarioRepository.cs`: CRUD de usuários, hash automático de senha, bloqueio de login para inativos (`RN04`) e desativação lógica (`RF15`).
  * `IMusicaRepository.cs` / `MusicaRepository.cs`: Listagem paginada com busca por título (`RF05`) e filtro por gênero (`RF04`), exclusão lógica (`RF10`) e deduplicação automática de artistas e álbuns por `spotify_id` ou nome (`ObterOuCriarArtistaAsync` e `ObterOuCriarAlbumAsync` — `RN10`).
* **`Services/`**
  * **`SpotifyService.cs`**:
    * Autenticação na Spotify Web API via *Client Credentials Flow* com cache do token em memória.
    * `BuscarFaixasAsync`: Pesquisa faixas no Spotify (`GET /v1/search?type=track`) e retorna uma lista limpa de `SpotifyBuscaItemDTO` (`SpotifyTrackId`, `Titulo`, `Artistas`, `Album`, `CapaUrl`, `DuracaoMs`).
    * `ObterFaixaPorIdAsync`: Busca os metadados completos de uma faixa (`GET /v1/tracks/{id}`) e converte a data de lançamento do álbum conforme a precisão (`year`, `month`, `day` — `RN08`).
  * **`LetraService.cs`**:
    * Cache em memória (`ConcurrentDictionary<int, LetraRespostaDTO>`) para respostas instantâneas após a 1ª consulta.
    * Execução paralela (`Task.WhenAll`) buscando simultaneamente na **LRCLIB** e no **YouTube** para carregamento rápido (< 1s).
    * **Casamento Inteligente de Duração:** Usa `DuracaoMs` do banco (vindo do Spotify) para validar e selecionar na **LRCLIB** (`/api/get` e `/api/search`) e no **YouTube** (`"Provided to YouTube"` / `"official audio"`) exatamente a versão de estúdio com a mesma minutagem (tolerância de $\le 2$ segundos), garantindo sincronia da legenda em qualquer música do catálogo.
    * **Tradução Poética sob demanda com Google Gemini (`gemini-2.5-flash-lite`):** Método `ObterTraducaoAsync` que só consome a API do Gemini quando o usuário solicita a tradução. Detecta músicas 100% em português (`JA_EM_PORTUGUES`), traduz músicas estrangeiras mantendo sentido musical/gírias e também trata músicas mistas (português + inglês/outros idiomas).
* **`Controllers/`**
  * `LoginController.cs` (`POST /api/Login`): Autentica e gera o Token JWT com as *Claims* (`NameIdentifier`, `Name`, `Email`, `Role`).
  * `GeneroController.cs` (`/api/Genero`):
    * `GET /api/Genero` e `GET /api/Genero/{id}`: Públicas (`[AllowAnonymous]`) para visitantes filtrarem o catálogo.
    * `POST` e `PUT`: Restritas a `ADMIN` (`RN11`).
  * `UsuarioController.cs` (`/api/Usuario`): Gestão completa de usuários para `ADMIN` (`RN03`) + rota `PUT /api/Usuario/meu-perfil` para o próprio usuário (`RF07`, `RN09`).
  * `SpotifyController.cs` (`GET /api/Spotify/buscar`): Busca faixas na API do Spotify retornando `SpotifyBuscaItemDTO` (restrito a `ADMIN`).
  * `MusicaController.cs` (`/api/Musica`):
    * **Rotas Públicas (`[AllowAnonymous]`):**
      * `GET /api/Musica`: Listagem paginada/filtrada do catálogo.
      * `GET /api/Musica/{id}`: Detalhes completos da música (álbum, capa, curiosidades/descrição, duração, artistas, gêneros).
      * `GET /api/Musica/{id}/letra`: Retorna o `YoutubeVideoId` e as `LinhasSincronizadas` em alta velocidade.
      * `GET /api/Musica/{id}/traducao`: Gera (ou busca do cache) a tradução contextual em português via Gemini sob demanda.
    * **Rotas Administrativas (`[Authorize(Roles = "ADMIN")]`):**
      * `POST /api/Musica/importar-spotify`: Importa automaticamente do Spotify pelo `SpotifyTrackId` somado à descrição rica (curiosidades + número da faixa no álbum) e gêneros.
      * `POST /api/Musica`: Cadastro manual.
      * `PUT /api/Musica/{id}`: Atualização de música.
      * `DELETE /api/Musica/{id}`: Exclusão lógica (`Ativo = false`).
* **`Program.cs`**
  * Configurado com injeção de dependência (`CatalogoContext`, `Repositories`, `SpotifyService`, `LetraService`), Autenticação JWT Bearer, política de CORS (`PermitirFrontEnd`), Swagger UI com suporte a Token Bearer (botão **Authorize**) e *Seeder* automático do primeiro usuário `ADMIN` (`admin@exemplo.com` / senha `admin1234`).

---

## 3. Checklist para rodar no computador do SENAI / Casa

### Passo 1: Executar o script do Banco de Dados
1. Abra o **SQL Server Management Studio (SSMS)**.
2. Abra e execute o arquivo `database/CatalogoMusicas.sql` para criar o banco `CatalogoMusicas`.

### Passo 2: Ajustar a Connection String no `appsettings.json`
No arquivo `appsettings.json`, estão prontas as strings `DefaultConnection` (Casa com Windows Auth) e `SenaiConnection` (SENAI com SQL Auth). No SENAI, copie o valor de `SenaiConnection` para `DefaultConnection`:

```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\MSSQLSERVER2;Database=CatalogoMusicas;User Id=sa;Password=Senai@134;TrustServerCertificate=True;"
  },
```

### Passo 3: Configurar as chaves do Spotify e do Gemini no User Secrets
> **Importante:** O arquivo `secrets.json` fica salvo em `%APPDATA%\Microsoft\UserSecrets\2e1cbdbd-4f6f-4ae9-913b-d6758d380fd9\secrets.json` e **não sobe para o GitHub** por segurança.

1. Clique com o botão direito no projeto **`CatalogoMusicas`** no Visual Studio $\rightarrow$ **Gerenciar Segredos do Usuário** (*Manage User Secrets*).
2. Configure o `ClientId` e `ClientSecret` do Spotify e a `ApiKey` do Google AI Studio (Gemini):
   ```json
   {
     "Spotify": {
       "ClientId": "SEU_CLIENT_ID_AQUI",
       "ClientSecret": "SEU_CLIENT_SECRET_AQUI"
     },
     "Gemini": {
       "ApiKey": "SUA_API_KEY_DO_GEMINI_AQUI"
     }
   }
   ```

---

## 4. Estado Atual do Front-end e Próximos Passos

O projeto Front-end em **React + TypeScript (Vite)** já foi criado em:
* **`C:\Users\52663088854\Desktop\catalogo-musicas-web`**

### O que já foi implementado e validado no Front-end:
* **`src/types/musica.ts`**: Interfaces TypeScript (`Genero`, `Musica`, `PaginacaoMusicas`, `LinhaSincronizada`, `LetraResposta`).
* **`src/services/api.ts`**: Instância do `axios` apontando para `https://localhost:7071/api` com interceptor de Token JWT.
* **`src/App.tsx` (Protótipo funcional validado):**
  * Integração com o **YouTube IFrame API** (`autoplay: 0` para não tocar sozinho sem o clique do usuário).
  * Sincronização em tempo real (`100ms`) destacando o verso atual de `linhasSincronizadas`.
  * Botão de **Tradução sob demanda** consumindo `GET /api/Musica/{id}/traducao`.

### Próximas Etapas do Front-end (UI Completa de Catálogo + Player):
1. **Home / Catálogo de Músicas (Pública):**
   * Barra de pesquisa por título (`RF05`) e filtro por pílulas de gênero (`RF04`).
   * Grid de cards das músicas exibindo capa do álbum, título, artistas e gêneros (`RF03`).
2. **Página/Modal de Detalhes da Música + Player Integrado (Pública):**
   * **Seção de Catálogo / Ficha Técnica:** Destaque para a capa do álbum, título do álbum, data de lançamento, **qual faixa do álbum ela é** e o card de **Curiosidades & História da Música** (`descricao`).
   * **Seção de Player & Letras:** Controles de Play/Pause para ouvir a faixa completa quando o usuário desejar, acompanhada do painel de **Letra Sincronizada** e botão de alternância para a **Tradução em Português**.
3. **Autenticação e Área Logada (`LOGIN` / `USER` / `ADMIN`):**
   * Tela de Login (`POST /api/Login`) e edição do próprio perfil (`PUT /api/Usuario/meu-perfil`).
   * Painel Administrativo (`ADMIN`):
     * **Importar do Spotify:** Buscar música em `GET /api/Spotify/buscar`, preencher a **curiosidade/detalhe interessante + número da faixa no álbum** no campo de descrição, selecionar os gêneros e salvar em `POST /api/Musica/importar-spotify`.
     * **Gerenciar Gêneros e Usuários:** CRUDs administrativos (`/api/Genero` e `/api/Usuario`).
