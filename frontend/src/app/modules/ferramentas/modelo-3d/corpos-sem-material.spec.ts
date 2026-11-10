import { avisoDeCorposSemMaterial, corposSemMaterial } from './corpos-sem-material';

describe('corposSemMaterial', () => {
  it('deve apontar os corpos com o material Default do SolidCraft ou sem material', () => {
    const medida = {
      bodies: [
        { name: 'Aro', material: 'Castwork liga 2' },
        { name: 'Garra', material: 'Default' },
        { name: 'Guia' },
        { name: 'Brilhante', material: 'Pedra' },
      ],
    };

    expect(corposSemMaterial(medida)).toEqual(['Garra', 'Guia']);
  });

  it('deve não apontar nada quando a resposta não é a medida esperada', () => {
    expect(corposSemMaterial(null)).toEqual([]);
    expect(corposSemMaterial({ erro: 'sem projeto' })).toEqual([]);
  });
});

describe('avisoDeCorposSemMaterial', () => {
  it('deve contar no singular e no plural', () => {
    expect(avisoDeCorposSemMaterial(1)).toBe('1 corpo sem material');
    expect(avisoDeCorposSemMaterial(2)).toBe('2 corpos sem material');
  });
});
