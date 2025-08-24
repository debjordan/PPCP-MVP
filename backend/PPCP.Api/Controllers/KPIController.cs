using Microsoft.AspNetCore.Mvc;
using PPCP.Api.Models;
using PPCP.Api.Services;

namespace PPCP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KPIController : ControllerBase
    {
        private readonly KPIService _kpiService;

        public KPIController(KPIService kpiService)
        {
            _kpiService = kpiService;
        }

        [HttpGet("oee")]
        public async Task<ActionResult<object>> GetOEE(
            [FromQuery] int? maquinaId,
            [FromQuery] string window = "shift",
            [FromQuery] DateTime? inicio = null,
            [FromQuery] DateTime? fim = null)
        {
            var agora = DateTime.UtcNow;
            var periodoInicio = inicio ?? window switch
            {
                "shift" => agora.Date.AddHours(6), // Início turno manhã
                "day" => agora.Date,
                "op" => agora.AddHours(-8), // Últimas 8h por padrão
                _ => agora.AddHours(-8)
            };

            var periodoFim = fim ?? agora;

            if (maquinaId.HasValue)
            {
                var kpi = await _kpiService.CalcularOEE(maquinaId.Value, periodoInicio, periodoFim);
                return Ok(kpi);
            }
            else
            {
                var kpis = await _kpiService.CalcularOEETodasMaquinas(periodoInicio, periodoFim);
                return Ok(kpis);
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<object>> GetDashboard()
        {
            var agora = DateTime.UtcNow;
            var inicioTurno = agora.Date.AddHours(agora.Hour < 14 ? 6 : agora.Hour < 22 ? 14 : 22);
            var fimTurno = inicioTurno.AddHours(8);

            var kpis = await _kpiService.CalcularOEETodasMaquinas(inicioTurno, agora);

            var dashboard = new
            {
                Periodo = new { Inicio = inicioTurno, Fim = agora },
                KPIs = kpis,
                Resumo = new
                {
                    OEEMedio = kpis.Any() ? kpis.Average(k => k.OEE) : 0,
                    DisponibilidadeMedia = kpis.Any() ? kpis.Average(k => k.Disponibilidade) : 0,
                    PerformanceMedia = kpis.Any() ? kpis.Average(k => k.Performance) : 0,
                    QualidadeMedia = kpis.Any() ? kpis.Average(k => k.Qualidade) : 0,
                    MaquinasRodando = kpis.Count(k => k.OEE > 0),
                    TotalMaquinas = kpis.Count
                }
            };

            return Ok(dashboard);
        }
    }
}
