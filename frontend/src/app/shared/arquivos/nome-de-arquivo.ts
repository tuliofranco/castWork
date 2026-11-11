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

/**
 * Nome do arquivo baixado com o nome da peça como o joalheiro escreveu ("Elo Infinito 2,5 cm" → "Elo Infinito 2,5 cm.dxf";
 * com `complemento`, "Elo Infinito 2,5 cm - Iso.png"): mantém acentos, espaços e vírgulas e só tira o que nenhum sistema
 * aceita em nome de arquivo (`<>:"/\|?*` e caracteres de controle), os espaços repetidos e os pontos e espaços das pontas.
 * Vazio vira `padrao`.
 */
export function nomeDeArquivoDaPeca(nome: string | null | undefined, extensao: string, complemento?: string, padrao = 'peca'): string {
  const limpar = (texto: string) =>
    [...texto]
      .map((c) => (c.charCodeAt(0) < 32 || '<>:"/\\|?*'.includes(c) ? '' : c))
      .join('')
      .replace(/\s+/g, ' ')
      .trim();
  const base = aparar(limpar(nome ?? ''), ' .') || padrao;
  const extra = complemento ? aparar(limpar(complemento), ' .') : '';
  const nomeBase = extra ? `${base} - ${extra}` : base;
  return `${nomeBase}.${extensao.replace(/^\./, '')}`;
}

/** Tira das pontas os caracteres de `pontas` (hífens e pontos; sem regex: a de pontas tem backtracking). */
function aparar(texto: string, pontas = '-.'): string {
  let inicio = 0;
  let fim = texto.length;
  while (inicio < fim && pontas.includes(texto[inicio])) inicio++;
  while (fim > inicio && pontas.includes(texto[fim - 1])) fim--;
  return texto.slice(inicio, fim);
}
