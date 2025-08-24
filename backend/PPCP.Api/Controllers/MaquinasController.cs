using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPCP.Api.Data;
using PPCP.Api.Models;

namespace PPCP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaquinasController : ControllerBase
    {
        private readonly PPCPContext _context;

        public MaquinasController(PPCPContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetMaquinas()
        {
            var maquinas = await _context.Maquinas
                .Select(m => new
                {
                    m.Id,
                    m.Codigo,
                    m.Descricao,
                    m.CapacidadeNominal_uh,
                    m.EficienciaAlvo_pct,
                    m.Ativa,
                    EstadoAtual = _context.Telemetrias
                        .Where(t => t.MaquinaId == m.Id)
                        .OrderByDescending(t => t.Timestamp)
                        .Select(t => t.Estado)
                        .FirstOrDefault(),
                    UltimaTelemetria = _context.Telemetrias
                        .Where(t => t.MaquinaId == m.Id)
                        .OrderByDescending(t => t.Timestamp)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(maquinas);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Maquina>> GetMaquina(int id)
        {
            var maquina = await _context.Maquinas.FindAsync(id);
            if (maquina == null) return NotFound();
            return maquina;
        }

        [HttpPost]
        public async Task<ActionResult<Maquina>> PostMaquina(Maquina maquina)
        {
            _context.Maquinas.Add(maquina);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMaquina), new { id = maquina.Id }, maquina);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutMaquina(int id, Maquina maquina)
        {
            if (id != maquina.Id) return BadRequest();

            _context.Entry(maquina).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Maquinas.Any(e => e.Id == id)) return NotFound();
                throw;
            }

            return NoContent();
        }
    }
}
