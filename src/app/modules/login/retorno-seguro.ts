/**
 * Evita "open redirect": `returnTo` vem da URL e qualquer um pode montar
 * /login?returnTo=https://site-falso. Só aceitamos caminhos internos.
 * `//dominio` e `/\dominio` são tratados por navegadores como URL de outro host.
 */
export function retornoSeguro(returnTo: string | null | undefined): string {
  if (!returnTo?.startsWith('/') || returnTo.startsWith('//') || returnTo.startsWith('/\\')) return '/';
  return returnTo.startsWith('/login') ? '/' : returnTo;
}
