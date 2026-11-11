using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;
using Castwork.DomainService.Precificacao;

using MediatR;

namespace Castwork.Application.Ferramentas.Modelo3d.Queries.GetConferenciaDoModelo;

/// <summary>
/// Conferência de peso e materiais da versão: mede o arquivo 3D atual (a mesma medição do preço, guardada pelo arquivo) e
/// lista os corpos com material, volume e peso, as sobreposições e os materiais do ateliê para escolher. Peso e material não
/// são dado de precificação: basta ler o projeto.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetConferenciaDoModeloQuery : IRequest<ConferenciaDoModeloDto>
{
    public long VersaoId { get; set; }
}

public sealed class GetConferenciaDoModeloQueryHandler(IUnitOfWork unitOfWork, IMedidorDoModelo medidor)
    : IRequestHandler<GetConferenciaDoModeloQuery, ConferenciaDoModeloDto>
{
    public async Task<ConferenciaDoModeloDto> Handle(GetConferenciaDoModeloQuery request, CancellationToken cancellationToken)
    {
        var status = await unitOfWork.GetRepository<Projeto>()
            .FindBy(p => p.Versoes.Any(v => v.Id == request.VersaoId))
            .SelectMany(p => p.Versoes.Where(v => v.Id == request.VersaoId).Select(v => (StatusVersao?)v.Status))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Versão", request.VersaoId);
        var alteravel = status == StatusVersao.EmDesenvolvimento;

        var atual = await PesoDoModelo.MedirArquivoAtualAsync(unitOfWork, medidor, request.VersaoId, cancellationToken);
        if (atual is null)
        {
            return new ConferenciaDoModeloDto { VersaoId = request.VersaoId, Alteravel = alteravel };
        }

        var materiais = atual.Materiais
            .Select(m => new MaterialDisponivelDto { Nome = m.Nome, Rotulo = m.Liga ?? m.Nome, Uso = PesoDoModelo.Uso(m.Nome), LigaId = m.LigaId })
            .ToList();
        if (atual.Medicao is null)
        {
            return new ConferenciaDoModeloDto { VersaoId = request.VersaoId, ArquivoId = atual.ArquivoId, Alteravel = alteravel, Materiais = materiais };
        }

        var modelo = PesoDoModelo.Classificar(atual.Medicao);
        var ligas = await NomesDasLigasAsync(atual, modelo, cancellationToken);
        var pesos = modelo.PesoPorLiga();
        var metais = modelo.Corpos.Where(c => c.Uso == UsoDoCorpo.Metal).ToDictionary(c => c.Nome, c => c.LigaId, StringComparer.Ordinal);

        return new ConferenciaDoModeloDto
        {
            VersaoId = request.VersaoId,
            ArquivoId = atual.ArquivoId,
            Medido = true,
            Alteravel = alteravel,
            PesoDoMetal = pesos.Sum(p => p.Peso.Gramas),
            PesosPorLiga = [.. pesos.Select(p => new PesoDaLigaDto { LigaId = p.LigaId, Liga = Nome(p.LigaId, ligas), Peso = p.Peso.Gramas })],
            Corpos = [.. atual.Medicao.Corpos.Zip(modelo.Corpos, (medido, corpo) => new CorpoConferidoDto
            {
                Nome = medido.Nome,
                Material = medido.Material,
                Uso = corpo.Uso,
                LigaId = corpo.LigaId,
                Liga = corpo.LigaId is { } ligaId ? Nome(ligaId, ligas) : null,
                VolumeMm3 = medido.VolumeMm3 is { } volume ? Math.Round(volume, 3) : null,
                MassaGramas = corpo.MassaGramas is { } massa ? Math.Round(massa, 3) : null,
                Fechado = corpo.Fechado,
                ContaNoPeso = corpo is { Uso: UsoDoCorpo.Metal, LigaId: not null, MassaGramas: not null },
            })],
            Sobreposicoes = [.. atual.Medicao.Sobreposicoes.Select(s => new SobreposicaoConferidaDto
            {
                CorpoA = s.CorpoA,
                CorpoB = s.CorpoB,
                VolumeMm3 = s.VolumeMm3,
                DeMetal = metais.ContainsKey(s.CorpoA) && metais.ContainsKey(s.CorpoB),
                PodeUnir = metais.TryGetValue(s.CorpoA, out var a) && metais.TryGetValue(s.CorpoB, out var b) && a == b,
            })],
            Materiais = materiais,
        };
    }

    private static string Nome(long ligaId, Dictionary<long, string> ligas) => ligas.TryGetValue(ligaId, out var nome) ? nome : $"Liga {ligaId}";

    /// <summary>
    /// As ligas ativas vêm com os materiais; a de um corpo com liga desativada é lida do banco.
    /// </summary>
    private async Task<Dictionary<long, string>> NomesDasLigasAsync(ModeloAtual atual, ModeloMedido modelo, CancellationToken cancellationToken)
    {
        var ligas = atual.Materiais.Where(m => m is { LigaId: not null, Liga: not null }).ToDictionary(m => m.LigaId!.Value, m => m.Liga!);
        var faltam = modelo.Corpos.Select(c => c.LigaId).OfType<long>().Where(id => !ligas.ContainsKey(id)).Distinct().ToList();
        if (faltam.Count > 0)
        {
            var desativadas = await unitOfWork.GetRepository<Liga>()
                .FindBy(l => faltam.Contains(l.Id))
                .Select(l => new { l.Id, l.Nome })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            foreach (var liga in desativadas)
            {
                ligas[liga.Id] = liga.Nome;
            }
        }

        return ligas;
    }
}
