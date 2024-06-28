namespace Lokad.Tokenizers.Vocab;

public interface IVocab
{
    string GetUnknownValue();
    Dictionary<string, long> Values { get; }
    Dictionary<long, string> Indices { get; }
    Dictionary<string, long> SpecialValues { get; }
    Dictionary<long, string> SpecialIndices { get; }

    IEnumerable<string> SpecialTokens();

    long TokenToId(string token);
    string IdToToken(long id);
    List<long> ConvertTokensToIds(IEnumerable<string> tokens);
    void AddExtraIds(long numExtraIds);
    void AddTokens(IEnumerable<string> tokens);
}
