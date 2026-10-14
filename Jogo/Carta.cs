using System;
using System.Collections.Generic;
using System.Text;


namespace Jogo
{
    public enum CorCarta
    {
        Vermelho,
        Verde,
        Azul,
        Amarelo,
        Preto
    }

    public enum SimboloCarta
    {
        Zero,
        Um,
        Dois,
        Tres,
        Quatro,
        Cinco,
        Seis,
        Sete,
        Oito,
        Nove,
        Compra2,
        Inverter,
        Salta,
        Joker,     
        Joker4     
    }

    public class Carta
    {
        public int Id { get; set; }
        public CorCarta Cor { get; set; }
        public SimboloCarta Simbolo { get; set; }

        public int Pontos => Simbolo switch
        {
            SimboloCarta.Zero => 0,
            SimboloCarta.Um => 1,
            SimboloCarta.Dois => 2,
            SimboloCarta.Tres => 3,
            SimboloCarta.Quatro => 4,
            SimboloCarta.Cinco => 5,
            SimboloCarta.Seis => 6,
            SimboloCarta.Sete => 7,
            SimboloCarta.Oito => 8,
            SimboloCarta.Nove => 9,
            SimboloCarta.Compra2 => 20,
            SimboloCarta.Inverter => 20,
            SimboloCarta.Salta => 20,
            SimboloCarta.Joker => 50,
            SimboloCarta.Joker4 => 50,
            _ => 0
        };

        public bool EJoker =>
            Simbolo == SimboloCarta.Joker ||
            Simbolo == SimboloCarta.Joker4;

        public bool EEspecial =>
            Simbolo == SimboloCarta.Compra2 ||
            Simbolo == SimboloCarta.Inverter ||
            Simbolo == SimboloCarta.Salta ||
            EJoker;

        public bool PodeJogarSobre(Carta cartaTopo, CorCarta corAtiva)
        {
            if (EJoker) return true;
            if (cartaTopo.EJoker)
                return Cor == corAtiva;
            return Cor == cartaTopo.Cor || Simbolo == cartaTopo.Simbolo;
        }

        public override string ToString()
        {
            if (EJoker) return Simbolo.ToString();
            return $"{Cor} {Simbolo}";
        }
    }
}