using System;
using System.Collections.Generic;
using System.Text;

namespace Jogo
{
    public class Mesa
    {
        // Pilha de cartas descartadas
        public List<Carta> CartasJogadas { get; set; } = new();

        // Baralho de compra
        public Baralho Baralho { get; set; } = new();

        // Cor ativa — muda quando um Joker é jogado e o jogador escolhe cor
        public CorCarta CorAtiva { get; set; }

        // Carta visível no topo da pilha de descarte
        public Carta? CartaNoTopo => CartasJogadas.Count > 0
            ? CartasJogadas[^1]
            : null;

        // Coloca uma carta no topo da pilha de descarte
        // e atualiza a cor ativa automaticamente
        public void ColocarCarta(Carta carta, CorCarta? corEscolhida = null)
        {
            CartasJogadas.Add(carta);

            // Se for Joker, a cor ativa é a escolhida pelo jogador
            // Caso contrário, a cor ativa é a cor da carta jogada
            CorAtiva = carta.EJoker && corEscolhida.HasValue
                ? corEscolhida.Value
                : carta.Cor;
        }

        // Quando o baralho esgota, recicla a pilha de descarte
        // (mantém só a carta do topo)
        public void ReinicializarBaralho()
        {
            if (CartasJogadas.Count <= 1) return;

            // Guarda a carta do topo
            var topo = CartaNoTopo!;

            // Remove todas menos o topo e devolve ao baralho
            CartasJogadas.RemoveAt(CartasJogadas.Count - 1);
            Baralho.ReabastecerCom(CartasJogadas);

            // Repõe só o topo na pilha
            CartasJogadas.Clear();
            CartasJogadas.Add(topo);
        }
    }
}
