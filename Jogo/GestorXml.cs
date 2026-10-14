using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Jogo
{
    public static class GestorXml
    {
        private static string PastaBase =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "UNO");

        private static string FicheiroEstatisticas =>
            Path.Combine(PastaBase, "estatisticas.xml");

        private static string FicheiroJogoGuardado =>
            Path.Combine(PastaBase, "jogo_guardado.xml");

        private static void GarantirPasta()
        {
            if (!Directory.Exists(PastaBase))
                Directory.CreateDirectory(PastaBase);
        }

        // ═══════════════════════════════════════════════════════════════
        //  ESTATÍSTICAS
        // ═══════════════════════════════════════════════════════════════

        /// Guarda estatísticas + pontuações acumuladas do jogo em curso.
        /// pontuacoesJogo: lista paralela a jogadores com a pontuação acumulada
        /// de cada um no jogo atual (pode ser null se não houver jogo ativo).
        public static void GuardarEstatisticas(List<Jogador> jogadores,
                                               List<int>? pontuacoesJogo = null)
        {
            try
            {
                GarantirPasta();

                // Preserva jogadores de sessões anteriores
                var existentes = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(FicheiroEstatisticas))
                {
                    try
                    {
                        var docAntigo = XDocument.Load(FicheiroEstatisticas);
                        foreach (var e in docAntigo.Root?.Elements("Jogador")
                                         ?? Enumerable.Empty<XElement>())
                        {
                            string nome = (string?)e.Element("Nome") ?? "";
                            if (!string.IsNullOrEmpty(nome))
                                existentes[nome] = e;
                        }
                    }
                    catch { }
                }

                // Atualiza jogadores da sessão atual
                for (int i = 0; i < jogadores.Count; i++)
                {
                    var j = jogadores[i];
                    int pontuacaoAcumulada = pontuacoesJogo != null && i < pontuacoesJogo.Count
                        ? pontuacoesJogo[i]
                        : 0;

                    // Se já existe entrada para este jogador, preserva a pontuação
                    // acumulada anterior caso não seja passada nenhuma nova
                    if (pontuacoesJogo == null && existentes.TryGetValue(j.Nome, out var anterior))
                        pontuacaoAcumulada = (int?)anterior.Element("PontuacaoAcumulada") ?? 0;

                    existentes[j.Nome] = new XElement("Jogador",
                        new XElement("Nome",               j.Nome),
                        new XElement("Fotografia",         j.Fotografia),
                        new XElement("N_Partidas_Jogadas", j.N_Partidas_Jogadas),
                        new XElement("N_Partidas_Ganhos",  j.N_Partidas_Ganhos),
                        new XElement("N_Jogos_Jogados",    j.N_Jogos_Jogados),
                        new XElement("N_Jogos_Ganhos",     j.N_Jogos_Ganhos),
                        new XElement("PontuacaoAcumulada", pontuacaoAcumulada)
                    );
                }

                var elementos = existentes.Values.ToList();

                var doc = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XElement("Estatisticas",
                        new XAttribute("DataGuardado", DateTime.Now.ToString("o")),
                        elementos
                    )
                );

                doc.Save(FicheiroEstatisticas);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GestorXml] Erro ao guardar estatísticas: {ex.Message}");
            }
        }

        /// Carrega estatísticas do XML para a lista de jogadores.
        /// Devolve também as pontuações acumuladas encontradas (paralelas à lista).
        public static List<int> CarregarEstatisticas(List<Jogador> jogadores)
        {
            var pontuacoes = jogadores.Select(_ => 0).ToList();
            if (!File.Exists(FicheiroEstatisticas)) return pontuacoes;

            try
            {
                var doc = XDocument.Load(FicheiroEstatisticas);

                for (int i = 0; i < jogadores.Count; i++)
                {
                    var j    = jogadores[i];
                    var elem = doc.Root?
                        .Elements("Jogador")
                        .FirstOrDefault(e => string.Equals(
                            (string?)e.Element("Nome"), j.Nome,
                            StringComparison.OrdinalIgnoreCase));

                    if (elem == null) continue;

                    j.N_Partidas_Jogadas = (int?)elem.Element("N_Partidas_Jogadas") ?? 0;
                    j.N_Partidas_Ganhos  = (int?)elem.Element("N_Partidas_Ganhos")  ?? 0;
                    j.N_Jogos_Jogados    = (int?)elem.Element("N_Jogos_Jogados")    ?? 0;
                    j.N_Jogos_Ganhos     = (int?)elem.Element("N_Jogos_Ganhos")     ?? 0;
                    pontuacoes[i]        = (int?)elem.Element("PontuacaoAcumulada") ?? 0;
                }
            }
            catch { }

            return pontuacoes;
        }

        public static List<Jogador> CarregarTodosJogadores()
        {
            var lista = new List<Jogador>();
            if (!File.Exists(FicheiroEstatisticas)) return lista;

            try
            {
                var doc = XDocument.Load(FicheiroEstatisticas);
                foreach (var elem in doc.Root?.Elements("Jogador")
                                     ?? Enumerable.Empty<XElement>())
                {
                    lista.Add(new Jogador
                    {
                        Nome               = (string?)elem.Element("Nome")               ?? "",
                        Fotografia         = (string?)elem.Element("Fotografia")         ?? "",
                        N_Partidas_Jogadas = (int?)elem.Element("N_Partidas_Jogadas")    ?? 0,
                        N_Partidas_Ganhos  = (int?)elem.Element("N_Partidas_Ganhos")     ?? 0,
                        N_Jogos_Jogados    = (int?)elem.Element("N_Jogos_Jogados")       ?? 0,
                        N_Jogos_Ganhos     = (int?)elem.Element("N_Jogos_Ganhos")        ?? 0
                    });
                }
            }
            catch { }

            return lista;
        }

        public static void LimparEstatisticas()
        {
            if (File.Exists(FicheiroEstatisticas))
                File.Delete(FicheiroEstatisticas);
        }

        // ═══════════════════════════════════════════════════════════════
        //  GUARDAR / CARREGAR ESTADO DO JOGO
        // ═══════════════════════════════════════════════════════════════

        public static bool ExisteJogoGuardado => File.Exists(FicheiroJogoGuardado);

        public static void GuardarJogo(Game jogo)
        {
            GarantirPasta();

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("JogoGuardado",
                    new XAttribute("DataGuardado", DateTime.Now.ToString("o")),
                    new XElement("Estado",
                        new XElement("IndiceAtivo",           jogo.IndiceAtivo),
                        new XElement("SentidoNormal",         jogo.SentidoNormal),
                        new XElement("CartasPendentesCompra", jogo.CartasPendentesCompra),
                        new XElement("CorAtiva",              (int)jogo.Mesa.CorAtiva)
                    ),
                    new XElement("Pontuacoes",
                        jogo.Pontuacoes.Select((p, i) =>
                            new XElement("Pontuacao", new XAttribute("Indice", i), p))
                    ),
                    new XElement("Jogadores",
                        jogo.Jogadores.Select(j =>
                            new XElement("Jogador",
                                new XElement("Nome",               j.Nome),
                                new XElement("Fotografia",         j.Fotografia),
                                new XElement("N_Partidas_Jogadas", j.N_Partidas_Jogadas),
                                new XElement("N_Partidas_Ganhos",  j.N_Partidas_Ganhos),
                                new XElement("N_Jogos_Jogados",    j.N_Jogos_Jogados),
                                new XElement("N_Jogos_Ganhos",     j.N_Jogos_Ganhos),
                                new XElement("Mao",
                                    j.Cartas.Select(c =>
                                        new XElement("Carta",
                                            new XAttribute("Id",      c.Id),
                                            new XAttribute("Cor",     (int)c.Cor),
                                            new XAttribute("Simbolo", (int)c.Simbolo)
                                        )
                                    )
                                )
                            )
                        )
                    ),
                    new XElement("Baralho",
                        jogo.Mesa.Baralho.Cartas.Select(c =>
                            new XElement("Carta",
                                new XAttribute("Id",      c.Id),
                                new XAttribute("Cor",     (int)c.Cor),
                                new XAttribute("Simbolo", (int)c.Simbolo)
                            )
                        )
                    ),
                    new XElement("Descarte",
                        jogo.Mesa.CartasJogadas.Select(c =>
                            new XElement("Carta",
                                new XAttribute("Id",      c.Id),
                                new XAttribute("Cor",     (int)c.Cor),
                                new XAttribute("Simbolo", (int)c.Simbolo)
                            )
                        )
                    )
                )
            );

            doc.Save(FicheiroJogoGuardado);
        }

        public static Game? CarregarJogo()
        {
            if (!File.Exists(FicheiroJogoGuardado)) return null;

            try
            {
                var doc  = XDocument.Load(FicheiroJogoGuardado);
                var root = doc.Root!;
                var jogo = new Game();

                foreach (var elem in root.Element("Jogadores")?.Elements("Jogador")
                                     ?? Enumerable.Empty<XElement>())
                {
                    var j = new Jogador
                    {
                        Nome               = (string?)elem.Element("Nome")               ?? "",
                        Fotografia         = (string?)elem.Element("Fotografia")         ?? "",
                        N_Partidas_Jogadas = (int?)elem.Element("N_Partidas_Jogadas")    ?? 0,
                        N_Partidas_Ganhos  = (int?)elem.Element("N_Partidas_Ganhos")     ?? 0,
                        N_Jogos_Jogados    = (int?)elem.Element("N_Jogos_Jogados")       ?? 0,
                        N_Jogos_Ganhos     = (int?)elem.Element("N_Jogos_Ganhos")        ?? 0
                    };
                    foreach (var ce in elem.Element("Mao")?.Elements("Carta")
                                       ?? Enumerable.Empty<XElement>())
                        j.Cartas.Add(CartaDeXml(ce));
                    jogo.Jogadores.Add(j);
                }

                foreach (var pe in root.Element("Pontuacoes")?.Elements("Pontuacao")
                                    ?? Enumerable.Empty<XElement>())
                    jogo.Pontuacoes.Add((int)pe);

                foreach (var ce in root.Element("Baralho")?.Elements("Carta")
                                    ?? Enumerable.Empty<XElement>())
                    jogo.Mesa.Baralho.Cartas.Add(CartaDeXml(ce));

                foreach (var ce in root.Element("Descarte")?.Elements("Carta")
                                    ?? Enumerable.Empty<XElement>())
                    jogo.Mesa.CartasJogadas.Add(CartaDeXml(ce));

                var estado = root.Element("Estado")!;
                jogo.RestaurarEstado(
                    indiceAtivo:     (int)estado.Element("IndiceAtivo")!,
                    sentidoNormal:   (bool)estado.Element("SentidoNormal")!,
                    cartasPendentes: (int)estado.Element("CartasPendentesCompra")!,
                    corAtiva:        (CorCarta)(int)estado.Element("CorAtiva")!
                );

                return jogo;
            }
            catch { return null; }
        }

        public static void ApagarJogoGuardado()
        {
            if (File.Exists(FicheiroJogoGuardado))
                File.Delete(FicheiroJogoGuardado);
        }

        private static Carta CartaDeXml(XElement e) => new Carta
        {
            Id      = (int)e.Attribute("Id")!,
            Cor     = (CorCarta)(int)e.Attribute("Cor")!,
            Simbolo = (SimboloCarta)(int)e.Attribute("Simbolo")!
        };
    }
}
