using System;
using System.Windows.Input;

namespace Jogo.ViewModels
{
    ///  
    /// ViewModel da janela de seleção do número de bots.
    /// A View faz Binding de NumeroDeBots ao RadioButton selecionado.
    ///  
    public class SelecaoBotsViewModel : BaseViewModel
    {
        private int _numeroDeBots = 1;

        ///  Número de bots escolhido (1, 2 ou 3). 
        public int NumeroDeBots
        {
            get => _numeroDeBots;
            set => SetField(ref _numeroDeBots, value);
        }

        // Propriedades booleanas para Binding bidirecional com RadioButtons
        public bool Um   { get => NumeroDeBots == 1; set { if (value) NumeroDeBots = 1; } }
        public bool Dois { get => NumeroDeBots == 2; set { if (value) NumeroDeBots = 2; } }
        public bool Tres { get => NumeroDeBots == 3; set { if (value) NumeroDeBots = 3; } }

        // ── Eventos ───────────────────────────────────────────────────────────

        ///  Disparado quando o utilizador confirma a seleção. 
        public event Action<int>? JogarConfirmado;

        ///  Disparado quando o utilizador cancela. 
        public event Action? Cancelado;

        // ── Comandos ──────────────────────────────────────────────────────────

        public ICommand JogarCommand  { get; }
        public ICommand VoltarCommand { get; }

        public SelecaoBotsViewModel()
        {
            JogarCommand  = new RelayCommand(() => JogarConfirmado?.Invoke(NumeroDeBots));
            VoltarCommand = new RelayCommand(() => Cancelado?.Invoke());
        }
    }
}
