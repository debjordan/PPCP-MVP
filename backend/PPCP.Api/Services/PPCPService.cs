using Microsoft.EntityFrameworkCore;
using PPCP.Api.Data;
using PPCP.Api.Models;

namespace PPCP.Api.Services
{
    public class PPCPService
    {
        private readonly PPCPContext _context;

        public PPCPService(PPCPContext context)
        {
            _context = context;
        }

        public async Task<OrdemProducao> CriarOP(OrdemProducao op)
        {
            // Gerar número sequencial
            var ultimaOP = await _context.OrdensProducao
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            var numeroSequencial = (ultimaOP?.Id ?? 0) + 1;
            op.Numero = $"OP-{numeroSequencial:D6}";
            op.Status = StatusOP.Planejada;
            op.CriadoEm = DateTime.UtcNow;

            _context.OrdensProducao.Add(op);
            await _context.SaveChangesAsync();

            return op;
        }

        public async Task<List<OrdemProducao>> ObterFilaPorMaquina(int maquinaId)
        {
            return await _context.OrdensProducao
                .Include(o => o.Produto)
                .Include(o => o.Maquina)
                .Where(o => o.MaquinaId == maquinaId &&
                           (o.Status == StatusOP.Planejada || o.Status == StatusOP.EmProducao))
                .OrderBy(o => o.CriadoEm) // FIFO
                .ToListAsync();
        }

        public async Task IniciarProximaOP(int maquinaId)
        {
            var proximaOP = await _context.OrdensProducao
                .Where(o => o.MaquinaId == maquinaId && o.Status == StatusOP.Planejada)
                .OrderBy(o => o.CriadoEm)
                .FirstOrDefaultAsync();

            if (proximaOP != null)
            {
                proximaOP.Status = StatusOP.EmProducao;
                proximaOP.InicioReal = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Registrar evento
                var evento = new Evento
                {
                    MaquinaId = maquinaId,
                    OrdemProducaoId = proximaOP.Id,
                    Tipo = TipoEvento.InicioOP,
                    TsInicio = DateTime.UtcNow
                };
                _context.Eventos.Add(evento);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ConcluirOP(int opId)
        {
            var op = await _context.OrdensProducao.FindAsync(opId);
            if (op != null && op.Status == StatusOP.EmProducao)
            {
                op.Status = StatusOP.Concluida;
                op.FimReal = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Registrar evento
                var evento = new Evento
                {
                    MaquinaId = op.MaquinaId,
                    OrdemProducaoId = opId,
                    Tipo = TipoEvento.FimOP,
                    TsInicio = DateTime.UtcNow
                };
                _context.Eventos.Add(evento);
                await _context.SaveChangesAsync();

                // Iniciar próxima OP na mesma máquina
                await IniciarProximaOP(op.MaquinaId);
            }
        }

        public async Task AtualizarETA(int opId)
        {
            var op = await _context.OrdensProducao
                .Include(o => o.Produto)
                .FirstOrDefaultAsync(o => o.Id == opId);

            if (op == null || op.Status != StatusOP.EmProducao) return;

            var remanescente = op.QuantidadePlanejada - op.QuantidadeBoa;
            if (remanescente <= 0)
            {
                await ConcluirOP(opId);
                return;
            }

            // Calcular taxa efetiva baseada na telemetria recente
            var agora = DateTime.UtcNow;
            var inicioCalculo = op.InicioReal ?? agora.AddHours(-1);

            var telemetrias = await _context.Telemetrias
                .Where(t => t.OrdemProducaoId == opId && t.Timestamp >= inicioCalculo)
                .OrderBy(t => t.Timestamp)
                .ToListAsync();

            if (telemetrias.Count >= 2)
            {
                var primeira = telemetrias.First();
                var ultima = telemetrias.Last();
                var tempDecorrido = (ultima.Timestamp - primeira.Timestamp).TotalHours;
                var pecasProduzidas = ultima.GoodCount - primeira.GoodCount;

                if (tempDecorrido > 0 && pecasProduzidas > 0)
                {
                    var taxaEfetiva = pecasProduzidas / tempDecorrido; // peças/hora
                    var horasRestantes = remanescente / taxaEfetiva;
                    op.ETA = agora.AddHours(horasRestantes);
                    op.EmRisco = op.ETA > op.Prazo;

                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
