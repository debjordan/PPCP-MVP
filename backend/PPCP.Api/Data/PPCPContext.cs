using Microsoft.EntityFrameworkCore;
using PPCP.Api.Models;

namespace PPCP.Api.Data
{
    public class PPCPContext : DbContext
    {
        public PPCPContext(DbContextOptions<PPCPContext> options) : base(options)
        {
        }

        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Maquina> Maquinas { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<OrdemProducao> OrdensProducao { get; set; }
        public DbSet<Evento> Eventos { get; set; }
        public DbSet<Telemetria> Telemetrias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrdemProducao>()
                .HasOne(o => o.Produto)
                .WithMany(p => p.OrdensProducao)
                .HasForeignKey(o => o.ProdutoId);

            modelBuilder.Entity<OrdemProducao>()
                .HasOne(o => o.Maquina)
                .WithMany(m => m.OrdensProducao)
                .HasForeignKey(o => o.MaquinaId);

            modelBuilder.Entity<Evento>()
                .HasOne(e => e.Maquina)
                .WithMany(m => m.Eventos)
                .HasForeignKey(e => e.MaquinaId);

            modelBuilder.Entity<Telemetria>()
                .HasOne(t => t.Maquina)
                .WithMany(m => m.Telemetrias)
                .HasForeignKey(t => t.MaquinaId);

            modelBuilder.Entity<Telemetria>()
                .HasIndex(t => new { t.MaquinaId, t.Timestamp });

            modelBuilder.Entity<Evento>()
                .HasIndex(e => new { e.MaquinaId, e.TsInicio });

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Produto>().HasData(
                new Produto { Id = 1, Codigo = "PROD-001", Descricao = "Produto A", CicloIdeal_s = 15.0, RendimentoEsperado_pct = 98.5 },
                new Produto { Id = 2, Codigo = "PROD-002", Descricao = "Produto B", CicloIdeal_s = 12.0, RendimentoEsperado_pct = 97.8 },
                new Produto { Id = 3, Codigo = "PROD-003", Descricao = "Produto C", CicloIdeal_s = 20.0, RendimentoEsperado_pct = 99.2 },
                new Produto { Id = 4, Codigo = "PROD-004", Descricao = "Produto D", CicloIdeal_s = 8.0, RendimentoEsperado_pct = 96.5 },
                new Produto { Id = 5, Codigo = "PROD-005", Descricao = "Produto E", CicloIdeal_s = 25.0, RendimentoEsperado_pct = 98.9 }
            );

            modelBuilder.Entity<Maquina>().HasData(
                new Maquina { Id = 1, Codigo = "MAQ-001", Descricao = "Torno CNC 1", CapacidadeNominal_uh = 240, EficienciaAlvo_pct = 85.0 },
                new Maquina { Id = 2, Codigo = "MAQ-002", Descricao = "Torno CNC 2", CapacidadeNominal_uh = 240, EficienciaAlvo_pct = 85.0 },
                new Maquina { Id = 3, Codigo = "MAQ-003", Descricao = "Fresadora 1", CapacidadeNominal_uh = 180, EficienciaAlvo_pct = 80.0 },
                new Maquina { Id = 4, Codigo = "MAQ-004", Descricao = "Prensa 1", CapacidadeNominal_uh = 450, EficienciaAlvo_pct = 90.0 },
                new Maquina { Id = 5, Codigo = "MAQ-005", Descricao = "Solda 1", CapacidadeNominal_uh = 144, EficienciaAlvo_pct = 82.0 }
            );

            modelBuilder.Entity<Turno>().HasData(
                new Turno { Id = 1, Nome = "Manhã", Inicio = new TimeSpan(6, 0, 0), Fim = new TimeSpan(14, 0, 0) },
                new Turno { Id = 2, Nome = "Tarde", Inicio = new TimeSpan(14, 0, 0), Fim = new TimeSpan(22, 0, 0) },
                new Turno { Id = 3, Nome = "Noite", Inicio = new TimeSpan(22, 0, 0), Fim = new TimeSpan(6, 0, 0) }
            );
        }
    }
}
