/* =====================================================================
   Catálogo de Músicas (projeto de estudo) - SQL Server (T-SQL)

   Correções em relação à versão anterior:
   1. spotify_id agora é OPCIONAL (permite cadastro manual) e continua
      ÚNICO quando preenchido (índice único filtrado).
   2. Dados de exemplo incluem uma música REAL do Spotify para testar o player.
   3. View vw_musicas_sem_genero para o admin achar músicas sem gênero
      (o Spotify não informa gênero da faixa).
   4. Data de lançamento com coluna de PRECISÃO ('year', 'month', 'day'),
      igual ao campo release_date_precision da API.
   5. Colunas criado_em / atualizado_em para saber quando o dado foi
      importado/atualizado (os dados são uma CÓPIA do Spotify).
   6. Constraints com nome (facilita ALTER/DROP depois).
   7. Script pode ser executado várias vezes (recria as tabelas).

   ATENÇÃO: re-executar APAGA os dados das tabelas.
   ===================================================================== */

IF DB_ID(N'CatalogoMusicas') IS NULL
    CREATE DATABASE CatalogoMusicas;
GO

USE CatalogoMusicas;
GO

/* Remove objetos na ordem inversa das dependências */
DROP VIEW  IF EXISTS dbo.vw_musicas_sem_genero;
DROP TABLE IF EXISTS dbo.musica_generos;
DROP TABLE IF EXISTS dbo.musica_artistas;
DROP TABLE IF EXISTS dbo.musicas;
DROP TABLE IF EXISTS dbo.albuns;
DROP TABLE IF EXISTS dbo.artistas;
DROP TABLE IF EXISTS dbo.generos;
DROP TABLE IF EXISTS dbo.usuarios;
GO

/* ---------------------------------------------------------------------
   USUÁRIOS (login próprio do sistema)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.usuarios (
    id          INT IDENTITY(1,1) CONSTRAINT PK_usuarios PRIMARY KEY,
    nome        NVARCHAR(120) NOT NULL,
    email       NVARCHAR(255) NOT NULL CONSTRAINT UQ_usuarios_email UNIQUE,   -- RN01
    senha_hash  VARCHAR(255)  NOT NULL,                                       -- RNF02
    perfil      VARCHAR(10)   NOT NULL CONSTRAINT DF_usuarios_perfil DEFAULT 'USER',
    ativo       BIT           NOT NULL CONSTRAINT DF_usuarios_ativo  DEFAULT 1, -- RN04
    criado_em   DATETIME2(0)  NOT NULL CONSTRAINT DF_usuarios_criado DEFAULT SYSDATETIME(),
    CONSTRAINT CK_usuarios_perfil CHECK (perfil IN ('ADMIN', 'USER'))
);
GO

/* ---------------------------------------------------------------------
   GÊNEROS (cadastrados pelo admin - o Spotify não fornece gênero de faixa)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.generos (
    id    INT IDENTITY(1,1) CONSTRAINT PK_generos PRIMARY KEY,
    nome  NVARCHAR(60) NOT NULL CONSTRAINT UQ_generos_nome UNIQUE              -- RN11
);
GO

/* ---------------------------------------------------------------------
   ARTISTAS
   --------------------------------------------------------------------- */
CREATE TABLE dbo.artistas (
    id             INT IDENTITY(1,1) CONSTRAINT PK_artistas PRIMARY KEY,
    spotify_id     VARCHAR(22)   NULL,              -- NULL = cadastrado manualmente
    nome           NVARCHAR(200) NOT NULL,
    imagem_url     NVARCHAR(500) NULL,
    criado_em      DATETIME2(0)  NOT NULL CONSTRAINT DF_artistas_criado DEFAULT SYSDATETIME(),
    atualizado_em  DATETIME2(0)  NULL
);
GO

CREATE UNIQUE INDEX UX_artistas_spotify_id
    ON dbo.artistas (spotify_id) WHERE spotify_id IS NOT NULL;
GO

/* ---------------------------------------------------------------------
   ÁLBUNS
   O Spotify devolve release_date como "1975", "1975-10" ou "1975-10-31"
   e informa release_date_precision = 'year' | 'month' | 'day'.
   A aplicação completa a data (1975 -> 1975-01-01) e guarda a precisão,
   assim a tela sabe se deve mostrar só o ano.
   --------------------------------------------------------------------- */
CREATE TABLE dbo.albuns (
    id                        INT IDENTITY(1,1) CONSTRAINT PK_albuns PRIMARY KEY,
    spotify_id                VARCHAR(22)   NULL,
    titulo                    NVARCHAR(250) NOT NULL,
    data_lancamento           DATE          NULL,
    data_lancamento_precisao  VARCHAR(5)    NULL,
    capa_url                  NVARCHAR(500) NULL,
    criado_em                 DATETIME2(0)  NOT NULL CONSTRAINT DF_albuns_criado DEFAULT SYSDATETIME(),
    atualizado_em             DATETIME2(0)  NULL,
    CONSTRAINT CK_albuns_precisao CHECK (data_lancamento_precisao IN ('year', 'month', 'day')),
    CONSTRAINT CK_albuns_data     CHECK (data_lancamento >= '1900-01-01')        -- RN08
);
GO

CREATE UNIQUE INDEX UX_albuns_spotify_id
    ON dbo.albuns (spotify_id) WHERE spotify_id IS NOT NULL;
GO

/* ---------------------------------------------------------------------
   MÚSICAS
   --------------------------------------------------------------------- */
CREATE TABLE dbo.musicas (
    id             INT IDENTITY(1,1) CONSTRAINT PK_musicas PRIMARY KEY,         -- RN10
    spotify_id     VARCHAR(22)    NULL,             -- necessário para tocar via embed
    titulo         NVARCHAR(250)  NOT NULL,
    descricao      NVARCHAR(1000) NULL,             -- preenchida pelo admin (Spotify não tem)
    album_id       INT            NULL CONSTRAINT FK_musicas_albuns REFERENCES dbo.albuns(id),
    duracao_ms     INT            NOT NULL,         -- milissegundos, como a API retorna
    ativo          BIT            NOT NULL CONSTRAINT DF_musicas_ativo DEFAULT 1, -- exclusão lógica (RF10)
    criado_em      DATETIME2(0)   NOT NULL CONSTRAINT DF_musicas_criado DEFAULT SYSDATETIME(),
    atualizado_em  DATETIME2(0)   NULL,
    CONSTRAINT CK_musicas_duracao CHECK (duracao_ms > 0)                          -- RN07
);
GO

CREATE UNIQUE INDEX UX_musicas_spotify_id
    ON dbo.musicas (spotify_id) WHERE spotify_id IS NOT NULL;
GO

CREATE INDEX IX_musicas_titulo ON dbo.musicas (titulo);                         -- RF05
GO

/* Uma música pode ter vários artistas.
   Na importação: procure o artista pelo spotify_id; se não existir, crie;
   só depois insira aqui (evita erro de duplicidade). */
CREATE TABLE dbo.musica_artistas (
    musica_id   INT NOT NULL CONSTRAINT FK_ma_musicas  REFERENCES dbo.musicas(id) ON DELETE CASCADE,
    artista_id  INT NOT NULL CONSTRAINT FK_ma_artistas REFERENCES dbo.artistas(id),
    ordem       TINYINT NOT NULL CONSTRAINT DF_ma_ordem DEFAULT 1,  -- 1 = artista principal
    CONSTRAINT PK_musica_artistas PRIMARY KEY (musica_id, artista_id)
);
GO

/* Uma música pode ter vários gêneros (RN05: ao menos um, validado na aplicação) */
CREATE TABLE dbo.musica_generos (
    musica_id  INT NOT NULL CONSTRAINT FK_mg_musicas REFERENCES dbo.musicas(id) ON DELETE CASCADE,
    genero_id  INT NOT NULL CONSTRAINT FK_mg_generos REFERENCES dbo.generos(id),
    CONSTRAINT PK_musica_generos PRIMARY KEY (musica_id, genero_id)
);
GO

/* Músicas que ainda precisam que o admin escolha um gênero */
CREATE VIEW dbo.vw_musicas_sem_genero AS
SELECT m.id, m.titulo, m.spotify_id
FROM dbo.musicas m
WHERE m.ativo = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.musica_generos mg WHERE mg.musica_id = m.id);
GO

/* ---------------------------------------------------------------------
   DADOS DE EXEMPLO
   - "Exemplo": fictícios, sem spotify_id (testam listagem/telas, NÃO tocam).
   - Bohemian Rhapsody: IDs reais do Spotify (testa o player via embed).
   --------------------------------------------------------------------- */
INSERT INTO dbo.generos (nome) VALUES
    (N'Rock'), (N'Pop'), (N'Hip Hop'), (N'MPB'), (N'Sertanejo'), (N'Outros');

INSERT INTO dbo.artistas (spotify_id, nome) VALUES
    (NULL,                     N'Artista Exemplo 1'),
    (NULL,                     N'Artista Exemplo 2'),
    ('1dfeR4HaWDbWqFHLkxsg1d', N'Queen');

INSERT INTO dbo.albuns (spotify_id, titulo, data_lancamento, data_lancamento_precisao) VALUES
    (NULL, N'Álbum Exemplo',        '2020-01-01', 'year'),
    (NULL, N'A Night at the Opera', '1975-11-21', 'day');

INSERT INTO dbo.musicas (spotify_id, titulo, descricao, album_id, duracao_ms)
SELECT NULL, N'Música Exemplo 1', N'Faixa fictícia para testes.', id, 180000
FROM dbo.albuns WHERE titulo = N'Álbum Exemplo';

INSERT INTO dbo.musicas (spotify_id, titulo, descricao, album_id, duracao_ms)
SELECT NULL, N'Música Exemplo 2', N'Faixa fictícia com dois artistas.', id, 215000
FROM dbo.albuns WHERE titulo = N'Álbum Exemplo';

INSERT INTO dbo.musicas (spotify_id, titulo, descricao, album_id, duracao_ms)
SELECT '4u7EnebtmKWzUH433cf5Qv', N'Bohemian Rhapsody', N'Faixa real para testar o player.', id, 354320
FROM dbo.albuns WHERE titulo = N'A Night at the Opera';

INSERT INTO dbo.musica_artistas (musica_id, artista_id, ordem)
SELECT m.id, a.id, v.ordem
FROM (VALUES (N'Música Exemplo 1',  N'Artista Exemplo 1', 1),
             (N'Música Exemplo 2',  N'Artista Exemplo 1', 1),
             (N'Música Exemplo 2',  N'Artista Exemplo 2', 2),
             (N'Bohemian Rhapsody', N'Queen',             1)) v(musica, artista, ordem)
JOIN dbo.musicas  m ON m.titulo = v.musica
JOIN dbo.artistas a ON a.nome   = v.artista;

INSERT INTO dbo.musica_generos (musica_id, genero_id)
SELECT m.id, g.id
FROM (VALUES (N'Música Exemplo 1',  N'Rock'),
             (N'Música Exemplo 2',  N'Pop'),
             (N'Bohemian Rhapsody', N'Rock')) v(musica, genero)
JOIN dbo.musicas m ON m.titulo = v.musica
JOIN dbo.generos g ON g.nome   = v.genero;
GO

/* O primeiro ADMIN deve ser criado pela aplicação (senha com hash), ex.:
   INSERT INTO dbo.usuarios (nome, email, senha_hash, perfil)
   VALUES (N'Admin', N'admin@exemplo.com', '<HASH>', 'ADMIN');
*/

SELECT m.titulo, m.spotify_id, al.titulo AS album, a.nome AS artista, g.nome AS genero
FROM musicas m
LEFT JOIN albuns al         ON al.id = m.album_id
LEFT JOIN musica_artistas ma ON ma.musica_id = m.id
LEFT JOIN artistas a        ON a.id = ma.artista_id
LEFT JOIN musica_generos mg ON mg.musica_id = m.id
LEFT JOIN generos g         ON g.id = mg.genero_id;
SELECT * FROM vw_musicas_sem_genero;   -- deve voltar vazio