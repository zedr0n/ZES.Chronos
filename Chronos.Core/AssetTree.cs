using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;
using QuickGraph.Algorithms;
using ZES.Interfaces;

namespace Chronos.Core
{
    public class AssetTree
    {
        private readonly BidirectionalGraph<string, Edge<string>> _graph = new BidirectionalGraph<string, Edge<string>>();
        
        public ILog Log { get; set; }
        
        public void Add(Asset forAsset, Asset domAsset)
        {
            if (!_graph.ContainsVertex(forAsset.Ticker))
                _graph.AddVertex(forAsset.Ticker);
            if (!_graph.ContainsVertex(domAsset.Ticker))
                _graph.AddVertex(domAsset.Ticker);
            
            var edge = new Edge<string>(forAsset.Ticker, domAsset.Ticker);
            if (!_graph.ContainsEdge(edge))
                _graph.AddEdge(edge);
            
            var inverseEdge = new Edge<string>(domAsset.Ticker, forAsset.Ticker);
            if (!_graph.ContainsEdge(inverseEdge))
                _graph.AddEdge(inverseEdge);
        }

        public IEnumerable<(string forAsset, string domAsset)> GetPath(Asset forAsset, Asset domAsset)
        {
            if (!_graph.ContainsVertex(forAsset.Ticker) || !_graph.ContainsVertex(domAsset.Ticker))
                return null;

            Log.Info($"Computing shortest path from {forAsset.AssetId} to {domAsset.AssetId}");
            return _graph.RankedShortestPathHoffmanPavley(e => 1.0, forAsset.Ticker, domAsset.Ticker, 1).FirstOrDefault()?.Select(e => (e.Source, e.Target));
        }
    }
}