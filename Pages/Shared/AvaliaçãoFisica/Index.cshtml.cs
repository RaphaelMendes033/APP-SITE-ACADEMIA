using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace APP_SITE_ACADEMIA.Pages.Shared.AvaliacaoFisica
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public double Peso { get; set; }

        [BindProperty]
        public double Altura { get; set; }

        public double IMC { get; set; }

        public string Classificacao { get; set; }

        public void OnGet()
        {
        }

        public void OnPost()
        {
            if (Altura > 0)
            {
                IMC = Math.Round(Peso / (Altura * Altura), 2);
                Classificacao = ObterClassificacao(IMC);
            }
        }

        private string ObterClassificacao(double imc)
        {
            if (imc < 18.5)
                return "Abaixo do peso";
            else if (imc < 25)
                return "Peso normal";
            else if (imc < 30)
                return "Sobrepeso";
            else if (imc < 35)
                return "Obesidade Grau I";
            else if (imc < 40)
                return "Obesidade Grau II";
            else
                return "Obesidade Grau III (mórbida)";
        }
    }
}
