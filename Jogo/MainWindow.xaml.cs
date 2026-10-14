using System.Collections.Generic;
using System.Windows;
using Jogo.ViewModels;

namespace Jogo
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            _vm = new MainViewModel();
            DataContext = _vm;

            _vm.PedirIniciarJogo        += AbrirSelecaoBots;
            _vm.PedirAbrirJogoCarregado += AbrirJanelaDeJogo;
            _vm.PedirEstatisticas       += AbrirEstatisticas;
        }

        private void AbrirSelecaoBots()
        {
            var selVM = new SelecaoBotsViewModel();
            var sel   = new SelecaoBotsWindow { DataContext = selVM };

            selVM.JogarConfirmado += numeroDeBots =>
            {
                sel.DialogResult = true;
                sel.Close();

                var jogo = new Game();

                string nomeWindows = System.Environment.UserName;
                jogo.Jogadores.Add(new Jogador { Nome = nomeWindows });
                for (int i = 1; i <= numeroDeBots; i++)
                    jogo.Jogadores.Add(new Jogador { Nome = $"Bot {i}" });

                // CarregarEstatisticas devolve agora as pontuações acumuladas
                // guardadas de sessões anteriores para este grupo de jogadores
                List<int> pontuacoes = GestorXml.CarregarEstatisticas(jogo.Jogadores);

                // Restaura as pontuações no jogo antes de iniciar
                while (jogo.Pontuacoes.Count < jogo.Jogadores.Count)
                    jogo.Pontuacoes.Add(0);
                for (int i = 0; i < pontuacoes.Count && i < jogo.Pontuacoes.Count; i++)
                    jogo.Pontuacoes[i] = pontuacoes[i];

                jogo.IniciarNovoJogo();
                AbrirJanelaDeJogo(jogo);
            };

            selVM.Cancelado += () => sel.Close();
            sel.ShowDialog();
        }

        private void AbrirJanelaDeJogo(Game jogo)
        {
            var jogoWindow = new JogoWindow(jogo);
            jogoWindow.ShowDialog();
            _vm.AtualizarEstadoJogoGuardado();
        }

        private void AbrirEstatisticas()
        {
            var jogadores = GestorXml.CarregarTodosJogadores();
            var estatVM   = new EstatisticasViewModel(jogadores);
            var janela    = new EstatisticasWindow { DataContext = estatVM };

            estatVM.PedirFechar += () => janela.Close();
            janela.ShowDialog();
        }
    }
}
