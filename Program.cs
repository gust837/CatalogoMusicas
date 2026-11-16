using System.Text;
using CatalogoMusicas.BdContextCatalogo;
using CatalogoMusicas.Interfaces;
using CatalogoMusicas.Repositories;
using CatalogoMusicas.Services;
using CatalogoMusicas.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Configure a DefaultConnection no appsettings.json")));

builder.Services.AddScoped<IGeneroRepository, GeneroRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IMusicaRepository, MusicaRepository>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.AddHttpClient();
builder.Services.AddSingleton(sp => new SpotifyService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    sp.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<LetraService>();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Configuração Jwt não encontrada.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontEnd", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CatalogoMusicas.WebApi",
        Version = "v1",
        Description = "API de Catálogo de Músicas integrado à Spotify Web API (Padrão SENAI)."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Cole apenas o seu token JWT aqui."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CatalogoContext>();
    if (!context.Usuarios.Any())
    {
        context.Usuarios.Add(new CatalogoMusicas.Models.Usuario
        {
            Nome = "Administrador",
            Email = "admin@exemplo.com",
            SenhaHash = Criptografia.GerarHash("admin1234"),
            Perfil = "ADMIN",
            Ativo = true,
            CriadoEm = DateTime.Now
        });
        context.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CatalogoMusicas.WebApi v1");
        options.RoutePrefix = string.Empty;
    });
    app.MapGet("/swagger", () => Results.Redirect("/"));
}

app.UseHttpsRedirection();

app.UseCors("PermitirFrontEnd");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();