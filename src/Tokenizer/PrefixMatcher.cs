namespace Lokad.Tokenizers.Tokenizer;

/// <summary>Sparse Aho-Corasick transitions with terminal suffix links, shared by both matching pipelines.</summary>
internal sealed class PrefixMatcher<T>
{
    private struct State
    {
        internal int EdgeStart, EdgeCount, Failure, Output, Depth, Value;
    }
    private readonly record struct Edge(int Symbol, int Target);
    private static readonly IComparer<Edge> edgeOrder = Comparer<Edge>.Create(static (a, b) => a.Symbol.CompareTo(b.Symbol));
    private readonly State[] states;
    private readonly Edge[] edges;
    private readonly T[] values;

    private PrefixMatcher(State[] states, Edge[] edges, T[] values)
    {
        this.states = states; this.edges = edges; this.values = values;
        // States are in breadth-first order, so every proper suffix is ready.
        for (int parent = 0; parent < states.Length; parent++)
            for (int i = states[parent].EdgeStart; i < states[parent].EdgeStart + states[parent].EdgeCount; i++)
            {
                Edge edge = edges[i];
                int fallback = parent == 0 ? 0 : Step(states[parent].Failure, edge.Symbol);
                states[edge.Target].Failure = fallback;
                states[edge.Target].Output = states[fallback].Value >= 0 ? fallback : states[fallback].Output;
            }
    }

    internal static PrefixMatcher<T> Create<TNode>(TNode root,
        Func<TNode, IEnumerable<(int Symbol, TNode Child)>> children,
        Func<TNode, (bool Terminal, T Value)> value) where TNode : class
    {
        var sources = new List<TNode> { root };
        var states = new List<State> { new() { Value = -1 } };
        var edges = new List<Edge>();
        var values = new List<T>();
        var seen = new HashSet<TNode>(ReferenceEqualityComparer.Instance) { root };
        for (int index = 0; index < sources.Count; index++)
        {
            State state = states[index];
            state.EdgeStart = edges.Count;
            var terminal = value(sources[index]);
            if (index != 0 && terminal.Terminal) { state.Value = values.Count; values.Add(terminal.Value); }
            foreach (var (symbol, child) in children(sources[index]))
            {
                if (!seen.Add(child)) throw new ArgumentException("Matcher nodes must form a tree without cycles or shared children");
                edges.Add(new(symbol, sources.Count));
                sources.Add(child);
                states.Add(new() { Depth = state.Depth + 1, Value = -1 });
            }
            state.EdgeCount = edges.Count - state.EdgeStart;
            if (state.EdgeCount > 1) edges.Sort(state.EdgeStart, state.EdgeCount, edgeOrder);
            states[index] = state;
        }
        return new(states.ToArray(), edges.ToArray(), values.ToArray());
    }

    private int Child(int state, int symbol)
    {
        int low = states[state].EdgeStart, high = low + states[state].EdgeCount - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int found = edges[middle].Symbol;
            if (found == symbol) return edges[middle].Target;
            if (found < symbol) low = middle + 1; else high = middle - 1;
        }
        return -1;
    }

    internal int Step(int state, int symbol) => Step(state, symbol, out _);

    // Counts attempted transitions, including failures, on the actual scan path.
    internal int Step(int state, int symbol, out int work)
    {
        work = 0;
        while (true)
        {
            work++;
            int child = Child(state, symbol);
            if (child >= 0) return child;
            if (state == 0) return 0;
            state = states[state].Failure;
        }
    }

    internal int FirstMatch(int state) => states[state].Value >= 0 ? state : states[state].Output;
    internal int NextMatch(int state) => states[state].Output;
    internal int Length(int state) => states[state].Depth;
    internal T Value(int state) => values[states[state].Value];
}
