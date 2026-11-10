namespace Castwork.Domain.Entities.Assistente;

/// <summary>
/// As vistas das imagens guardadas de um arquivo: as quatro do modelo 3D e a única do desenho 2D. A ordem de
/// <see cref="Todas"/> é a de mostrar.
/// </summary>
public static class VistaDaImagem
{
    public const string Iso = "iso";
    public const string Frente = "frente";
    public const string Topo = "topo";
    public const string Lado = "lado";
    public const string Desenho2d = "2d";

    public const int MaxLength = 10;

    public static IReadOnlyList<string> Todas { get; } = [Iso, Frente, Topo, Lado, Desenho2d];

    public static bool EhValida(string? vista) => vista is not null && Todas.Contains(vista);

    /// <summary>
    /// Posição para ordenar (vista desconhecida vai para o fim).
    /// </summary>
    public static int Ordem(string vista)
    {
        var indice = Todas.ToList().IndexOf(vista);
        return indice < 0 ? int.MaxValue : indice;
    }
}

/// <summary>
/// Os tipos de imagem guardada: PNG (o render) ou SVG (a exportação vetorial do 2D), pelo conteúdo.
/// </summary>
public static class TipoDaImagemGuardada
{
    public const string Png = "image/png";
    public const string Svg = "image/svg+xml";

    public const int MaxLength = 20;

    public static string? Detectar(byte[] conteudo)
    {
        if (conteudo is [0x89, 0x50, 0x4E, 0x47, ..])
        {
            return Png;
        }

        var inicio = System.Text.Encoding.UTF8.GetString(conteudo, 0, Math.Min(conteudo.Length, 4096)).TrimStart('﻿', ' ', '\t', '\r', '\n');
        return inicio.StartsWith('<') && inicio.Contains("<svg", StringComparison.OrdinalIgnoreCase) ? Svg : null;
    }
}
