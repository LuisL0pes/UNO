namespace Jogo.ViewModels
{
    /// ViewModel do painel de um bot na mesa de jogo.
    /// Cada painel (Bot1, Bot2, Bot3) tem o seu BotVM.
    public class BotVM : BaseViewModel
    {
        private string _nome         = string.Empty;
        private int    _numCartas    = 0;
        private bool   _ativo        = false;
        private bool   _visivel      = false;

        public string Nome
        {
            get => _nome;
            set => SetField(ref _nome, value);
        }

        public int NumCartas
        {
            get => _numCartas;
            set
            {
                if (SetField(ref _numCartas, value))
                    OnPropertyChanged(nameof(LabelCartas));
            }
        }

        ///Label "X Carta(s)" para mostrar na View.
        public string LabelCartas => $"{NumCartas} Carta{(NumCartas == 1 ? "" : "s")}";

        ///True quando é a vez deste bot — a View aplica destaque visual.
        public bool Ativo
        {
            get => _ativo;
            set
            {
                if (SetField(ref _ativo, value))
                    OnPropertyChanged(nameof(NomeComIndicador));
            }
        }

        ///Nome com indicador visual de turno ativo.
        public string NomeComIndicador => Ativo ? $"{Nome} ◀" : Nome;

        ///True quando este slot de bot está em uso (≥N bots escolhidos).
        public bool Visivel
        {
            get => _visivel;
            set => SetField(ref _visivel, value);
        }
    }
}
