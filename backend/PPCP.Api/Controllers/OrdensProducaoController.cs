using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPCP.Api.Data;
using PPCP.Api.Models;
using PPCP.Api.Services;

namespace PPCP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdensProducaoController : ControllerBase
    {
        private readonly PPCPContext _context;
        private readonly PPCPService _ppcpService;

        public OrdensProducaoController(PPCPContext context, PPCPService ppcpService)
        {
            _context = context;
            _ppcpService = ppcpService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetOPs([FromQuery] int? maquinaId, [FromQuery] StatusOP? status)
        {
            var query = _context.OrdensProducao
                .Include(o => o.Produto)
                .Include(o => o.Maquina)
                .AsQueryable();

            if (maquinaId.HasValue)
                query = query.Where(o => o.MaquinaId == maquinaId.Value);

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            var ops = await query
                .Select(o => new
                {
                    o.Id,
                    o.Numero,
                    Produto = new { o.Produto.Id, o.Produto.Codigo, o.Produto.Descricao },
                    Maquina = new { o.Maquina.Id, o.Maquina.Codigo, o.Maquina.Descricao },
                    o.QuantidadePlanejada,
                    o.QuantidadeBoa,
                    o.QuantidadeTotal,
                    o.Prazo,
                    o.Status,
                    o.InicioReal,
                    o.FimReal,
                    o.ETA,
                    o.EmRisco,
                    PercentualConclusao = o.QuantidadePlanejada > 0 ? (double)o.QuantidadeBoa / o.QuantidadePlanejada * 100 : 0,
                    o.CriadoEm
                })
                .OrderBy(o => o.CriadoEm)
                .ToListAsync();

            return Ok(ops);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrdemProducao>> GetOP(int id)
        {
            var op = await _context.OrdensProducao
                .Include(o => o.Produto)
                .Include(o => o.Maquina)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (op == null) return NotFound();
            return op;
        }

        [HttpPost]
        public async Task<ActionResult<OrdemProducao>> PostOP(CreateOPDto dto)
        {
            var op = new OrdemProducao
            {
                ProdutoId = dto.ProdutoId,
                MaquinaId = dto.MaquinaId,
                QuantidadePlanejada = dto.QuantidadePlanejada,
                Prazo = dto.Prazo
            };

            var createdOP = await _ppcpService.CriarOP(op);


            var filaMaquina = await _ppcpService.ObterFilaPorMaquina(dto.MaquinaId);
            if (filaMaquina.Count == 1 && filaMaquina[0].Id == createdOP.Id)
            {
                await _ppcpService.IniciarProximaOP(dto.MaquinaId);
            }

            return CreatedAtAction(nameof(GetOP), new { id = createdOP.Id }, createdOP);
        }

        [HttpGet("fila/{maquinaId}")]
        public async Task<ActionResult<IEnumerable<OrdemProducao>>> GetFila(int maquinaId)
        {
            var fila = await _ppcpService.ObterFilaPorMaquina(maquinaId);
            return Ok(fila);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var op = await _context.OrdensProducao.FindAsync(id);
            if (op == null) return NotFound();

            if (dto.Status == StatusOP.EmProducao && op.Status == StatusOP.Planejada)
            {
                await _ppcpService.IniciarProximaOP(op.MaquinaId);
            }
            else if (dto.Status == StatusOP.Concluida && op.Status == StatusOP.EmProducao)
            {
                await _ppcpService.ConcluirOP(id);
            }
            else
            {
                op.Status = dto.Status;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }
    }

    public class CreateOPDto
    {
        public int ProdutoId { get; set; }
        public int MaquinaId { get; set; }
        public int QuantidadePlanejada { get; set; }
        public DateTime Prazo { get; set; }
    }

    public class UpdateStatusDto
    {
        public StatusOP Status { get; set; }
    }
}
