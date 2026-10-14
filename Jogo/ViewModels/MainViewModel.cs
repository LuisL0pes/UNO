using System;
using System.Windows;
using System.Windows.Input;

namespace Jogo.ViewModels
{
    ///  
    /// ViewModel da janela principal (menu inicial).
    /// Expõe comandos e propriedades que a MainWindow consome via Binding,
    /// sem lógica no code-behind.
    ///  
    public class MainViewModel : BaseViewModel
    {
        // ── Estado ────────────────────────────────────────────────────────────

        private bool _jogoGuardadoExiste;

        ///  
        /// True se existe um ficheiro de jogo guardado.
        /// Controla IsEnabled do botão "Carregar Jogo" via Binding.
        ///  
        public bool JogoGuardadoExiste
        {
            get => _jogoGuardadoExiste;
            private set => SetField(ref _jogoGuardadoExiste, value);
        }

        // ── Eventos para a View abrir janelas ─────────────────────────────────

        ///  Pedido para abrir a janela de seleção de bots e iniciar jogo. 
        public event Action? PedirIniciarJogo;

        ///  Pedido para abrir a janela de jogo com um jogo carregado. 
        public event Action<Game>? PedirAbrirJogoCarregado;

        ///  Pedido para abrir a janela de estatísticas. 
        public event Action? PedirEstatisticas;

        // ── Comandos ──────────────────────────────────────────────────────────

        public ICommand ComecarJogoCommand    { get; }
        public ICommand CarregarJogoCommand   { get; }
        public ICommand EstatisticasCommand   { get; }
        public ICommand SairCommand           { get; }

        // ── Construtor ────────────────────────────────────────────────────────

        public MainViewModel()
        {
            JogoGuardadoExiste = GestorXml.ExisteJogoGuardado;

            ComecarJogoCommand  = new RelayCommand(ExecutarComecarJogo);
            CarregarJogoCommand = new RelayCommand(ExecutarCarregarJogo, () => JogoGuardadoExiste);
            EstatisticasCommand = new RelayCommand(ExecutarEstatisticas);
            SairCommand         = new RelayCommand(() => Application.Current.Shutdown());
        }

        // ── Execução dos comandos ─────────────────────────────────────────────

        private void ExecutarComecarJogo()
        {
            PedirIniciarJogo?.Invoke();
        }

        private void ExecutarCarregarJogo()
        {
            var jogo = GestorXml.CarregarJogo();
            if (jogo == null)
            {
                MessageBox.Show("Não foi possível carregar o jogo guardado.",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            PedirAbrirJogoCarregado?.Invoke(jogo);
            AtualizarEstadoJogoGuardado();
        }

        private void ExecutarEstatisticas()
        {
            PedirEstatisticas?.Invoke();
        }

        ///  
        /// Chamado pela View após fechar a janela de jogo,
        /// para reavaliação do botão "Carregar Jogo".
        ///  
        public void AtualizarEstadoJogoGuardado()
        {
            JogoGuardadoExiste = GestorXml.ExisteJogoGuardado;
        }
    }
}
