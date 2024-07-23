namespace Lokad.Tokenizers.Vocab;

public interface IVocab
{

    Dictionary<string, long> Values { get; }
    Dictionary<long, string> Indices { get; }
    Dictionary<string, long> SpecialValues { get; }
    Dictionary<long, string> SpecialIndices { get; }

    IEnumerable<string> SpecialTokens();

    string GetUnknownValue();
    string GetBosValue();
    string GetClsValue();
    string GetEosValue();
    string GetMaskValue();
    string GetPadValue();
    string GetSepValue();
    
    long TokenToId(string token);
    string IdToToken(long id);
    List<long> ConvertTokensToIds(IEnumerable<string> tokens);
    void AddExtraIds(long numExtraIds);
    void AddTokens(IEnumerable<string> tokens);
}
