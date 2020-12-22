using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NodaTime;
using QuickGraph;
using QuickGraph.Algorithms;
using ZES.Interfaces;

namespace Chronos.Core
{
    public class AssetTree
    {
        private readonly BidirectionalGraph<Asset, RateEdge> _graph = new BidirectionalGraph<Asset, RateEdge>();

        public AssetTree()
        {
            _graph.AddVertex(new Currency("GBP"));
            _graph.AddVertex(new Currency("USD"));
        }
        
        public ILog Log { get; set; }

        public IEnumerable<Asset> Assets => _graph.Vertices;
        
        public void Add(Asset forAsset, Asset domAsset)
        {
            if (!_graph.ContainsVertex(forAsset))
                _graph.AddVertex(forAsset);
            if (!_graph.ContainsVertex(domAsset))
                _graph.AddVertex(domAsset);
            
            var edge = new RateEdge(forAsset, domAsset);
            if (!_graph.ContainsEdge(edge))
                _graph.AddEdge(edge);
            
            var inverseEdge = new RateEdge(domAsset, forAsset);
            if (!_graph.ContainsEdge(inverseEdge))
                _graph.AddEdge(inverseEdge);
        }

        public IEnumerable<(string forAsset, string domAsset)> GetPath(Asset forAsset, Asset domAsset)
        {
            if (!_graph.ContainsVertex(forAsset) || !_graph.ContainsVertex(domAsset))
                return null;

            return _graph.RankedShortestPathHoffmanPavley(e => 1.0, forAsset, domAsset, 1).FirstOrDefault()?.Select(e => (e.Source.Ticker, e.Target.Ticker));
        }

        private class RateEdge : Edge<Asset>
        {
            public RateEdge(Asset source, Asset target)
                : base(source, target)
            {
            }
            
            public string Url { get; set; }
        }
    }
}