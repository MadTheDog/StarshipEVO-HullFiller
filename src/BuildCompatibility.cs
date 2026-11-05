namespace HullFiller
{
    public static class BuildCompatibility
    {
        public const string Build26394979 = "EC43D7D362A26B9F7795F130118B0E1EA630B5A3AE0E1732DEB5CC7FA813FE13";
        public const string Build26451593 = "A2C8EA0B33FE08AA8975E7CA05066E63E2742E9DB91957CBD3F5DD6D5776C163";
        public static bool IsSupported(string assemblyHash)
            => string.Equals(assemblyHash, Build26394979, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(assemblyHash, Build26451593, System.StringComparison.OrdinalIgnoreCase);
    }
}
