// Automatisch erzeugt von make_unity.py aus out/meta.json – nicht von Hand ändern.
using System.Collections.Generic;

public static class RioHausDaten
{
    public struct Haus
    {
        public string slug, name, klasse;
        public float lotBreite, lotTiefe;   // Grundstück in Metern (X, Z), 2-m-Raster
        public int dreiecke;

        public Haus(string slug, string name, string klasse, float lotBreite, float lotTiefe, int dreiecke)
        {
            this.slug = slug; this.name = name; this.klasse = klasse;
            this.lotBreite = lotBreite; this.lotTiefe = lotTiefe; this.dreiecke = dreiecke;
        }
    }

    public static readonly Haus[] Haeuser =
    {
        new Haus("01_barraco", "Barraco", "Sehr arm", 6f, 6f, 1532),
        new Haus("02_palafita", "Palafita", "Sehr arm", 10f, 8f, 2100),
        new Haus("03_puxadinho", "Puxadinho", "Arm", 6f, 8f, 1880),
        new Haus("04_favela_reihe", "Favela-Reihe", "Arm", 10f, 8f, 2340),
        new Haus("05_cortico", "Cortiço", "Arm", 10f, 10f, 3636),
        new Haus("06_casa_rio", "Casa Rio", "Einfach", 8f, 8f, 2088),
        new Haus("07_boteco", "Casa do Boteco", "Mittel", 8f, 10f, 2608),
        new Haus("08_sobrado", "Sobrado", "Mittel", 8f, 10f, 2070),
        new Haus("09_mercadinho", "Mercadinho", "Mittel", 10f, 10f, 2156),
        new Haus("10_predio", "Prédio Copacabana", "Mittel", 12f, 12f, 2284),
        new Haus("11_zona_oeste", "Casa Zona Oeste", "Gehoben", 16f, 14f, 3584),
        new Haus("12_villa_zona_sul", "Villa Zona Sul", "Reich", 20f, 18f, 2364),
        new Haus("13_casa_barra", "Casa Barra", "Reich", 24f, 20f, 4644),
        new Haus("14_cobertura_leblon", "Cobertura Leblon", "Reich", 18f, 18f, 2748),
        new Haus("15_casa_joa", "Casa Modernista Joá", "Reich", 32f, 20f, 2044),
        new Haus("16_tankstelle_257", "Tankstelle 257", "Spezial", 30f, 22f, 6560),
        new Haus("17_mansao_leblon", "Mansão Leblon · R$ 20 Mio.", "Luxus", 36f, 30f, 5044),
        new Haus("18_mansao_joatinga", "Mansão Joatinga · R$ 50 Mio.", "Luxus", 52f, 44f, 7600),
        new Haus("19_banco_brasilia", "Banco de Brasilia", "Spezial", 28f, 24f, 14926)
    };

    public static readonly Dictionary<string, string[]> Paletten = new Dictionary<string, string[]>
    {
        { "wall", new[] { "#3FA39A", "#EBC03F", "#E07A8C", "#6FB3D9", "#8FD0B0", "#E8893A", "#A98BC9", "#B7C94A", "#E5664E", "#EFE1C0", "#EDEBE4", "#4F9FD6", "#F0A07A", "#5BA35B", "#D9A441", "#C8473A" } },
        { "nwall", new[] { "#F2F0EA", "#E9E4D8", "#D9C9A8", "#C9C9C4", "#A8A196", "#D7A27E", "#BFD3DD", "#B5C2A5" } },
        { "base", new[] { "#B5582F", "#C4683A", "#9C9B93", "#3FA39A", "#EBC03F", "#E07A8C", "#6FB3D9", "#8FD0B0", "#E8893A", "#EFE1C0", "#EDEBE4", "#F0A07A" } },
        { "door", new[] { "#2E5FA8", "#3E7D4F", "#7A4E2D", "#A33A2C", "#ECEAE4", "#5E6B66", "#D9A441", "#26282A" } },
        { "shutter", new[] { "#3E7D4F", "#2F6EA5", "#ECEAE4", "#7A3B2A", "#2F8C86", "#C9A24A" } },
        { "roof", new[] { "#B2552F", "#8E3B25", "#6E4B36", "#7C7B74" } },
        { "trim", new[] { "#E9E5D8", "#F4F2EC", "#D8C7A2", "#C9C9C4", "#EBC03F" } },
        { "accent", new[] { "#C8372D", "#2E5FA8", "#3E7D4F", "#EBC03F", "#E8893A", "#A98BC9" } },
        { "wood", new[] { "#A86B3C", "#7A4E2D", "#C9A878", "#5B4029", "#9C6B42" } },
        { "glass", new[] { "#6F98AE", "#4F7F95", "#7FA79A", "#8A9AA8", "#5E6E7A" } }
    };

    public static bool Finde(string modellName, out Haus haus)
    {
        foreach (var h in Haeuser)
            if (modellName.StartsWith(h.slug)) { haus = h; return true; }
        haus = default(Haus);
        return false;
    }
}
