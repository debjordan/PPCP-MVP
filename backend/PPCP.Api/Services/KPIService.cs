using Microsoft.EntityFrameworkCore;
using PPCP.Api.Data;
using PPCP.Api.Models;

namespace PPCP.Api.Services
{
    public class KPIService
    {
        private readonly PPCPContext _context;

        public KPIService(PPCPContext context)
        {
            _context = context;
        }

        public async Task<KPIResult> CalcularOEE(int maquinaId, DateTime inicio, DateTime fim)
        {
            var telemetrias = await _context.Telemetrias
                .Where(t => t.MaquinaId == maquinaId &&
                           t.Timestamp >= inicio &&
                           t.Timestamp <= fim)
                .OrderBy(t => t.Timestamp)
                .ToListAsync();

            if (!telemetrias.Any())
            {
                return new KPIResult
                {
                    MaquinaId = maquinaId,
                    PeriodoInicio = inicio,
                    PeriodoFim = fim
                };
            }

            var maquina = await _context.Maquinas.FindAsync(maquinaId);
            var maquinaCodigo = maquina?.Codigo ?? "";

            // Calcular tempos e contadores
            var totalMinutos = (fim - inicio).TotalMinutes;
            var tempoRodando = telemetrias.Count(t => t.Estado == EstadoMaquina.RUNNING) *
                              ((telemetrias.Count > 1) ? (fim - inicio).TotalMinutes / telemetrias.Count : 0);
            var tempoParadasProgramadas = telemetrias.Count(t => t.Estado == EstadoMaquina.PLANNED_STOP) *
                                        ((telemetrias.Count > 1) ? (fim - inicio).TotalMinutes / telemetrias.Count : 0);
            var tempoParadasNaoProgramadas = telemetrias.Count(t =>
                t.Estado == EstadoMaquina.UNPLANNED_STOP || t.Estado == EstadoMaquina.DOWN) *
                ((telemetrias.Count > 1) ? (fim - inicio).TotalMinutes / telemetrias.Count : 0);

            var ultimaTelemetria = telemetrias.LastOrDefault();
            var totalPecas = ultimaTelemetria?.TotalCount ?? 0;
            var pecasBoas = ultimaTelemetria?.GoodCount ?? 0;

            // Cálculos de KPI
            var tempoOperacao = tempoRodando;
            var tempoPlanejado = totalMinutos - tempoParadasProgramadas;

            var disponibilidade = tempoPlanejado > 0 ?
                (tempoPlanejado - tempoParadasNaoProgramadas) / tempoPlanejado : 0;

            var performance = 0.0;
            if (tempoOperacao > 0 && maquina != null)
            {
                var tempoCicloIdeal = 60.0 / maquina.CapacidadeNominal_uh; // minutos por peça
                var tempoIdealTotal = pecasBoas * tempoCicloIdeal;
                performance = tempoIdealTotal / tempoOperacao;
            }

            var qualidade = totalPecas > 0 ? (double)pecasBoas / totalPecas : 0;
            var oee = disponibilidade * performance * qualidade;

            return new KPIResult
            {
                OEE = Math.Round(oee * 100, 2),
                Disponibilidade = Math.Round(disponibilidade * 100, 2),
                Performance = Math.Round(performance * 100, 2),
                Qualidade = Math.Round(qualidade * 100, 2),
                PeriodoInicio = inicio,
                PeriodoFim = fim,
                MaquinaId = maquinaId,
                MaquinaCodigo = maquinaCodigo
            };
        }

        public async Task<List<KPIResult>> CalcularOEETodasMaquinas(DateTime inicio, DateTime fim)
        {
            var maquinas = await _context.Maquinas.Where(m => m.Ativa).ToListAsync();
            var results = new List<KPIResult>();

            foreach (var maquina in maquinas)
            {
                var kpi = await CalcularOEE(maquina.Id, inicio, fim);
                results.Add(kpi);
            }

            return results;
        }
    }
}
