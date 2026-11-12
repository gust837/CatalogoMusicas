# Contexto do Projeto — Catálogo de Músicas com Spotify API (Padrão SENAI)

> **Documento de acompanhamento e contexto do projeto (Casa / SENAI).**
> Contém o resumo completo de tudo o que já foi implementado na Web API, como rodar o projeto nos computadores do SENAI e os próximos passos (Front-end).

---

## 1. O que já está 100% pronto (Back-end Web API)

A **Web API em ASP.NET Core (.NET 10.0)** está completa, compilando sem erros ou avisos (`0 Aviso(s), 0 Erro(s)`) e seguindo o padrão arquitetural do SENAI (baseado no projeto `Plot.WebApi`):

### Estrutura implementada no projeto:
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
  * `MusicaImportacaoSpotify.cs`: Recebe `SpotifyTrackId`, `Descricao` e `IdGeneros` para importação automática (`RF08`).
  * `MusicRespostaDTO.cs`: Retorno formatado com dados da música, álbum, lista ordenada de artistas e gêneros (`RF03`, `RF06`).
* **`Interfaces/` & `Repositories/`**
  * `IGeneroRepository.cs` / `GeneroRepository.cs`: CRUD de gêneros e busca por lista de IDs.
  * `IUsuarioRepository.cs` / `UsuarioRepository.cs`: CRUD de usuários, hash automático de senha, bloqueio de login para inativos (`RN04`) e desativação lógica (`RF15`).
  * `IMusicaRepository.cs` / `MusicaRepository.cs`: Listagem paginada com busca por título (`RF05`) e filtro por gênero (`RF04`), exclusão lógica (`RF10`) e deduplicação automática de artistas e álbuns por `spotify_id` ou nome (`ObterOuCriarArtistaAsync` e `ObterOuCriarAlbumAsync` — `RN10`).
* **`Services/SpotifyService.cs`**
  * Autenticação na Spotify Web API via *Client Credentials Flow* com cache do token em memória.
  * `BuscarFaixasAsync`: Pesquisa faixas no Spotify (`GET /v1/search?type=track`).
  * `ObterFaixaPorIdAsync`: Busca os metadados completos de uma faixa (`GET /v1/tracks/{id}`) e converte a data de lançamento do álbum conforme a precisão (`year`, `month`, `day` — `RN08`).
* **`Controllers/`**
  * `LoginController.cs` (`POST /api/Login`): Autentica e gera o Token JWT com as *Claims* (`NameIdentifier`, `Name`, `Email`, `Role`).
  * `GeneroController.cs` (`/api/Genero`): Listagem para usuários autenticados e criação/edição restrita a `ADMIN` (`RN11`).
  * `UsuarioController.cs` (`/api/Usuario`): Gestão completa de usuários para `ADMIN` (`RN03`) + rota `PUT /api/Usuario/meu-perfil` para o próprio usuário (`RF07`, `RN09`).
  * `SpotifyController.cs` (`GET /api/Spotify/buscar`): Busca faixas na API do Spotify (restrito a `ADMIN`).
  * `MusicaController.cs` (`/api/Musica`): Listagem paginada/filtrada, detalhes, importação via Spotify (`POST /api/Musica/importar-spotify`), cadastro manual (`POST /api/Musica`), edição (`PUT`) e exclusão lógica (`DELETE`).
* **`Program.cs`**
  * Configurado com injeção de dependência (`DbContext`, `Repositories`, `SpotifyService`), Autenticação JWT Bearer, política de CORS (`PermitirFrontEnd`), Swagger UI com suporte a Token Bearer (botão **Authorize**) e *Seeder* automático do primeiro usuário `ADMIN` (`admin@exemplo.com` / senha `admin1234`).

---

## 2. Checklist para rodar no computador do SENAI

Quando você abrir este projeto na máquina do SENAI, siga estes **3 passos rápidos**:

### Passo 1: Executar o script do Banco de Dados
1. Abra o **SQL Server Management Studio (SSMS)** no PC do SENAI.
2. Abra e execute o arquivo `database/CatalogoMusicas.sql` para criar o banco `CatalogoMusicas` na instância do SENAI.

### Passo 2: Ajustar a Connection String no `appsettings.json`
No arquivo `appsettings.json`, já deixamos as duas strings prontas (`DefaultConnection` para Casa com Windows Auth e `SenaiConnection` para o SENAI com SQL Auth).
No SENAI, basta copiar o valor de `SenaiConnection` para `DefaultConnection` (conferindo o nome do servidor da máquina do laboratório):

```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\MSSQLSERVER2;Database=CatalogoMusicas;User Id=sa;Password=Senai@134;TrustServerCertificate=True;"
  },
```

### Passo 3: Configurar as chaves do Spotify no User Secrets do PC do SENAI
> **Importante:** O arquivo `secrets.json` (User Secrets do Visual Studio) fica salvo numa pasta local do Windows (`%APPDATA%\Microsoft\UserSecrets`) e **não sobe para o GitHub** por segurança.

Por isso, ao clonar/abrir o projeto no PC do SENAI:
1. Clique com o botão direito no projeto **`CatalogoMusicas`** no Visual Studio $\rightarrow$ **Gerenciar Segredos do Usuário** (*Manage User Secrets*).
2. Cole o seu `ClientId` e `ClientSecret` do Spotify Developer Dashboard:
   ```json
   {
     "Spotify:ClientId": "SEU_CLIENT_ID_AQUI",
     "Spotify:ClientSecret": "SEU_CLIENT_SECRET_AQUI"
   }
   ```

### Passo 4: Rodar e Logar no Swagger
1. Aperte **F5** para rodar a API.
2. O *Seeder* do `Program.cs` criará automaticamente o usuário administrador inicial caso o banco esteja vazio:
   * **E-mail:** `admin@exemplo.com`
   * **Senha:** `admin1234`
3. Faça login na rota `POST /api/Login`, copie o `token` retornado, clique no botão **Authorize** no topo do Swagger e cole o token.

---

## 3. O que ainda falta fazer no Projeto Geral

Com o Banco de Dados e o Back-end (API) concluídos, as próximas etapas do projeto são:

1. **Prototipação no Figma (UI/UX):**
   * Tela 1: Login (`RF01`).
   * Tela 2: Home / Catálogo de Músicas com barra de pesquisa (`RF05`), filtro por pílulas de gênero (`RF04`), cards de músicas (`RF03`) e barra fixa/modal com o **Player Spotify Embed** (`<iframe src="https://open.spotify.com/embed/track/{spotifyId}">` — `RF06`).
   * Tela 3: Modal/Tela de Perfil do Usuário (`RF07`).
   * Tela 4 (Admin): Cadastrar / Importar Música buscando direto na rota `/api/Spotify/buscar` e salvando em `/api/Musica/importar-spotify` (`RF08`, `RF09`, `RF10`).
   * Tela 5 (Admin): Gerenciar Gêneros (`/api/Genero` — `RF11`) e Gerenciar Usuários (`/api/Usuario` — `RF12` a `RF15`).

2. **Desenvolvimento do Front-end (React + TypeScript):**
   * Criação do projeto React (`Vite` + `TypeScript`).
   * Configuração do `axios` enviando o cabeçalho `Authorization: Bearer <token>`.
   * Criação do contexto de Autenticação (guardando token e perfil `ADMIN`/`USER` e bloqueando rotas administrativas — `RNF06`).
   * Integração das telas com os endpoints da API e exibição do `<iframe src="https://open.spotify.com/embed/track/{spotifyId}">`.
