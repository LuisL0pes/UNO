using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Jogo.ViewModels
{
    ///  
    /// ViewModel principal da mesa de jogo.
    /// Toda a lógica de apresentação que estava no JogoWindow.xaml.cs
    /// foi movida para aqui. O code-behind fica só com o timer (que precisa
    /// do Dispatcher) e com os event handlers de botões que delegam neste VM.
    ///  
    public class JogoViewModel : BaseViewModel
    {
        // ── Referência ao modelo ──────────────────────────────────────────────
        private readonly Game _jogo;

        // ── Coleções observáveis expostas à View ──────────────────────────────

        ///  Mão do jogador humano — o ItemsSource do painel de cartas. 
        public ObservableCollection<CartaVM> MaoHumano { get; } = new();

        ///  ViewModels dos três slots de bot. 
        public BotVM Bot1 { get; } = new();
        public BotVM Bot2 { get; } = new();
        public BotVM Bot3 { get; } = new();

        // ── Propriedades da mesa ──────────────────────────────────────────────

        private string _imagemCartaTopo = "/Assets/card_back.png";
        public string ImagemCartaTopo
        {
            get => _imagemCartaTopo;
            private set => SetField(ref _imagemCartaTopo, value);
        }

        private Brush _corAtivaPincel = Brushes.Gray;
        public Brush CorAtivaPincel
        {
            get => _corAtivaPincel;
            private set => SetField(ref _corAtivaPincel, value);
        }

        private string _corAtivaTexto = "—";
        public string CorAtivaTexto
        {
            get => _corAtivaTexto;
            private set => SetField(ref _corAtivaTexto, value);
        }

        private string _sentido = "↺";
        public string Sentido
        {
            get => _sentido;
            private set => SetField(ref _sentido, value);
        }

        private string _numBaralho = "0";
        public string NumBaralho
        {
            get => _numBaralho;
            private set => SetField(ref _numBaralho, value);
        }

        // ── Propriedades dos controlos ────────────────────────────────────────

        private string _estadoTexto = string.Empty;
        public string EstadoTexto
        {
            get => _estadoTexto;
            set => SetField(ref _estadoTexto, value);
        }

        private string _textoComprar = "Comprar Carta";
        public string TextoComprar
        {
            get => _textoComprar;
            private set => SetField(ref _textoComprar, value);
        }

        private bool _comprarEnabled;
        public bool ComprarEnabled
        {
            get => _comprarEnabled;
            private set => SetField(ref _comprarEnabled, value);
        }

        private bool _unoVisivel;
        public bool UnoVisivel
        {
            get => _unoVisivel;
            private set => SetField(ref _unoVisivel, value);
        }

        private bool _escolhaCorVisivel;
        public bool EscolhaCorVisivel
        {
            get => _escolhaCorVisivel;
            private set => SetField(ref _escolhaCorVisivel, value);
        }

        private string _nomeHumano = string.Empty;
        public string NomeHumano
        {
            get => _nomeHumano;
            private set => SetField(ref _nomeHumano, value);
        }

        // ── Eventos para a View ───────────────────────────────────────────────

        ///  Pedido para iniciar o timer de bot (a View tem o Dispatcher). 
        public event Action? PedirIniciarTimerBot;

        ///  Pedido para parar o timer de bot. 
        public event Action? PedirPararTimerBot;

        ///  Pedido para fechar a janela. 
        public event Action? PedirFechar;

        // ── Comandos ──────────────────────────────────────────────────────────

        public ICommand JogarCartaCommand     { get; }
        public ICommand ComprarCommand        { get; }
        public ICommand GritarUnoCommand      { get; }
        public ICommand EscolherCorCommand    { get; }
        public ICommand PausaCommand          { get; }
        public ICommand AjudaCommand          { get; }
        public ICommand SairCommand           { get; }

        // ── Construtor ────────────────────────────────────────────────────────

        public JogoViewModel(Game jogo)
        {
            _jogo = jogo;

            // Subscreve eventos do modelo
            _jogo.OnCartaJogada         += (_, _)  => AtualizarTudo();
            _jogo.OnCartaComprada       += (_, _)  => AtualizarTudo();
            _jogo.OnTurnoMudou          += Modelo_TurnoMudou;
            _jogo.OnJogoTerminou        += Modelo_JogoTerminou;
            _jogo.OnUnoGritado          += Modelo_UnoGritado;
            _jogo.OnPenalidadeUno       += Modelo_PenalidadeUno;
            _jogo.OnBaralhoReabastecido += (_, _)  => NumBaralho = $"{_jogo.Mesa.Baralho.Cartas.Count}";

            // Comandos
            JogarCartaCommand  = new RelayCommand(ExecutarJogarCarta);
            ComprarCommand     = new RelayCommand(ExecutarComprar,   () => ComprarEnabled);
            GritarUnoCommand   = new RelayCommand(ExecutarGritarUno, () => _jogo.HumanoDeveGritarUno);
            EscolherCorCommand = new RelayCommand(ExecutarEscolherCor);
            PausaCommand       = new RelayCommand(ExecutarPausa);
            AjudaCommand       = new RelayCommand(ExecutarAjuda);
            SairCommand        = new RelayCommand(ExecutarSair);

            AtualizarTudo();

            // Se o primeiro turno é de um bot, arranca o timer
            if (_jogo.JogoEmCurso && _jogo.JogadorAtivo != _jogo.Jogadores[0])
                PedirIniciarTimerBot?.Invoke();
        }

        // ── Atualização completa ──────────────────────────────────────────────

        public void AtualizarTudo()
        {
            AtualizarMaoHumano();
            AtualizarBots();
            AtualizarMesa();
            AtualizarControlos();
        }

        // ── Mão do jogador humano ─────────────────────────────────────────────

        private void AtualizarMaoHumano()
        {
            if (_jogo.Jogadores.Count == 0) return;

            var humano    = _jogo.Jogadores[0];
            var topo      = _jogo.Mesa.CartaNoTopo;
            bool vezHumano = _jogo.JogadorHumanoTemVez;

            MaoHumano.Clear();
            foreach (var carta in humano.Cartas)
            {
                bool jogavel = vezHumano && topo != null
                            && _jogo.CartasPendentesCompra == 0
                            && carta.PodeJogarSobre(topo, _jogo.Mesa.CorAtiva);

                MaoHumano.Add(new CartaVM
                {
                    Carta      = carta,
                    ImagemPath = ObterCaminhoImagem(carta),
                    Jogavel    = jogavel
                });
            }

            NomeHumano = $"{humano.Nome}  •  {humano.Cartas.Count} carta{(humano.Cartas.Count == 1 ? "" : "s")}";
        }

        // ── Bots ──────────────────────────────────────────────────────────────

        private void AtualizarBots()
        {
            int nBots = _jogo.Jogadores.Count - 1;
            AtualizarBot(Bot1, nBots >= 1 ? _jogo.Jogadores[1] : null);
            AtualizarBot(Bot2, nBots >= 2 ? _jogo.Jogadores[2] : null);
            AtualizarBot(Bot3, nBots >= 3 ? _jogo.Jogadores[3] : null);
        }

        private void AtualizarBot(BotVM vm, Jogador? bot)
        {
            if (bot == null) { vm.Visivel = false; return; }
            vm.Visivel   = true;
            vm.Nome      = bot.Nome;
            vm.NumCartas = bot.Cartas.Count;
            vm.Ativo     = _jogo.JogadorAtivo == bot;
        }

        // ── Mesa ──────────────────────────────────────────────────────────────

        private void AtualizarMesa()
        {
            var topo = _jogo.Mesa.CartaNoTopo;
            if (topo != null)
                ImagemCartaTopo = ObterCaminhoImagem(topo);

            CorAtivaPincel = CorParaPincel(_jogo.Mesa.CorAtiva);
            CorAtivaTexto  = CorParaNome(_jogo.Mesa.CorAtiva);
            Sentido        = _jogo.SentidoNormal ? "↻" : "↺";
            NumBaralho     = $"{_jogo.Mesa.Baralho.Cartas.Count}";
        }

        // ── Controlos ─────────────────────────────────────────────────────────

        private void AtualizarControlos()
        {
            bool vezHumano = _jogo.JogadorHumanoTemVez;

            ComprarEnabled    = vezHumano;
            UnoVisivel        = _jogo.HumanoDeveGritarUno;
            EscolhaCorVisivel = _jogo.AguardarEscolhaCor && _jogo.JogadorAtivo == _jogo.Jogadores[0];

            TextoComprar = _jogo.CartasPendentesCompra > 0
                ? $"Comprar +{_jogo.CartasPendentesCompra}"
                : "Comprar Carta";

            if (!_jogo.JogoEmCurso)
            {
                EstadoTexto = "";
                return;
            }

            if (EscolhaCorVisivel)
                EstadoTexto = "Escolhe uma cor!";
            else if (vezHumano && _jogo.CartasPendentesCompra > 0)
                EstadoTexto = $"Obrigado a comprar +{_jogo.CartasPendentesCompra} cartas!";
            else if (vezHumano)
                EstadoTexto = _jogo.JogadorHumanoTemCartaJogavel
                    ? "A tua vez — joga uma carta!"
                    : "Sem cartas jogáveis — compra uma carta.";
            else
                EstadoTexto = $"Vez de {_jogo.JogadorAtivo?.Nome}...";
        }

        // ── Eventos do modelo ─────────────────────────────────────────────────

        private void Modelo_TurnoMudou(object? sender, TurnoMudouEventArgs e)
        {
            AtualizarTudo();
            if (_jogo.JogoEmCurso && _jogo.JogadorAtivo != _jogo.Jogadores[0])
                PedirIniciarTimerBot?.Invoke();
            else
                PedirPararTimerBot?.Invoke();
        }

        private void Modelo_JogoTerminou(object? sender, JogoTerminouEventArgs e)
        {
            PedirPararTimerBot?.Invoke();
            AtualizarTudo();

            string pontuacoes = string.Join("\n", _jogo.Jogadores.Select((j, i) =>
                $"  {j.Nome}: {_jogo.Pontuacoes[i]} pontos"));

            bool humanoVenceu = e.Vencedor == _jogo.Jogadores[0];
            string cabecalho  = humanoVenceu
                ? $"🎉 Parabéns! Ganhaste esta partida!\n(+{e.PontuacaoRond} pontos)"
                : $"😔 {e.Vencedor.Nome} ganhou a partida!\n(+{e.PontuacaoRond} pontos)";

            if (e.FimDeJogo)
            {
                string vencedorJogo = humanoVenceu ? "Tu ganhaste o jogo!" : $"{e.Vencedor.Nome} ganhou o jogo!";
                MessageBox.Show(
                    $"{cabecalho}\n\n🏆 {vencedorJogo}\n\nPlacar final:\n{pontuacoes}",
                    "Fim do Jogo!", MessageBoxButton.OK, MessageBoxImage.Information);
                GestorXml.ApagarJogoGuardado();
                PedirFechar?.Invoke();
            }
            else
            {
                var r = MessageBox.Show(
                    $"{cabecalho}\n\nPlacar:\n{pontuacoes}\n\nJogar nova partida?",
                    "Fim da Partida", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (r == MessageBoxResult.Yes)
                {
                    _jogo.IniciarNovoJogo();
                    AtualizarTudo();
                    if (_jogo.JogadorAtivo != _jogo.Jogadores[0])
                        PedirIniciarTimerBot?.Invoke();
                }
                else
                {
                    _jogo.SuspenderJogo();
                    PedirFechar?.Invoke();
                }
            }
        }

        private void Modelo_UnoGritado(object? sender, UnoGritadoEventArgs e)
        {
            EstadoTexto = $"🃏 UNO! {e.Jogador.Nome} tem só 1 carta!";
            if (e.Jogador == _jogo.Jogadores[0])
                UnoVisivel = false;
        }

        private void Modelo_PenalidadeUno(object? sender, PenalidadeUnoEventArgs e)
        {
            EstadoTexto = $"⚠️ {e.Jogador.Nome} não gritou UNO! +{e.NumCartas} cartas de penalidade!";
            AtualizarTudo();
        }

        // ── Execução dos comandos ─────────────────────────────────────────────

        ///  
        /// Chamado pela View quando o utilizador clica numa carta.
        /// O parâmetro é o objeto Carta passado via CommandParameter no XAML.
        ///  
        private void ExecutarJogarCarta(object? parameter)
        {
            if (parameter is not Carta carta) return;
            if (!_jogo.JogarCarta(carta))
                EstadoTexto = "Não podes jogar essa carta agora.";
        }

        private void ExecutarComprar()
        {
            _jogo.ComprarCarta();
        }

        private void ExecutarGritarUno()
        {
            _jogo.GritarUno();
            EstadoTexto = $"🃏 UNO! {_jogo.Jogadores[0].Nome} gritou UNO!";
            UnoVisivel  = false;
        }

        ///  
        /// Chamado com parâmetro string: "Vermelho", "Verde", "Azul", "Amarelo".
        ///  
        private void ExecutarEscolherCor(object? parameter)
        {
            if (parameter is not string nomeCor) return;
            var cor = nomeCor switch
            {
                "Vermelho" => CorCarta.Vermelho,
                "Verde"    => CorCarta.Verde,
                "Azul"     => CorCarta.Azul,
                "Amarelo"  => CorCarta.Amarelo,
                _          => CorCarta.Vermelho
            };
            _jogo.EscolherCor(cor);
        }

        private void ExecutarPausa()
        {
            PedirPararTimerBot?.Invoke();

            var resultado = MessageBox.Show(
                "Jogo em pausa.\n\nQueres guardar e sair?",
                "Pausa", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                _jogo.SuspenderJogo();
                MessageBox.Show("Jogo guardado com sucesso!", "Guardado",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                PedirFechar?.Invoke();
            }
            else if (resultado == MessageBoxResult.Cancel)
            {
                // Retoma o timer se for vez de um bot
                if (_jogo.JogoEmCurso && _jogo.JogadorAtivo != _jogo.Jogadores[0])
                    PedirIniciarTimerBot?.Invoke();
            }
        }

        private static void ExecutarAjuda()
        {
            MessageBox.Show(
                "Como jogar UNO:\n\n" +
                "• Joga uma carta da mesma cor ou símbolo que a carta do topo.\n" +
                "• Joker: muda a cor ativa.\n" +
                "• Joker +4: muda a cor e o próximo jogador compra 4 cartas.\n" +
                "• +2: o próximo jogador compra 2 cartas.\n" +
                "• Inverter: inverte o sentido de jogo.\n" +
                "• Salta: o próximo jogador perde a vez.\n" +
                "• Se não tiveres cartas jogáveis, compra uma carta.\n" +
                "• O primeiro a ficar sem cartas ganha!",
                "Ajuda — Regras do UNO",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecutarSair()
        {
            if (MessageBox.Show("Tens a certeza que queres sair?", "Sair",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                PedirFechar?.Invoke();
        }

        // ── Método público para o timer (chamado pelo code-behind) ────────────

        ///  
        /// Executa o turno do bot. Chamado pelo DispatcherTimer no code-behind.
        /// Toda a lógica fica no ViewModel; o code-behind só despacha a chamada.
        ///  
        public void ExecutarTurnoBot()
        {
            if (!_jogo.JogoEmCurso) return;
            if (_jogo.JogadorAtivo == _jogo.Jogadores[0]) return;
            _jogo.ExecutarTurnoBot();
        }

        // ── Helpers estáticos (imagens e cores) ───────────────────────────────

        public static string ObterCaminhoImagem(Carta carta)
        {
            if (carta.Simbolo == SimboloCarta.Joker)  return "/Assets/wild_card.png";
            if (carta.Simbolo == SimboloCarta.Joker4) return "/Assets/wild_card_four.png";

            string cor = carta.Cor switch
            {
                CorCarta.Vermelho => "red",
                CorCarta.Verde    => "green",
                CorCarta.Azul     => "blue",
                CorCarta.Amarelo  => "yellow",
                _                 => "red"
            };

            string simbolo = carta.Simbolo switch
            {
                SimboloCarta.Zero     => "0",
                SimboloCarta.Um       => "1",
                SimboloCarta.Dois     => "2",
                SimboloCarta.Tres     => "3",
                SimboloCarta.Quatro   => "4",
                SimboloCarta.Cinco    => "5",
                SimboloCarta.Seis     => "6",
                SimboloCarta.Sete     => "7",
                SimboloCarta.Oito     => "8",
                SimboloCarta.Nove     => "9",
                SimboloCarta.Compra2  => "draw_two",
                SimboloCarta.Inverter => "reverse",
                SimboloCarta.Salta    => "skip",
                _                     => "0"
            };

            return $"/Assets/{cor}_{simbolo}.png";
        }

        public static Brush CorParaPincel(CorCarta cor) => cor switch
        {
            CorCarta.Vermelho => new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B)),
            CorCarta.Verde    => new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60)),
            CorCarta.Azul     => new SolidColorBrush(Color.FromRgb(0x29, 0x80, 0xB9)),
            CorCarta.Amarelo  => new SolidColorBrush(Color.FromRgb(0xF1, 0xC4, 0x0F)),
            _                 => Brushes.Gray
        };

        public static string CorParaNome(CorCarta cor) => cor switch
        {
            CorCarta.Vermelho => "Vermelho",
            CorCarta.Verde    => "Verde",
            CorCarta.Azul     => "Azul",
            CorCarta.Amarelo  => "Amarelo",
            _                 => "—"
        };
    }
}
