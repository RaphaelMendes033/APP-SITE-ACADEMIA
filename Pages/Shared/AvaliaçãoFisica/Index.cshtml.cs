using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace APP_SITE_ACADEMIA.Pages.Shared.AvaliacaoFisica
{
    public class IndexModel : PageModel
    {
        // Dados do formulário
        [BindProperty] public string Nome { get; set; }
        [BindProperty] public int Idade { get; set; }
        [BindProperty] public string Sexo { get; set; }

        [BindProperty] public double BracoDireito { get; set; }
        [BindProperty] public double BracoEsquerdo { get; set; }
        [BindProperty] public double Peitoral { get; set; }
        [BindProperty] public double Abdomen { get; set; }
        [BindProperty] public double Cintura { get; set; }
        [BindProperty] public double Quadril { get; set; }
        [BindProperty] public double CoxaDireita { get; set; }
        [BindProperty] public double CoxaEsquerda { get; set; }
        [BindProperty] public double PanturrilhaDireita { get; set; }
        [BindProperty] public double PanturrilhaEsquerda { get; set; }

        [BindProperty] public double Peso { get; set; }
        [BindProperty] public double Altura { get; set; }

        public double IMC { get; set; }

        // Histórico simulado em memória
        public static List<AvaliacaoFisicaModel> HistoricoAvaliacoes { get; set; } = new();

        // Filtro de datas
        [BindProperty(SupportsGet = true)] public DateTime? DataInicio { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? DataFim { get; set; }
        public List<AvaliacaoFisicaModel> AvaliacoesFiltradas { get; set; } = new();

        public void OnGet()
        {
            FiltrarHistorico();
        }

        public IActionResult OnPost()
        {
            if (Altura > 0)
                IMC = Math.Round(Peso / (Altura * Altura), 2);

            var nova = new AvaliacaoFisicaModel
            {
                Data = DateTime.Now,
                Nome = Nome,
                Idade = Idade,
                Sexo = Sexo,
                BracoDireito = BracoDireito,
                BracoEsquerdo = BracoEsquerdo,
                Peitoral = Peitoral,
                Abdomen = Abdomen,
                Cintura = Cintura,
                Quadril = Quadril,
                CoxaDireita = CoxaDireita,
                CoxaEsquerda = CoxaEsquerda,
                PanturrilhaDireita = PanturrilhaDireita,
                PanturrilhaEsquerda = PanturrilhaEsquerda,
                Peso = Peso,
                Altura = Altura,
                IMC = IMC
            };

            HistoricoAvaliacoes.Add(nova);
            return RedirectToPage(); // limpa o formulário
        }

        private void FiltrarHistorico()
        {
            AvaliacoesFiltradas = HistoricoAvaliacoes;

            if (DataInicio.HasValue)
                AvaliacoesFiltradas = AvaliacoesFiltradas.Where(x => x.Data.Date >= DataInicio.Value.Date).ToList();

            if (DataFim.HasValue)
                AvaliacoesFiltradas = AvaliacoesFiltradas.Where(x => x.Data.Date <= DataFim.Value.Date).ToList();

            AvaliacoesFiltradas = AvaliacoesFiltradas.OrderByDescending(x => x.Data).ToList();
        }

        public class AvaliacaoFisicaModel
        {
            public DateTime Data { get; set; }
            public string Nome { get; set; }
            public int Idade { get; set; }
            public string Sexo { get; set; }
            public double BracoDireito { get; set; }
            public double BracoEsquerdo { get; set; }
            public double Peitoral { get; set; }
            public double Abdomen { get; set; }
            public double Cintura { get; set; }
            public double Quadril { get; set; }
            public double CoxaDireita { get; set; }
            public double CoxaEsquerda { get; set; }
            public double PanturrilhaDireita { get; set; }
            public double PanturrilhaEsquerda { get; set; }
            public double Peso { get; set; }
            public double Altura { get; set; }
            public double IMC { get; set; }
        }
    }
}
