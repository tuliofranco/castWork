/**
 * Nome de arquivo a partir do nome que o joalheiro deu ("Visão 2d" → "visao-2d.dxf"): sem acentos, sem
 * espaços e só com caracteres seguros em qualquer sistema. Vazio vira `padrao`.
 */
export function nomeDeArquivo(nome: string | null | undefined, extensao: string, padrao = 'arquivo'): string {
  const base = (nome ?? '')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9._-]+/g, '-')
    .replace(/-{2,}/g, '-');
  return `${aparar(base) || padrao}.${extensao.replace(/^\./, '')}`;
}

/** Tira hífens e pontos das pontas (sem regex: a de pontas tem backtracking). */
function aparar(texto: string): string {
  let inicio = 0;
  let fim = texto.length;
  while (inicio < fim && '-.'.includes(texto[inicio])) inicio++;
  while (fim > inicio && '-.'.includes(texto[fim - 1])) fim--;
  return texto.slice(inicio, fim);
}
