/** O conteúdo do arquivo em base64, em pedaços (um `String.fromCharCode` só estouraria a pilha em arquivo grande). */
export async function emBase64(arquivo: Blob): Promise<string> {
  const bytes = new Uint8Array(await arquivo.arrayBuffer());
  let binario = '';
  for (let i = 0; i < bytes.length; i += 0x8000) {
    binario += String.fromCharCode(...bytes.subarray(i, i + 0x8000));
  }
  return btoa(binario);
}
