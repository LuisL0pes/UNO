using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Jogo.ViewModels;

namespace Jogo
{
    public partial class JogoWindow : Window
    {
        private readonly JogoViewModel _vm;

        private readonly DispatcherTimer _timerBot = new()
        {
            Interval = TimeSpan.FromMilliseconds(2000)  // protocolo: bots demoram 2 segundos
        };

        public JogoWindow(Game jogo)
        {
            InitializeComponent();

            _vm         = new JogoViewModel(jogo);
            DataContext = _vm;

            // O ViewModel pede ao code-behind para controlar o timer
            _vm.PedirIniciarTimerBot += () => Dispatcher.Invoke(_timerBot.Start);
            _vm.PedirPararTimerBot   += () => Dispatcher.Invoke(_timerBot.Stop);
            _vm.PedirFechar          += () => Dispatcher.Invoke(Close);

            _timerBot.Tick += TimerBot_Tick;

            // Subscreve mudanças nos BotVMs para redesenhar os Canvas
            _vm.Bot1.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(BotVM.NumCartas)) DesenharBot1(); };
            _vm.Bot2.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(BotVM.NumCartas)) DesenharBot2(); };
            _vm.Bot3.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(BotVM.NumCartas)) DesenharBot3(); };

            // Desenho inicial
            DesenharBot1();
            DesenharBot2();
            DesenharBot3();
        }

        // ── Timer ─────────────────────────────────────────────────────────────

        private void TimerBot_Tick(object? sender, EventArgs e)
        {
            _timerBot.Stop();
            _vm.ExecutarTurnoBot();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _timerBot.Stop();
            base.OnClosing(e);
        }

        // ── Desenho das costas das cartas dos bots (Canvas) ───────────────────
        // Canvas não suporta binding de coleções — este é o único código de UI
        // que permanece no code-behind, justificado pela limitação do controlo.

        private void DesenharBot1() => DesenharCostas(CartasBot1Canvas, _vm.Bot1.NumCartas);
        private void DesenharBot2() => DesenharCostas(CartasBot2Canvas, _vm.Bot2.NumCartas);
        private void DesenharBot3() => DesenharCostas(CartasBot3Canvas, _vm.Bot3.NumCartas);

        private static void DesenharCostas(Canvas canvas, int quantidade)
        {
            canvas.Children.Clear();
            if (quantidade == 0) return;

            int    visiveis = Math.Min(quantidade, 7);
            double largura  = 38;
            double altura   = 55;
            double passo    = Math.Min(18.0, (canvas.Width - largura) / Math.Max(visiveis - 1, 1));

            for (int i = 0; i < visiveis; i++)
            {
                var img = new Image
                {
                    Source = new BitmapImage(new Uri("/Assets/card_back.png", UriKind.Relative)),
                    Width  = largura,
                    Height = altura
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                Canvas.SetLeft(img, i * passo);
                Canvas.SetTop(img, (canvas.Height - altura) / 2);
                canvas.Children.Add(img);
            }
        }
    }
}
