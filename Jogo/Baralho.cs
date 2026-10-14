using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Jogo
{
    public class Baralho
    {
        public List<Carta> Cartas { get; set; } = new();

        public Baralho()
        {
            InicializarBaralho();
        }

        private void InicializarBaralho()
        {
            int id = 1;

            CorCarta[] coresNormais = { CorCarta.Vermelho, CorCarta.Verde, CorCarta.Azul, CorCarta.Amarelo };

            foreach (CorCarta cor in coresNormais)
            {
                // 1 carta de Zero por cor
                Cartas.Add(new Carta { Id = id++, Cor = cor, Simbolo = SimboloCarta.Zero });

                // 2 cartas de cada número (1-9) por cor
                SimboloCarta[] numeros =
                {
                    SimboloCarta.Um, SimboloCarta.Dois, SimboloCarta.Tres,
                    SimboloCarta.Quatro, SimboloCarta.Cinco, SimboloCarta.Seis,
                    SimboloCarta.Sete, SimboloCarta.Oito, SimboloCarta.Nove
                };

                foreach (SimboloCarta numero in numeros)
                {
                    Cartas.Add(new Carta { Id = id++, Cor = cor, Simbolo = numero });
                    Cartas.Add(new Carta { Id = id++, Cor = cor, Simbolo = numero });
                }

                // 2 cartas de cada especial por cor
                SimboloCarta[] especiais =
                {
                    SimboloCarta.Compra2, SimboloCarta.Inverter, SimboloCarta.Salta
                };

                foreach (SimboloCarta especial in especiais)
                {
                    Cartas.Add(new Carta { Id = id++, Cor = cor, Simbolo = especial });
                    Cartas.Add(new Carta { Id = id++, Cor = cor, Simbolo = especial });
                }
            }

            // 4 Joker e 4 Joker4 (cor Preto)
            for (int i = 0; i < 4; i++)
            {
                Cartas.Add(new Carta { Id = id++, Cor = CorCarta.Preto, Simbolo = SimboloCarta.Joker });
                Cartas.Add(new Carta { Id = id++, Cor = CorCarta.Preto, Simbolo = SimboloCarta.Joker4 });
            }

            // Total: 4×(1+18+6) + 8 = 4×25 + 8 = 108 cartas ✓
        }

        public void Embaralhar()
        {
            Random rnd = new Random();
            Cartas = Cartas.OrderBy(c => rnd.Next()).ToList();
        }

        public Carta? ComprarCarta()
        {
            if (Cartas.Count == 0) return null;
            var carta = Cartas[0];
            Cartas.RemoveAt(0);
            return carta;
        }

        public List<Carta> ComprarCartas(int quantidade)
        {
            List<Carta> compradas = new();
            for (int i = 0; i < quantidade && Cartas.Count > 0; i++)
                compradas.Add(ComprarCarta()!);
            return compradas;
        }

        // Reabastece o baralho com as cartas da pilha de descarte quando esgota
        public void ReabastecerCom(List<Carta> cartasDescarte)
        {
            Cartas.AddRange(cartasDescarte);
            Embaralhar();
        }
    }
}