using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Jogo.ViewModels
{
    ///  
    /// ViewModel da janela de estatísticas.
    /// Expõe a coleção de jogadores para o DataGrid e os comandos de limpar/fechar.
    ///  
    public class EstatisticasViewModel : BaseViewModel
    {
        // ── Dados expostos à View ─────────────────────────────────────────────

        ///  Lista de linhas de estatísticas para o DataGrid. 
        public ObservableCollection<JogadorEstatisticaVM> Jogadores { get; } = new();

        private bool _semDados;

        ///  True quando não há estatísticas para mostrar. 
        public bool SemDados
        {
            get => _semDados;
            private set => SetField(ref _semDados, value);
        }

        // ── Eventos ───────────────────────────────────────────────────────────

        public event System.Action? PedirFechar;

        // ── Comandos ──────────────────────────────────────────────────────────

        public ICommand VoltarCommand  { get; }
        public ICommand LimparCommand  { get; }

        // ── Construtor ────────────────────────────────────────────────────────

        public EstatisticasViewModel(List<Jogador> jogadores)
        {
            VoltarCommand = new RelayCommand(() => PedirFechar?.Invoke());
            LimparCommand = new RelayCommand(ExecutarLimpar);

            CarregarDados(jogadores);
        }

        // ── Lógica ────────────────────────────────────────────────────────────

        private void CarregarDados(List<Jogador> jogadores)
        {
            Jogadores.Clear();

            if (jogadores == null || jogadores.Count == 0)
            {
                SemDados = true;
                return;
            }

            foreach (var j in jogadores)
            {
                Jogadores.Add(new JogadorEstatisticaVM
                {
                    Nome               = j.Nome,
                    N_Partidas_Jogadas = j.N_Partidas_Jogadas,
                    N_Partidas_Ganhos  = j.N_Partidas_Ganhos,
                    N_Jogos_Jogados    = j.N_Jogos_Jogados,
                    N_Jogos_Ganhos     = j.N_Jogos_Ganhos,
                    TaxaVitoriaStr     = j.N_Partidas_Jogadas > 0
                        ? $"{(j.N_Partidas_Ganhos * 100.0 / j.N_Partidas_Jogadas):F1}%"
                        : "—"
                });
            }

            SemDados = false;
        }

        private void ExecutarLimpar()
        {
            var resultado = MessageBox.Show(
                "Tens a certeza que queres apagar todas as estatísticas?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Yes) return;

            GestorXml.LimparEstatisticas();
            Jogadores.Clear();
            SemDados = true;
        }
    }

    // ── ViewModel auxiliar para cada linha da tabela ──────────────────────────

    ///  
    /// ViewModel de uma linha na tabela de estatísticas.
    /// Usado como item da ObservableCollection exposta ao DataGrid.
    ///  
    public class JogadorEstatisticaVM
    {
        public string Nome               { get; set; } = string.Empty;
        public int    N_Partidas_Jogadas { get; set; }
        public int    N_Partidas_Ganhos  { get; set; }
        public int    N_Jogos_Jogados    { get; set; }
        public int    N_Jogos_Ganhos     { get; set; }
        public string TaxaVitoriaStr     { get; set; } = "—";
    }
}
