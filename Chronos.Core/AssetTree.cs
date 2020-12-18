using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;
using QuickGraph.Algorithms;

namespace Chronos.Core
{
    public class AssetTree
    {
        private readonly BidirectionalGraph<string, Edge<string>> _graph = new BidirectionalGraph<string, Edge<string>>();
        
        private class Vertex
        {
            public Vertex(string ticker)
            {
                Ticker = ticker;
            }

            public string Ticker { get; }
        }

        public void Add(Asset forAsset, Asset domAsset)
        {
            if (!_graph.ContainsVertex(forAsset.Ticker))
                _graph.AddVertex(forAsset.Ticker);
            if (!_graph.ContainsVertex(domAsset.Ticker))
                _graph.AddVertex(domAsset.Ticker);
            
            var edge = new Edge<string>(forAsset.Ticker, domAsset.Ticker);
            if (!_graph.ContainsEdge(edge))
                _graph.AddEdge(edge);
        }

        public IEnumerable<string> GetPath(Asset forAsset, Asset domAsset)
        {
            if (!_graph.ContainsVertex(forAsset.Ticker) || !_graph.ContainsVertex(domAsset.Ticker))
                return null;

            return _graph.RankedShortestPathHoffmanPavley(e => 1.0, forAsset.Ticker, domAsset.Ticker, 1).FirstOrDefault()?.Select(e => e.Source + e.Target);
        }
    }
}