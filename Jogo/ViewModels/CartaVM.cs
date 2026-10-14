using Jogo.ViewModels;

namespace Jogo.ViewModels
{
    ///  
    /// ViewModel que representa uma carta na mão do jogador humano.
    /// Expõe a propriedade Jogavel para que a View possa aplicar estilos diferentes
    /// consoante a carta pode ou não ser jogada neste momento.
    ///  
    public class CartaVM : BaseViewModel
    {
        private bool _jogavel;

        ///  A carta do modelo de dados associada a este ViewModel. 
        public Carta Carta { get; init; } = null!;

        ///  Caminho relativo para a imagem da carta nos Assets. 
        public string ImagemPath { get; init; } = string.Empty;

        ///  
        /// True se esta carta pode ser jogada sobre a carta do topo da mesa neste turno.
        /// Quando muda, a View actualiza automaticamente o estilo do botão via Binding.
        ///  
        public bool Jogavel
        {
            get => _jogavel;
            set => SetField(ref _jogavel, value);
        }
    }
}
