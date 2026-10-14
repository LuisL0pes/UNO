using System;
using System.Collections.Generic;
using System.Linq;

namespace Jogo
{
    // ─── Eventos ────────────────────────────────────────────────────────────────

    public class CartaJogadaEventArgs : EventArgs
    {
        public Jogador  Jogador      { get; init; } = null!;
        public Carta    Carta        { get; init; } = null!;
        public CorCarta CorEscolhida { get; init; }
    }

    public class CartaCompradaEventArgs : EventArgs
    {
        public Jogador Jogador { get; init; } = null!;
        public Carta   Carta   { get; init; } = null!;
    }

    public class TurnoMudouEventArgs : EventArgs
    {
        public Jogador JogadorAtivo { get; init; } = null!;
        public int     IndiceAtivo  { get; init; }
    }

    public class JogoTerminouEventArgs : EventArgs
    {
        public Jogador Vencedor      { get; init; } = null!;
        public int     PontuacaoRond { get; init; }
        public bool    FimDeJogo     { get; init; } // true se atingiu 500 pontos
    }

    public class UnoGritadoEventArgs : EventArgs
    {
        public Jogador Jogador { get; init; } = null!;
    }

    public class PenalidadeUnoEventArgs : EventArgs
    {
        public Jogador Jogador    { get; init; } = null!;
        public int     NumCartas  { get; init; }
    }

    // ─── Game ────────────────────────────────────────────────────────────────────

    public class Game
    {
        // ── Constante de pontuação máxima (fim do jogo completo) ─────────────────
        public const int PONTUACAO_MAXIMA = 500;

        // ── Estado público ───────────────────────────────────────────────────────
        public List<Jogador> Jogadores    { get; set; } = new();
        public List<int>     Pontuacoes   { get; set; } = new();
        public Jogador?      JogadorAtivo { get; private set; }
        public Mesa          Mesa         { get; set; } = new();

        public int  IndiceAtivo           { get; private set; } = 0;
        public bool SentidoNormal         { get; private set; } = true;
        public bool JogoEmCurso           { get; private set; } = false;
        public bool AguardarEscolhaCor    { get; private set; } = false;
        public int  CartasPendentesCompra { get; private set; } = 0;

        // UNO: humano gritou UNO nesta ronda?
        public bool HumanoGritouUno       { get; private set; } = false;

        // ── Eventos ──────────────────────────────────────────────────────────────
        public event EventHandler<CartaJogadaEventArgs>?   OnCartaJogada;
        public event EventHandler<CartaCompradaEventArgs>? OnCartaComprada;
        public event EventHandler<TurnoMudouEventArgs>?    OnTurnoMudou;
        public event EventHandler<JogoTerminouEventArgs>?  OnJogoTerminou;
        public event EventHandler<UnoGritadoEventArgs>?    OnUnoGritado;
        public event EventHandler<PenalidadeUnoEventArgs>? OnPenalidadeUno;
        public event EventHandler?                         OnBaralhoReabastecido;

        private readonly Random _rnd = new();

        // ═════════════════════════════════════════════════════════════════════════
        //  INICIALIZAÇÃO
        // ═════════════════════════════════════════════════════════════════════════

        public void IniciarNovoJogo()
        {
            if (Jogadores.Count < 2)
                throw new InvalidOperationException("São necessários pelo menos 2 jogadores.");

            while (Pontuacoes.Count < Jogadores.Count) Pontuacoes.Add(0);

            Mesa = new Mesa();
            Mesa.Baralho.Embaralhar();

            foreach (var j in Jogadores) j.Cartas.Clear();
            foreach (var j in Jogadores)
                for (int i = 0; i < 7; i++)
                    ComprarCartaParaJogador(j, silencioso: true);

            // Primeira carta — não pode ser Joker
            Carta? primeira;
            do
            {
                primeira = Mesa.Baralho.ComprarCarta();
                if (primeira == null) break;
                if (primeira.EJoker) { Mesa.Baralho.Cartas.Add(primeira); primeira = null; }
            } while (primeira == null);

            if (primeira != null) Mesa.ColocarCarta(primeira);

            IndiceAtivo           = 0;
            SentidoNormal         = true;
            JogadorAtivo          = Jogadores[IndiceAtivo];
            AguardarEscolhaCor    = false;
            CartasPendentesCompra = 0;
            HumanoGritouUno       = false;
            JogoEmCurso           = true;

            AplicarEfeitoPrimeiraCartaMesa();

            OnTurnoMudou?.Invoke(this, new TurnoMudouEventArgs
            {
                JogadorAtivo = JogadorAtivo,
                IndiceAtivo  = IndiceAtivo
            });
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  AÇÕES DO JOGADOR HUMANO
        // ═════════════════════════════════════════════════════════════════════════

        ///O jogador humano grita UNO (antes de jogar a penúltima carta). 
        public void GritarUno()
        {
            if (!JogoEmCurso) return;
            HumanoGritouUno = true;
            OnUnoGritado?.Invoke(this, new UnoGritadoEventArgs { Jogador = Jogadores[0] });
        }

        ///Joga uma carta. Devolve false se inválido. 
        public bool JogarCarta(Carta carta, CorCarta? corEscolhida = null)
        {
            if (!JogoEmCurso || AguardarEscolhaCor)    return false;
            if (JogadorAtivo != Jogadores[0])           return false;
            // Se há cartas pendentes por +2 ou +4, o jogador é obrigado a apanhá-las primeiro
            if (CartasPendentesCompra > 0)              return false;
            if (!carta.PodeJogarSobre(Mesa.CartaNoTopo!, Mesa.CorAtiva)) return false;

            var humano = Jogadores[0];

            // Penalidade: tinha 2 cartas, não gritou UNO antes de jogar a penúltima
            if (humano.Cartas.Count == 2 && !HumanoGritouUno)
            {
                // Penaliza com 2 cartas antes de jogar
                for (int i = 0; i < 2; i++) ComprarCartaParaJogador(humano);
                OnPenalidadeUno?.Invoke(this, new PenalidadeUnoEventArgs
                    { Jogador = humano, NumCartas = 2 });
            }

            HumanoGritouUno = false; // reset para a próxima vez
            ExecutarJogadaCarta(humano, carta, corEscolhida);
            return true;
        }

        /// Escolhe cor após Joker. 
        public bool EscolherCor(CorCarta cor)
        {
            if (!AguardarEscolhaCor) return false;

            Mesa.CorAtiva      = cor;
            AguardarEscolhaCor = false;

            if (Mesa.CartaNoTopo!.Simbolo == SimboloCarta.Joker4)
                CartasPendentesCompra += 4;

            if (!VerificarFimDeJogo())
                AvançarTurno();

            return true;
        }

        ///Compra uma carta (ou as pendentes por penalização). 
        public Carta? ComprarCarta()
        {
            if (!JogoEmCurso || AguardarEscolhaCor) return null;
            if (JogadorAtivo != Jogadores[0])        return null;

            if (CartasPendentesCompra > 0)
            {
                ComprarCartasPendentes(JogadorAtivo);
                AvançarTurno();
                return null;
            }

            var carta = ComprarCartaParaJogador(JogadorAtivo);
            AvançarTurno();
            return carta;
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  TURNO DOS BOTS
        // ═════════════════════════════════════════════════════════════════════════

        public void ExecutarTurnoBot()
        {
            if (!JogoEmCurso || AguardarEscolhaCor) return;
            if (JogadorAtivo == Jogadores[0])        return;

            var bot = JogadorAtivo!;

            if (CartasPendentesCompra > 0)
            {
                ComprarCartasPendentes(bot);
                AvançarTurno();
                return;
            }

            var jogaveis = bot.CartasJogaveis(Mesa.CartaNoTopo!, Mesa.CorAtiva);
            if (jogaveis.Count > 0)
            {
                var carta        = jogaveis[_rnd.Next(jogaveis.Count)];
                var corEscolhida = CorMaisFrequenteNaMao(bot);

                // Bot grita UNO se vai ficar com 1 carta
                if (bot.Cartas.Count == 2)
                    OnUnoGritado?.Invoke(this, new UnoGritadoEventArgs { Jogador = bot });

                ExecutarJogadaCarta(bot, carta, carta.EJoker ? corEscolhida : null);

                if (AguardarEscolhaCor)
                {
                    Mesa.CorAtiva      = corEscolhida;
                    AguardarEscolhaCor = false;

                    if (Mesa.CartaNoTopo!.Simbolo == SimboloCarta.Joker4)
                        CartasPendentesCompra += 4;

                    if (!VerificarFimDeJogo())
                        AvançarTurno();
                }
            }
            else
            {
                ComprarCartaParaJogador(bot);
                AvançarTurno();
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  SUSPENDER / RESTAURAR (XML)
        // ═════════════════════════════════════════════════════════════════════════

        public void SuspenderJogo()
        {
            GestorXml.GuardarJogo(this);
            GestorXml.GuardarEstatisticas(Jogadores, Pontuacoes);
        }

        ///Chamado pelo GestorXml após desserialização. 
        public void RestaurarEstado(int indiceAtivo, bool sentidoNormal,
                                    int cartasPendentes, CorCarta corAtiva)
        {
            IndiceAtivo           = indiceAtivo;
            SentidoNormal         = sentidoNormal;
            CartasPendentesCompra = cartasPendentes;
            Mesa.CorAtiva         = corAtiva;
            JogoEmCurso           = true;
            AguardarEscolhaCor    = false;
            HumanoGritouUno       = false;

            if (Jogadores.Count > 0)
                JogadorAtivo = Jogadores[IndiceAtivo];
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  LÓGICA INTERNA
        // ═════════════════════════════════════════════════════════════════════════

        private void ExecutarJogadaCarta(Jogador jogador, Carta carta, CorCarta? corEscolhida)
        {
            jogador.JogarCarta(carta);
            bool eJoker = carta.EJoker;
            Mesa.ColocarCarta(carta, eJoker ? corEscolhida : null);

            if (VerificarFimDeJogo()) return;

            // Define efeitos ANTES de disparar o evento,
            // para que a UI já veja AguardarEscolhaCor=true quando atualiza
            if (eJoker && jogador == Jogadores[0])
            {
                AguardarEscolhaCor = true;
            }
            else
            {
                AplicarEfeitoCartaJogada(carta);
            }

            OnCartaJogada?.Invoke(this, new CartaJogadaEventArgs
            {
                Jogador      = jogador,
                Carta        = carta,
                CorEscolhida = corEscolhida ?? carta.Cor
            });

            if (!AguardarEscolhaCor) AvançarTurno();
        }

        private void AplicarEfeitoCartaJogada(Carta carta)
        {
            switch (carta.Simbolo)
            {
                case SimboloCarta.Inverter:
                    SentidoNormal = !SentidoNormal;
                    if (Jogadores.Count == 2) AvançarIndice();
                    break;
                case SimboloCarta.Salta:
                    AvançarIndice();
                    break;
                case SimboloCarta.Compra2:
                    CartasPendentesCompra += 2;
                    break;
                case SimboloCarta.Joker4:
                    AguardarEscolhaCor = true;
                    break;
                case SimboloCarta.Joker:
                    AguardarEscolhaCor = true;
                    break;
            }
        }

        private void AplicarEfeitoPrimeiraCartaMesa()
        {
            var carta = Mesa.CartaNoTopo;
            if (carta == null) return;
            switch (carta.Simbolo)
            {
                case SimboloCarta.Inverter: SentidoNormal = !SentidoNormal; break;
                case SimboloCarta.Salta:
                    AvançarIndice();
                    JogadorAtivo = Jogadores[IndiceAtivo];
                    break;
                case SimboloCarta.Compra2: CartasPendentesCompra += 2; break;
            }
        }

        private void AvançarTurno()
        {
            AvançarIndice();
            JogadorAtivo = Jogadores[IndiceAtivo];
            OnTurnoMudou?.Invoke(this, new TurnoMudouEventArgs
            {
                JogadorAtivo = JogadorAtivo,
                IndiceAtivo  = IndiceAtivo
            });
        }

        private void AvançarIndice()
        {
            int n = Jogadores.Count;
            IndiceAtivo = SentidoNormal
                ? (IndiceAtivo + 1) % n
                : (IndiceAtivo - 1 + n) % n;
        }

        private void ComprarCartasPendentes(Jogador jogador)
        {
            for (int i = 0; i < CartasPendentesCompra; i++)
                ComprarCartaParaJogador(jogador);
            CartasPendentesCompra = 0;
        }

        private Carta? ComprarCartaParaJogador(Jogador jogador, bool silencioso = false)
        {
            if (Mesa.Baralho.Cartas.Count == 0)
            {
                Mesa.ReinicializarBaralho();
                OnBaralhoReabastecido?.Invoke(this, EventArgs.Empty);
            }

            var carta = Mesa.Baralho.ComprarCarta();
            if (carta == null) return null;

            jogador.ReceberCarta(carta);

            if (!silencioso)
                OnCartaComprada?.Invoke(this, new CartaCompradaEventArgs
                    { Jogador = jogador, Carta = carta });

            return carta;
        }

        private bool VerificarFimDeJogo()
        {
            var vencedor = Jogadores.FirstOrDefault(j => j.Cartas.Count == 0);
            if (vencedor == null) return false;

            JogoEmCurso = false;

            int pontos = Jogadores.Where(j => j != vencedor).Sum(j => j.PontuacaoNaMao);

            int idx = Jogadores.IndexOf(vencedor);
            if (idx >= 0 && idx < Pontuacoes.Count)
                Pontuacoes[idx] += pontos;

            // Estatísticas de partida
            vencedor.N_Partidas_Ganhos++;
            foreach (var j in Jogadores) j.N_Partidas_Jogadas++;

            // Verifica se o jogo completo terminou (≥500 pontos)
            bool fimDeJogo = Pontuacoes.Any(p => p >= PONTUACAO_MAXIMA);
            if (fimDeJogo)
            {
                vencedor.N_Jogos_Ganhos++;
                foreach (var j in Jogadores) j.N_Jogos_Jogados++;
            }

            // Guarda estatísticas automaticamente
            GestorXml.GuardarEstatisticas(Jogadores, Pontuacoes);

            OnJogoTerminou?.Invoke(this, new JogoTerminouEventArgs
            {
                Vencedor      = vencedor,
                PontuacaoRond = pontos,
                FimDeJogo     = fimDeJogo
            });

            return true;
        }

        private CorCarta CorMaisFrequenteNaMao(Jogador jogador)
        {
            var cores = new[] { CorCarta.Vermelho, CorCarta.Verde, CorCarta.Azul, CorCarta.Amarelo };
            return jogador.Cartas
                .Where(c => !c.EJoker)
                .GroupBy(c => c.Cor)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault(cores[_rnd.Next(cores.Length)]);
        }

        // ── Helpers para a UI ─────────────────────────────────────────────────────

        public bool JogadorHumanoTemVez =>
            JogoEmCurso && JogadorAtivo == Jogadores[0] && !AguardarEscolhaCor;

        public bool JogadorHumanoTemCartaJogavel =>
            CartasPendentesCompra == 0 &&
            Mesa.CartaNoTopo != null &&
            Jogadores.Count > 0 &&
            Jogadores[0].TemCartaJogavel(Mesa.CartaNoTopo, Mesa.CorAtiva);

        ///  True se o humano tem exactamente 2 cartas e ainda não gritou UNO. 
        public bool HumanoDeveGritarUno =>
            JogadorHumanoTemVez &&
            Jogadores[0].Cartas.Count == 2 &&
            !HumanoGritouUno;
    }
}
