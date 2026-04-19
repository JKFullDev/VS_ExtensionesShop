namespace ExtensionesShop.Shared.Utils;

/// <summary>
/// Utilidad que contiene las 52 provincias de España
/// Se usa en los formularios de registro, perfil y checkout
/// </summary>
public static class SpanishProvinces
{
    /// <summary>
    /// Lista completa de las 52 provincias de España (incluyendo ciudades autónomas)
    /// </summary>
    public static readonly List<string> Provinces = new()
    {
        "Álava",
        "Albacete",
        "Alicante",
        "Almería",
        "Ávila",
        "Badajoz",
        "Barcelona",
        "Burgos",
        "Cáceres",
        "Cádiz",
        "Cantabria",
        "Castellón",
        "Ceuta",
        "Ciudad Real",
        "Córdoba",
        "Cuenca",
        "Girona",
        "Granada",
        "Guadalajara",
        "Guipúzcoa",
        "Huelva",
        "Huesca",
        "Islas Baleares",
        "Jaén",
        "La Coruña",
        "La Rioja",
        "Las Palmas",
        "León",
        "Lleida",
        "Lugo",
        "Madrid",
        "Málaga",
        "Melilla",
        "Murcia",
        "Navarra",
        "Ourense",
        "Palencia",
        "Palma de Mallorca",
        "Pontevedra",
        "Salamanca",
        "Segovia",
        "Sevilla",
        "Soria",
        "Tarragona",
        "Teruel",
        "Toledo",
        "Valencia",
        "Valladolid",
        "Vizcaya",
        "Zamora",
        "Zaragoza"
    };

    /// <summary>
    /// Obtiene la lista de provincias ordenadas alfabéticamente
    /// </summary>
    public static List<string> GetSortedProvinces()
    {
        return Provinces.OrderBy(p => p).ToList();
    }

    /// <summary>
    /// Valida si una provincia existe en la lista
    /// </summary>
    public static bool IsValidProvince(string province)
    {
        if (string.IsNullOrWhiteSpace(province))
            return false;

        return Provinces.Any(p => p.Equals(province, StringComparison.OrdinalIgnoreCase));
    }
}
