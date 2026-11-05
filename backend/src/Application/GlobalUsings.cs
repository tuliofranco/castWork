global using Microsoft.EntityFrameworkCore;

// Extensões do EF usadas em praticamente todo handler (ToListAsync, AnyAsync...).
// O Mapster fica fora de propósito: ele tem um IMapFrom<T> próprio que conflitaria com o nosso.
