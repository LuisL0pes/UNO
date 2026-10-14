using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Jogo
{
    public class Jogador
    {
        // --- Identidade ---
        public string Nome { get; set; } = string.Empty;
        public string Fotografia { get; set; } = string.Empty; // caminho para imagem do perfil

        // --- Mão do jogador ---
        public List<Carta> Cartas { get; set; } = new();

        // --- Estatísticas (persistidas em XML) ---
        public int N_Partidas_Jogadas { get; set; }
        public int N_Partidas_Ganhos { get; set; }
        public int N_Jogos_Jogados { get; set; }
        public int N_Jogos_Ganhos { get; set; }

        // --- Propriedades calculadas ---

        // Número de cartas na mão (útil para a UI dos bots)
        public int NumeroDeCartas => Cartas.Count;

        // Pontuação total das cartas que ainda tem na mão
        public int PontuacaoNaMao => Cartas.Sum(c => c.Pontos);

        // Verdadeiro se o jogador já disse UNO (tem só 1 carta)
        public bool DeveDizerUno => Cartas.Count == 1;

        // Taxa de vitória em partidas (0.0 a 1.0)
        public double TaxaVitoriaPartidas =>
            N_Partidas_Jogadas > 0
            ? (double)N_Partidas_Ganhos / N_Partidas_Jogadas
            : 0.0;

        // --- Métodos auxiliares ---

        // Devolve as cartas que podem ser jogadas sobre a carta do topo
        public List<Carta> CartasJogaveis(Carta cartaTopo, CorCarta corAtiva)
        {
            return Cartas.Where(c => c.PodeJogarSobre(cartaTopo, corAtiva)).ToList();
        }

        // Verifica se tem alguma carta jogável
        public bool TemCartaJogavel(Carta cartaTopo, CorCarta corAtiva)
        {
            return Cartas.Any(c => c.PodeJogarSobre(cartaTopo, corAtiva));
        }

        // Adiciona uma carta à mão
        public void ReceberCarta(Carta carta)
        {
            Cartas.Add(carta);
        }

        // Remove e devolve a carta jogada
        public Carta? JogarCarta(Carta carta)
        {
            if (!Cartas.Contains(carta)) return null;
            Cartas.Remove(carta);
            return carta;
        }
    }
}