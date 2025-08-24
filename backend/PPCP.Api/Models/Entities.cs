using System.ComponentModel.DataAnnotations;

namespace PPCP.Api.Models
{
    public class Produto
    {
        public int Id { get; set; }
        [Required]
        public string Codigo { get; set; } = string.Empty;
        [Required]
        public string Descricao { get; set; } = string.Empty;
        public double CicloIdeal_s { get; set; } // tempo ciclo ideal em segundos
        public double RendimentoEsperado_pct { get; set; } // % rendimento esperado
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public ICollection<OrdemProducao> OrdensProducao { get; set; } = new List<OrdemProducao>();
    }

    public class Maquina
    {
        public int Id { get; set; }
        [Required]
        public string Codigo { get; set; } = string.Empty;
        [Required]
        public string Descricao { get; set; } = string.Empty;
        public double CapacidadeNominal_uh { get; set; } // unidades por hora
        public double EficienciaAlvo_pct { get; set; } // % eficiência alvo
        public bool Ativa { get; set; } = true;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;


        public ICollection<OrdemProducao> OrdensProducao { get; set; } = new List<OrdemProducao>();
        public ICollection<Evento> Eventos { get; set; } = new List<Evento>();
        public ICollection<Telemetria> Telemetrias { get; set; } = new List<Telemetria>();
    }

    public class Turno
    {
        public int Id { get; set; }
        [Required]
        public string Nome { get; set; } = string.Empty;
        public TimeSpan Inicio { get; set; }
        public TimeSpan Fim { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }

    public enum StatusOP
    {
        Planejada = 0,
        EmProducao = 1,
        Concluida = 2,
        Cancelada = 3
    }

    public class OrdemProducao
    {
        public int Id { get; set; }
        [Required]
        public string Numero { get; set; } = string.Empty;
        public int ProdutoId { get; set; }
        public Produto Produto { get; set; } = null!;
        public int MaquinaId { get; set; }
        public Maquina Maquina { get; set; } = null!;
        public int QuantidadePlanejada { get; set; }
        public int QuantidadeBoa { get; set; } = 0;
        public int QuantidadeTotal { get; set; } = 0;
        public DateTime Prazo { get; set; }
        public StatusOP Status { get; set; } = StatusOP.Planejada;
        public DateTime? InicioReal { get; set; }
        public DateTime? FimReal { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;


        public double PercentualConclusao => QuantidadePlanejada > 0 ?
            (double)QuantidadeBoa / QuantidadePlanejada * 100 : 0;

        public DateTime? ETA { get; set; } // Estimativa de conclusão
        public bool EmRisco { get; set; } = false;
    }

    public enum EstadoMaquina
    {
        RUNNING = 0,
        IDLE = 1,
        SETUP = 2,
        PLANNED_STOP = 3,
        UNPLANNED_STOP = 4,
        DOWN = 5
    }

    public enum TipoEvento
    {
        InicioOP = 0,
        FimOP = 1,
        InicioParada = 2,
        FimParada = 3,
        TrocaTurno = 4,
        Setup = 5,
        MudancaVelocidade = 6
    }

    public class Evento
    {
        public int Id { get; set; }
        public int MaquinaId { get; set; }
        public Maquina Maquina { get; set; } = null!;
        public int? OrdemProducaoId { get; set; }
        public OrdemProducao? OrdemProducao { get; set; }
        public TipoEvento Tipo { get; set; }
        public string? Motivo { get; set; }
        public DateTime TsInicio { get; set; }
        public DateTime? TsFim { get; set; }
        public string? Atributos { get; set; } // JSON para dados extras
    }

    public class Telemetria
    {
        public int Id { get; set; }
        public int MaquinaId { get; set; }
        public Maquina Maquina { get; set; } = null!;
        public int? OrdemProducaoId { get; set; }
        public OrdemProducao? OrdemProducao { get; set; }
        public DateTime Timestamp { get; set; }
        public EstadoMaquina Estado { get; set; }
        public int TotalCount { get; set; }
        public int GoodCount { get; set; }
        public int ScrapCount { get; set; }
        public double Speed_uph { get; set; } // velocidade atual em un/h
    }

    public class KPIResult
    {
        public double OEE { get; set; }
        public double Disponibilidade { get; set; }
        public double Performance { get; set; }
        public double Qualidade { get; set; }
        public DateTime PeriodoInicio { get; set; }
        public DateTime PeriodoFim { get; set; }
        public int MaquinaId { get; set; }
        public string MaquinaCodigo { get; set; } = string.Empty;
    }
}
